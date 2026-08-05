#if CROSSRUNTIME
using System;
using Silk.NET.OpenGL;
using SkiaSharp;
using Uno.WinUI.Graphics3DGL;
using Windows.Foundation;

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// Self-contained Skia-on-GL island backing <see cref="SKCanvasElement"/>. It renders the user's
/// SkiaSharp drawing into <see cref="GLCanvasElement"/>'s own offscreen GL framebuffer through a
/// dedicated <see cref="GRContext"/>, so it is independent of the app's active render backend (Skia,
/// WebGPU, …). <see cref="GLCanvasElement"/> reads the pixels back and composites them as an image.
/// </summary>
internal sealed class SkiaGLCanvasElement : GLCanvasElement
{
	private readonly SKCanvasElement _owner;

	private GRContext? _grContext;
	private GRBackendRenderTarget? _renderTarget;
	private SKSurface? _surface;
	private int _surfaceWidth;
	private int _surfaceHeight;

	public SkiaGLCanvasElement(SKCanvasElement owner) : base(null)
	{
		_owner = owner;
	}

	protected override void Init(GL gl)
	{
		// The base has made our GL context current; build a GRContext over it. Desktop GL uses the
		// parameterless interface factory (matching the host's Skia GL renderer); the getter overload is
		// only for GLES.
		_grContext = GRContext.CreateGl(
			GRGlInterface.Create() ?? throw new NotSupportedException("OpenGL is not available (GRGlInterface create failed)."));
	}

	protected override void OnDestroy(GL gl)
	{
		_surface?.Dispose();
		_renderTarget?.Dispose();
		_grContext?.Dispose();
		_surface = null;
		_renderTarget = null;
		_grContext = null;
		_surfaceWidth = 0;
		_surfaceHeight = 0;
	}

	protected override void RenderOverride(GL gl)
	{
		if (_grContext is null)
		{
			return;
		}

		var width = (int)RenderSize.Width;
		var height = (int)RenderSize.Height;
		if (width <= 0 || height <= 0)
		{
			return;
		}

		// The base has already bound our offscreen FBO; wrap it as a Skia surface (recreated on resize).
		if (_surface is null || _surfaceWidth != width || _surfaceHeight != height)
		{
			_surface?.Dispose();
			_renderTarget?.Dispose();

			var fbo = (uint)gl.GetInteger(GLEnum.FramebufferBinding);
			var fbInfo = new GRGlFramebufferInfo(fbo, SKColorType.Rgba8888.ToGlSizedFormat());
			_renderTarget = new GRBackendRenderTarget(width, height, sampleCount: 0, stencilBits: 8, fbInfo);
			// BottomLeft matches GL framebuffer orientation; GLCanvasElement's image brush applies the Y-flip.
			_surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
			_surfaceWidth = width;
			_surfaceHeight = height;
		}

		var canvas = _surface!.Canvas;
		canvas.Clear(SKColors.Transparent);
		canvas.Save();
		// Guarantee drawing stays inside the element's area, matching the previous SKCanvasElement behaviour.
		canvas.ClipRect(new SKRect(0, 0, width, height));
		_owner.InvokeRenderOverride(canvas, new Size(width, height));
		canvas.Restore();

		_grContext.Flush();
		// Skia mutates GL state; reset so the base's glReadPixels sees a clean context.
		_grContext.ResetContext();
	}
}
#endif
