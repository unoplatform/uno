#if !__TVOS__
using System;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Metal;
using MetalKit;
using Microsoft.Graphics.Display;
using SkiaSharp;
using UIKit;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.AppleUIKit
{
	[Register(nameof(UnoSKMetalView))]
	internal sealed class UnoSKMetalView : MTKView, IMTKViewDelegate
	{
		private readonly GRContext? _context;
		private readonly IMTLCommandQueue? _queue;

		private RootViewController? _owner;

		public UnoSKMetalView()
			: base(CGRect.Empty, null)
		{
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

			var fps = UIScreen.MainScreen.MaximumFramesPerSecond;
			PreferredFramesPerSecond = fps;

			this.LogDebug()?.LogDebug($"UnoSKMetalView: {nameof(PreferredFramesPerSecond)} = {fps}");

			Device = device;

			Delegate = this;
		}

		internal void SetOwner(RootViewController owner) => _owner = owner;

		void IMTKViewDelegate.DrawableSizeWillChange(MTKView view, CGSize size)
		{
			if (Paused && EnableSetNeedsDisplay)
			{
				SetNeedsDisplay();
			}
		}

		void IMTKViewDelegate.Draw(MTKView view)
		{
			var size = DrawableSize;

			var width = (int)size.Width;
			var height = (int)size.Height;

			if (width <= 0 || height <= 0)
			{
				return;
			}

			SKSurface? surface = null;
			SKCanvas? canvas = null;
			ICAMetalDrawable? drawable = null;
			IMTLCommandBuffer? commandBuffer = null;

			try
			{
				// Defer the acquisition of the drawable
				surface = SKSurface.Create(_context, this, GRSurfaceOrigin.TopLeft, (int)SampleCount, SKColorType.Bgra8888);

				canvas = surface.Canvas;

				// Paint
				_owner!.OnPaintSurfaceInner(surface, canvas);

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
#endif
