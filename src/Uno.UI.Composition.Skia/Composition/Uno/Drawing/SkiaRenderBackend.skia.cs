#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>The default <see cref="IRenderer"/>: the established Skia two-phase (SKPicture) lifecycle.</summary>
internal sealed class SkiaRenderer : IRenderer, IDisposable
{
	// GL state (built lazily on the first GL present, once the host's GL context is current; per-renderer,
	// i.e. per graphics context). Null for the software / host-canvas cases.
	private GRContext? _glContext;
	private GRBackendRenderTarget? _glRenderTarget;
	private SKSurface? _glSurface;
	private int _glWidth;
	private int _glHeight;

	public ICommandRecorder BeginFrame() => SkiaDrawingSession.StartRecording();

	// Wraps whatever target the active context handed over. A host that already owns an SKCanvas passes a
	// SkiaRenderTarget directly; a neutral context (Uno's context factory) hands a kind-specific target that
	// the Skia backend wraps here — CPU framebuffer → SKSurface over pixels; GL framebuffer → GRContext-GL.
	public IPresentSession BeginPresent(IRenderTarget target)
		=> target switch
		{
			SkiaRenderTarget skia => new SkiaPresentSession(skia.Canvas),
			ISoftwareRenderTarget software => SkiaPresentSession.ForSoftware(software),
			IGLRenderTarget gl => PresentForGL(gl),
			_ => throw new NotSupportedException($"The Skia backend cannot present onto a render target of type {target.GetType().Name}."),
		};

	// The host has made its GL context current; build/reuse a GRContext-GL and an SKSurface over the window
	// framebuffer, then compose into its (borrowed, cached-across-frames) canvas. The context swaps on present.
	private IPresentSession PresentForGL(IGLRenderTarget gl)
	{
		_glContext ??= GRContext.CreateGl(
				(gl.IsGles && gl.GetProcAddress is { } loader
					? GRGlInterface.CreateGles(name => loader(name))
					: GRGlInterface.Create())
				?? throw new NotSupportedException("OpenGL is not available (GRGlInterface create failed)."))
			?? throw new NotSupportedException("Failed to create an OpenGL GRContext.");

		if (_glSurface is null || gl.Width != _glWidth || gl.Height != _glHeight)
		{
			_glWidth = gl.Width;
			_glHeight = gl.Height;
			_glRenderTarget?.Dispose();
			_glSurface?.Dispose();

			var info = new GRGlFramebufferInfo(gl.FramebufferId, SKColorType.Rgba8888.ToGlSizedFormat());
			_glRenderTarget = new GRBackendRenderTarget(gl.Width, gl.Height, gl.SampleCount, gl.StencilBits, info);
			// BottomLeft to match OpenGL's origin.
			_glSurface = SKSurface.Create(_glContext, _glRenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
		}

		return new SkiaPresentSession(_glSurface!.Canvas);
	}

	public void Dispose()
	{
		_glSurface?.Dispose();
		_glRenderTarget?.Dispose();
		_glContext?.Dispose();
	}
}
