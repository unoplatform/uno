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

	// Metal state (built lazily on the first Metal present from the host's device/queue; per-renderer). The
	// per-frame texture changes, so the render target + surface are recreated each present; the GRContext is cached.
	private GRContext? _metalContext;

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
			IMetalRenderTarget metal => PresentForMetal(metal),
			_ => throw new NotSupportedException($"The Skia backend cannot present onto a render target of type {target.GetType().Name}."),
		};

	// The host hands the per-frame MTLTexture (+ its device/queue); build/reuse a GRContext-Metal and wrap the
	// texture as an SKSurface to compose into. Present flushes the GRContext so the render lands in the texture
	// before the host commits/presents the drawable. Recreated each frame (the texture differs per call).
	private IPresentSession PresentForMetal(IMetalRenderTarget metal)
	{
		_metalContext ??= GRContext.CreateMetal(new GRMtlBackendContext { DeviceHandle = metal.Device, QueueHandle = metal.Queue })
			?? throw new NotSupportedException("Failed to create a Metal GRContext.");

		var colorType = metal.ColorFormat == GraphicsColorFormat.Bgra8888 ? SKColorType.Bgra8888 : SKColorType.Rgba8888;
		var target = new GRBackendRenderTarget(metal.Width, metal.Height, new GRMtlTextureInfo(metal.Texture));
		var surface = SKSurface.Create(_metalContext, target, GRSurfaceOrigin.TopLeft, colorType);
		// The render target descriptor is consumed by SKSurface.Create; the surface is disposed on present.
		target.Dispose();
		return SkiaPresentSession.ForGpuTexture(surface, _metalContext);
	}

	// The host has made its GL context current; build/reuse a GRContext-GL and an SKSurface over the window
	// framebuffer, then compose into its (borrowed, cached-across-frames) canvas. The context swaps on present.
	private IPresentSession PresentForGL(IGLRenderTarget gl)
	{
		// GLES/WebGL: assemble the interface from the host's neutral proc loader (the seam mandates one, so this
		// path is backend-agnostic). Desktop GL: SkiaSharp's proc-assembled interface (CreateOpenGl/Create(getProc))
		// segfaults on Mesa/llvmpipe — validated with three loader variants — whereas its compiled-in native
		// interface (Create()) renders correctly, so desktop GL uses that. The loader still rides the seam for any
		// non-Skia backend; this is purely SkiaSharp's own desktop-GL assembly being unstable.
		var loader = gl.GetProcAddress;
		_glContext ??= GRContext.CreateGl(
				(gl.Flavor switch
				{
					GLFlavor.OpenGLES => GRGlInterface.CreateGles(name => loader(name)),
					GLFlavor.WebGL => GRGlInterface.CreateWebGl(name => loader(name)),
					_ => GRGlInterface.Create(),
				})
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
