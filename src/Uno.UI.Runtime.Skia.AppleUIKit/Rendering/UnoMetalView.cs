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
		/// Creates the neutral native-texture Metal context bound to this view's device/queue.
		/// </summary>
		internal Uno.UI.Composition.Drawing.ISwapChain CreateGraphicsContext()
			=> new AppleMetalGraphicsContext(Device!.Handle, _queue!.Handle);

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
		static FrameRateLogger _drawFpsLogger = new FrameRateLogger(typeof(UnoMetalView), "Draw");
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

#if __TVOS__ // TODO: tvOS is not supported yet.
			return;
#else
			ICAMetalDrawable? drawable = null;
			IMTLCommandBuffer? commandBuffer = null;

			try
			{
				drawable = CurrentDrawable;
				if (drawable is null)
				{
					return;
				}

				// Push the drawable's texture into the negotiated Metal context and render through the neutral loop.
				_owner?.OnMetalFrame(drawable.Texture.Handle);

				commandBuffer = _queue!.CommandBuffer()!;
				commandBuffer.PresentDrawable(drawable);
				commandBuffer.Commit();
			}
			finally
			{
				// Release the drawable as soon as possible
				// See : https://developer.apple.com/library/archive/documentation/3DDrawing/Conceptual/MTLBestPracticesGuide/Drawables.html
				((IDisposable?)commandBuffer)?.Dispose();
				((IDisposable?)drawable)?.Dispose();
			}
#endif
		}

	}
}
