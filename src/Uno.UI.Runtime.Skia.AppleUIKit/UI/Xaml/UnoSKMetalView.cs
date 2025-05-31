﻿#if !__TVOS__
using System;
using System.Threading;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using IOSurface;
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
		private SKPicture? _picture;
		private CADisplayLink _link;
		private Thread? _renderThread;

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

#if !__TVOS__
			FramebufferOnly = false;

			// Disable UIKit’s display‑link
			Paused = true;

			// We're drawing ourselves
			EnableSetNeedsDisplay = false;
#endif

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
			var recorder = new SKPictureRecorder();
			var canvas = recorder.BeginRecording(new SKRect(-9999, -9999, 9999, 9999));
			using (new SKAutoCanvasRestore(canvas, true))
			{
				_owner!.OnPaintSurfaceInner(canvas);

				var picture = recorder.EndRecording();

				Interlocked.Exchange(ref _picture, picture);
			}

			_link.Paused = false;
		}

		void IMTKViewDelegate.DrawableSizeWillChange(MTKView view, CGSize size)
		{
			if (Paused && EnableSetNeedsDisplay)
			{
				SetNeedsDisplay();
			}
		}

		void IMTKViewDelegate.Draw(MTKView view)
		{
			var currentPicture = Volatile.Read(ref _picture);

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
				using (new SKAutoCanvasRestore(canvas, true))
				{
					// start drawing
					if (_picture is { } picture)
					{
						canvas.DrawPicture(picture);
					}
				}

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

			_link.Paused = ReferenceEquals(currentPicture, Volatile.Read(ref _picture));
		}
	}
}
#endif
