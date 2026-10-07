#if CROSSRUNTIME
using System;
using SkiaSharp;
using Uno.Graphics;
using Windows.Foundation;

namespace Uno.WinUI.Graphics2DSK;

/// <summary>
/// Draws <see cref="SKCanvasElement"/> into a GL island's framebuffer through a dedicated <see cref="GRContext"/>,
/// for backends that expose no <see cref="SKCanvas"/>.
/// </summary>
internal sealed class SkiaGLIslandRenderer(SKCanvasElement owner) : IGLIslandRenderer
{
	private GRContext? _grContext;
	private GRBackendRenderTarget? _renderTarget;
	private SKSurface? _surface;
	private int _surfaceWidth;
	private int _surfaceHeight;

	// Desktop GL uses the parameterless interface factory (the getter overload is only for GLES).
	public void Init()
		=> _grContext = GRContext.CreateGl(
			GRGlInterface.Create() ?? throw new NotSupportedException("OpenGL is not available (GRGlInterface create failed)."));

	public void Render(uint framebuffer, int width, int height)
	{
		if (_grContext is null)
		{
			return;
		}

		if (_surface is null || _surfaceWidth != width || _surfaceHeight != height)
		{
			_surface?.Dispose();
			_renderTarget?.Dispose();

			var fbInfo = new GRGlFramebufferInfo(framebuffer, SKColorType.Rgba8888.ToGlSizedFormat());
			_renderTarget = new GRBackendRenderTarget(width, height, sampleCount: 0, stencilBits: 8, fbInfo);
			// BottomLeft matches GL framebuffer orientation; the island's image brush applies the Y-flip.
			_surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
			_surfaceWidth = width;
			_surfaceHeight = height;
		}

		var canvas = _surface!.Canvas;
		canvas.Clear(SKColors.Transparent);
		canvas.Save();
		canvas.ClipRect(new SKRect(0, 0, width, height));
		owner.InvokeRenderOverride(canvas, new Size(width, height));
		canvas.Restore();

		_grContext.Flush();
		// Skia mutates GL state; reset so the island's glReadPixels sees a clean context.
		_grContext.ResetContext();
	}

	public void Destroy()
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

	public void OnUnavailable() => owner.OnIslandUnavailable();
}
#endif
