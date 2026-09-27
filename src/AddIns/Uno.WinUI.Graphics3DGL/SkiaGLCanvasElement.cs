#if CROSSRUNTIME
using System;
using Silk.NET.OpenGL;
using SkiaSharp;
using Windows.Foundation;

namespace Uno.WinUI.Graphics3DGL;

/// <summary>
/// Self-contained Skia-on-GL island backing Graphics2DSK's <c>SKCanvasElement</c> when the active render backend
/// exposes no <see cref="SKCanvas"/>: renders the SkiaSharp drawing into this element's offscreen GL framebuffer
/// through a dedicated <see cref="GRContext"/>, which <see cref="GLCanvasElement"/> reads back and composites.
/// </summary>
/// <remarks>
/// Lives here rather than in Graphics2DSK so that Graphics2DSK has no reference to this optional add-in.
/// Graphics2DSK creates it by name; keep the type name and constructor in sync with <c>SKCanvasElement</c>.
/// </remarks>
internal sealed class SkiaGLCanvasElement : GLCanvasElement
{
	private readonly Action<SKCanvas, Size> _render;
	private readonly Action _onUnavailable;

	private GRContext? _grContext;
	private GRBackendRenderTarget? _renderTarget;
	private SKSurface? _surface;
	private int _surfaceWidth;
	private int _surfaceHeight;

	public SkiaGLCanvasElement(Action<SKCanvas, Size> render, Action onUnavailable) : base(null)
	{
		_render = render;
		_onUnavailable = onUnavailable;
	}

	protected override void Init(GL gl)
	{
		// The base has made our GL context current; build a GRContext over it. Desktop GL uses the parameterless
		// interface factory (the getter overload is only for GLES).
		_grContext = GRContext.CreateGl(
			GRGlInterface.Create() ?? throw new NotSupportedException("OpenGL is not available (GRGlInterface create failed)."));
	}

	protected override void OnGLUnavailable() => _onUnavailable();

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
		// Keep drawing inside the element's area.
		canvas.ClipRect(new SKRect(0, 0, width, height));
		_render(canvas, new Size(width, height));
		canvas.Restore();

		_grContext.Flush();
		// Skia mutates GL state; reset so the base's glReadPixels sees a clean context.
		_grContext.ResetContext();
	}
}
#endif
