using System;
using System.Diagnostics;
using System.Threading;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using IOSurface;
using Metal;
using MetalKit;
using Microsoft.Graphics.Display;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia.AppleUIKit
{
	internal sealed partial class UnoSKMetalView : MTKView, IMTKViewDelegate
	{
		/// <summary>
		/// GPU resource cache limit for iOS devices (128 MB).
		/// Prevents unbounded GPU resource growth that can trigger jetsam kills.
		/// </summary>
		private const int ResourceCacheBytes = 128 * 1024 * 1024;

		/// <summary>
		/// Number of consecutive idle frames (no new render request) before pausing
		/// the CADisplayLink. This provides a grace period for the UI-thread render
		/// pipeline to call <see cref="QueueRender"/> after processing animations.
		/// </summary>
		private const int IdleFramesBeforePause = 2;

		private readonly GRContext? _context;
		private readonly IMTLCommandQueue? _queue;

		private RootViewController? _owner;
		private CADisplayLink _link;
		private Thread? _renderThread;

		/// <summary>
		/// Set by <see cref="QueueRender"/> on any thread, read and cleared by
		/// <see cref="IMTKViewDelegate.Draw"/> on the render thread.
		/// </summary>
		private volatile bool _renderQueued;

		/// <summary>
		/// Tracks consecutive frames where no new render was requested.
		/// Only accessed from the render thread (CADisplayLink callback).
		/// </summary>
		private int _idleFrameCount;

		/// <summary>
		/// Creates a new instance of <see cref="UnoSKMetalView"/>.
		/// </summary>
		/// <param name="onFrameDrawn">A delegate that will be called on a separate thread once per frame draw.</param>
		public UnoSKMetalView()
			: base(CGRect.Empty, null)
		{
			_link = CADisplayLink.Create(() => this.Draw());
			var device = MTLDevice.SystemDefault;

			if (device == null)
			{
				Console.WriteLine("Metal is not supported on this device.");
				return;
			}

			var queue = device.CreateCommandQueue();

			if (queue == null)
			{
				Console.WriteLine("Failed to create command queue.");

				return;
			}

			_context = GRContext.CreateMetal(new GRMtlBackendContext()
			{
				Device = device,
				Queue = queue
			});

			_context.SetResourceCacheLimit(ResourceCacheBytes);

			_queue = queue;

			ColorPixelFormat = MTLPixelFormat.BGRA8Unorm;
			DepthStencilPixelFormat = MTLPixelFormat.Depth32Float_Stencil8;
			SampleCount = 1;

			// Skia renders via GPU commands and never reads pixels back from the
			// framebuffer. Setting FramebufferOnly = true tells Metal the drawable
			// texture is write-only, which avoids forcing tile data to system memory
			// on TBDR GPUs (all iOS devices), saving significant memory bandwidth.
			FramebufferOnly = true;

			// Disable UIKit’s display‑link
			Paused = true;

			// We're drawing ourselves
			EnableSetNeedsDisplay = false;

			var fps = UIScreen.MainScreen.MaximumFramesPerSecond;
			PreferredFramesPerSecond = fps;

			this.LogDebug()?.LogDebug($"UnoSKMetalView: {nameof(PreferredFramesPerSecond)} = {fps}");

			Device = device;

			Delegate = this;

			StartRenderThread();
		}

		private void StartRenderThread()
		{
			// Purge GPU resources when the system signals memory pressure
			// to reduce the risk of jetsam kills on iOS.
			NSNotificationCenter.DefaultCenter.AddObserver(
				UIApplication.DidReceiveMemoryWarningNotification,
				_ => _context?.PurgeResources());

			_renderThread = new Thread(() =>
			{
				var currentThread = NSThread.Current;
				currentThread.QualityOfService = NSQualityOfService.UserInteractive;
				currentThread.Name = "UnoSKMetalViewRenderThread";

				// CAFrameRateRange is only available on iOS 15.0+
				if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
				{
					_link.PreferredFrameRateRange = new CAFrameRateRange()
					{
						Minimum = 30,
						Preferred = PreferredFramesPerSecond,
						Maximum = PreferredFramesPerSecond
					};
				}
				else
				{
					// Fallback for iOS < 15.0: use the deprecated PreferredFramesPerSecond property
					// Note: The legacy API doesn't support setting minimum/maximum frame rates,
					// so we only set the preferred rate. This provides best-effort frame rate control.
#pragma warning disable CA1422 // Validate platform compatibility
					_link.PreferredFramesPerSecond = PreferredFramesPerSecond;
#pragma warning restore CA1422 // Validate platform compatibility
				}

				_link.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Default);

				NSRunLoop.Current.Run();   // blocks forever
			})
			{
				IsBackground = true,
				Name = "UnoSKMetalViewRenderThread"
			};
			_renderThread.Start();
		}

		/// <summary>
		/// Whether we are currently in the high frame rate mode.
		/// Only accessed from the render thread.
		/// </summary>
		private bool _isHighFrameRate = true;

		internal void SetOwner(RootViewController owner) => _owner = owner;

		/// <summary>
		/// Adjusts the CADisplayLink's <see cref="CADisplayLink.PreferredFrameRateRange"/>
		/// based on whether rendering activity is ongoing. On ProMotion devices this
		/// allows iOS to lower the physical display refresh rate when the app is idle,
		/// saving significant battery.
		/// </summary>
		private void UpdateFrameRateRange(bool wantHighRate)
		{
			if (_isHighFrameRate == wantHighRate)
			{
				return;
			}

			_isHighFrameRate = wantHighRate;

			if (!UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
			{
				return;
			}

			if (wantHighRate)
			{
				_link.PreferredFrameRateRange = new CAFrameRateRange()
				{
					Minimum = 30,
					Preferred = PreferredFramesPerSecond,
					Maximum = PreferredFramesPerSecond
				};
			}
			else
			{
				// When idle, tell ProMotion we only need a low refresh rate.
				// The display will dynamically reduce its physical refresh rate
				// to save power.
				_link.PreferredFrameRateRange = new CAFrameRateRange()
				{
					Minimum = 10,
					Preferred = 30,
					Maximum = 60
				};
			}
		}

		public void QueueRender()
		{
			_renderQueued = true;
			_link.Paused = false;
		}

		void IMTKViewDelegate.DrawableSizeWillChange(MTKView view, CGSize size)
		{
			if (Paused && EnableSetNeedsDisplay)
			{
				SetNeedsDisplay();
			}
		}

#if REPORT_FPS
		static FrameRateLogger _drawFpsLogger = new FrameRateLogger(typeof(UnoSKMetalView), "Draw");
#endif

		void IMTKViewDelegate.Draw(MTKView view)
		{
#if REPORT_FPS
			_drawFpsLogger.ReportFrame();
#endif

			// Instead of pausing the display link immediately (which forces a full
			// round-trip through the UI thread before the next frame can fire), we
			// track consecutive idle frames and only pause after the link has fired
			// without any new render request for several frames. This avoids missing
			// VSyncs during animations when the UI thread is slightly delayed in
			// calling QueueRender().
			if (_renderQueued)
			{
				_renderQueued = false;
				_idleFrameCount = 0;
				UpdateFrameRateRange(wantHighRate: true);
			}
			else
			{
				if (++_idleFrameCount > IdleFramesBeforePause)
				{
					UpdateFrameRateRange(wantHighRate: false);
					_link.Paused = true;
					return;
				}
			}

			var size = DrawableSize;

			var width = (int)size.Width;
			var height = (int)size.Height;

			SKSurface? surface = null;
			SKCanvas? canvas = null;
			ICAMetalDrawable? drawable = null;
			IMTLCommandBuffer? commandBuffer = null;

			try
			{
				// Defer the acquisition of the drawable
#if __TVOS__ // TODO: tvOS is not supported yet.
				surface = SKSurface.CreateNull(width, height);
#else
				surface = SKSurface.Create(_context, this, GRSurfaceOrigin.TopLeft, (int)SampleCount, SKColorType.Bgra8888);
#endif

				canvas = surface.Canvas;

				_owner?.OnRenderFrameRequested(canvas);

				// Submit all pending GPU work. The context flush already submits
				// everything including any pending canvas operations, so we pass
				// skipCanvasFlush: true to RenderPicture to avoid a redundant
				// canvas.Flush() call.
				_context!.Flush(submit: true);

				// Present
				drawable = CurrentDrawable;

				if (drawable != null)
				{
					commandBuffer = _queue!.CommandBuffer()!;
					commandBuffer.PresentDrawable(drawable);
					commandBuffer.Commit();
				}
			}
			finally
			{
				// Release the drawable as soon as possible
				// See : https://developer.apple.com/library/archive/documentation/3DDrawing/Conceptual/MTLBestPracticesGuide/Drawables.html
				((IDisposable?)commandBuffer)?.Dispose();
				((IDisposable?)drawable)?.Dispose();
				((IDisposable?)canvas)?.Dispose();
				((IDisposable?)surface)?.Dispose();
			}
		}
	}
}
