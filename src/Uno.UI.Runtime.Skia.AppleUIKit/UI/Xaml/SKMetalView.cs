#if __IOS__ || __MACOS__
using System;
using System.ComponentModel;
using System.Diagnostics;
using CoreGraphics;
using Foundation;
using Metal;
using MetalKit;

#if __IOS__
namespace SkiaSharp.Views.iOS
#elif __MACOS__
namespace SkiaSharp.Views.Mac
#endif
{
	[Register(nameof(SKMetalView))]
	[DesignTimeVisible(true)]
	public class SKMetalView : MTKView, IMTKViewDelegate, IComponent
	{
		// for IComponent
#pragma warning disable 67
		private event EventHandler DisposedInternal;
#pragma warning restore 67
		ISite IComponent.Site { get; set; }
		event EventHandler IComponent.Disposed
		{
			add { DisposedInternal += value; }
			remove { DisposedInternal -= value; }
		}

		private bool designMode;

		private GRMtlBackendContext backendContext;
		private GRContext context;

		// created in code
		public SKMetalView()
			: this(CGRect.Empty)
		{
			Initialize();
		}

		// created in code
		public SKMetalView(CGRect frame)
			: base(frame, null)
		{
			Initialize();
		}

		// created in code
		public SKMetalView(CGRect frame, IMTLDevice device)
			: base(frame, device)
		{
			Initialize();
		}

		// created via designer
		public SKMetalView(IntPtr p)
			: base(p)
		{
		}

		// created via designer
		public override void AwakeFromNib()
		{
			Initialize();
		}

		private void Initialize()
		{
			designMode = ((IComponent)this).Site?.DesignMode == true || !Extensions.IsValidEnvironment;

			if (designMode)
				return;

			var device = MTLDevice.SystemDefault;
			if (device == null)
			{
				Console.WriteLine("Metal is not supported on this device.");
				return;
			}

			ColorPixelFormat = MTLPixelFormat.BGRA8Unorm;
			DepthStencilPixelFormat = MTLPixelFormat.Depth32Float_Stencil8;
			SampleCount = 1;
			Device = device;
			backendContext = new GRMtlBackendContext
			{
				Device = device,
				Queue = device.CreateCommandQueue(),
			};

			// hook up the drawing
			Delegate = this;
		}

		public SKSize CanvasSize { get; private set; }

		public GRContext GRContext => context;

		void IMTKViewDelegate.DrawableSizeWillChange(MTKView view, CGSize size)
		{
			CanvasSize = size.ToSKSize();

			if (Paused && EnableSetNeedsDisplay)
#if __IOS__
				SetNeedsDisplay();
#elif __MACOS__
				NeedsDisplay = true;
#endif
		}

		void IMTKViewDelegate.Draw(MTKView view)
		{
			if (designMode)
				return;

			if (backendContext.Device == null || backendContext.Queue == null || CurrentDrawable?.Texture == null)
				return;

			CanvasSize = DrawableSize.ToSKSize();

			if (CanvasSize.Width <= 0 || CanvasSize.Height <= 0)
				return;

			// create the contexts if not done already
			context ??= GRContext.CreateMetal(backendContext);

			const SKColorType colorType = SKColorType.Bgra8888;
			const GRSurfaceOrigin surfaceOrigin = GRSurfaceOrigin.TopLeft;

			// create the render target
			var metalInfo = new GRMtlTextureInfo(CurrentDrawable.Texture);
			using var renderTarget = new GRBackendRenderTarget((int)CanvasSize.Width, (int)CanvasSize.Height, (int)SampleCount, metalInfo);

			// create the surface
			using var surface = SKSurface.Create(context, renderTarget, surfaceOrigin, colorType);
			using var canvas = surface.Canvas;

			// start drawing
			var e = new SKPaintMetalSurfaceEventArgs(surface, renderTarget, surfaceOrigin, colorType);
			OnPaintSurface(e);

			// flush the SkiaSharp contents
			canvas.Flush();
			surface.Flush();
			context.Flush();

			// present
			using var commandBuffer = backendContext.Queue.CommandBuffer();
			commandBuffer.PresentDrawable(CurrentDrawable);
			commandBuffer.Commit();
		}

		public event EventHandler<SKPaintMetalSurfaceEventArgs> PaintSurface;

		protected virtual void OnPaintSurface(SKPaintMetalSurfaceEventArgs e)
		{
			PaintSurface?.Invoke(this, e);
		}
	}
}
#endif
