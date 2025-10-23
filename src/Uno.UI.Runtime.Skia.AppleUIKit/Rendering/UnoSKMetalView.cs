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
		private readonly GRContext? _context;
		private readonly IMTLCommandQueue? _queue;

		private RootViewController? _owner;
		private CADisplayLink _link;
		private Thread? _renderThread;

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

			_queue = queue;

			ColorPixelFormat = MTLPixelFormat.BGRA8Unorm;
			DepthStencilPixelFormat = MTLPixelFormat.Depth32Float_Stencil8;
			SampleCount = 1;

			FramebufferOnly = false;

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
			_renderThread = new Thread(() =>
			{
				var currentThread = NSThread.Current;
				currentThread.QualityOfService = NSQualityOfService.UserInteractive;
				currentThread.Name = "UnoSKMetalViewRenderThread";

				_link.PreferredFrameRateRange = new CAFrameRateRange()
				{
					Minimum = 30,
					Preferred = PreferredFramesPerSecond,
					Maximum = PreferredFramesPerSecond
				};

				_link.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Default);

				NSRunLoop.Current.Run();   // blocks forever
			})
			{
				IsBackground = true,
				Name = "UnoSKMetalViewRenderThread"
			};
			_renderThread.Start();
		}

		internal void SetOwner(RootViewController owner) => _owner = owner;

		public void QueueRender()
		{
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

			_link.Paused = true;

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

				// Flush
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
