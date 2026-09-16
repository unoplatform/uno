using System;
using System.Threading;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using IOSurface;
using Metal;
using MetalKit;
using Microsoft.Graphics.Display;
using Microsoft.UI.Xaml.Media;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia.AppleUIKit
{
	internal sealed partial class UnoMetalView : MTKView, IMTKViewDelegate, IAppleUIKitRenderView
	{
		private readonly IMTLCommandQueue? _queue;

		private RootViewController? _owner;
		private CADisplayLink _link;
		private Thread? _renderThread;
		private int _renderRequested;

		/// <summary>
		/// Creates a new instance of <see cref="UnoMetalView"/>.
		/// </summary>
		/// <param name="onFrameDrawn">A delegate that will be called on a separate thread once per frame draw.</param>
		public UnoMetalView()
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

			// The negotiated backend owns its Metal render state via the neutral IMetalRenderTarget seam; the view only
			// supplies the per-frame drawable texture and presents.
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

			this.LogDebug()?.LogDebug($"UnoMetalView: {nameof(PreferredFramesPerSecond)} = {fps}");

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
				currentThread.Name = "UnoMetalViewRenderThread";

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
				Name = "UnoMetalViewRenderThread"
			};
			_renderThread.Start();
		}

		internal void SetOwner(RootViewController owner) => _owner = owner;

		void IAppleUIKitRenderView.SetOwner(RootViewController owner) => SetOwner(owner);

		/// <summary>
		/// Creates the neutral native-texture Metal context bound to this view's device/queue, or <c>null</c> when
		/// the constructor found no Metal device — negotiation then reports a decline and tries the next kind,
		/// instead of the NullReferenceException a dereference here would surface as.
		/// </summary>
		internal Uno.UI.Composition.Drawing.ISwapChain? CreateGraphicsContext()
			=> Device is { } device && _queue is { } queue
				? new AppleMetalGraphicsContext(device, queue, () => CurrentDrawable)
				: null;

		public void QueueRender()
		{
			// Ordered before the un-pause: Draw clears this and then decides whether to pause, so a request that
			// lands while a frame is in flight is still seen even if its un-pause is overwritten.
			Volatile.Write(ref _renderRequested, 1);
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
		static FrameRateLogger _drawFpsLogger = new FrameRateLogger(typeof(UnoMetalView), "Draw");
#endif

		void IMTKViewDelegate.Draw(MTKView view)
		{
#if REPORT_FPS
			_drawFpsLogger.ReportFrame();
#endif

			// This frame answers every request made so far; anything asked from here on has to keep the link
			// running, which is why this is cleared before rendering rather than after.
			Volatile.Write(ref _renderRequested, 0);

			try
			{
				// The drawable is acquired by the context at present time, not here: holding one across the frame's
				// CPU work drains CAMetalLayer's small pool and stalls every frame.
				// See : https://developer.apple.com/library/archive/documentation/3DDrawing/Conceptual/MTLBestPracticesGuide/Drawables.html
				_owner?.OnFrameRequested();
			}
			finally
			{
				// Pausing is what loses a request: QueueRender un-pauses, and pausing afterwards overwrites that
				// with no trace, so the link never fires again and rendering stops for good. Pause only when
				// nothing is pending, then re-check for a request that raced the pause itself.
				if (Volatile.Read(ref _renderRequested) == 0)
				{
					_link.Paused = true;

					if (Volatile.Read(ref _renderRequested) != 0)
					{
						_link.Paused = false;
					}
				}
			}
		}

	}
}
