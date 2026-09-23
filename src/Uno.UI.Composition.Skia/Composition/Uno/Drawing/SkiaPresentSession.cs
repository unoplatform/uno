#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IPresentSession"/>; disposing flushes the composed frame to the surface.</summary>
internal sealed class SkiaPresentSession : SkiaDrawingSession, IPresentSession
{
	private readonly int _saveCount;
	// The SKSurface this session composes into, disposed on present only when owned (_ownsSurface).
	private readonly SKSurface? _surface;
	private readonly bool _ownsSurface;
	// Non-null for a GPU-texture present (Metal/Vulkan): the GRContext to submit on present so the render lands
	// in the texture/image before the host commits/blits it. The GRContext itself is cached by the renderer.
	private readonly GRContext? _flushContext;
	// Non-null when this session composes into a retained offscreen the backend owns: the surface the offscreen is
	// blitted onto at present.
	private readonly SKSurface? _presentTo;

	public SkiaPresentSession(SKCanvas canvas, IDrawingFactory factory) : base(canvas, factory)
		=> _saveCount = canvas.Save();

	private SkiaPresentSession(SKSurface surface, GRContext? flushContext, bool ownsSurface, IDrawingFactory factory, SKSurface? presentTo = null) : base(surface.Canvas, factory)
	{
		_presentTo = presentTo;
		_surface = surface;
		_flushContext = flushContext;
		_ownsSurface = ownsSurface;
		_saveCount = surface.Canvas.Save();
	}

	/// <summary>Wraps the host's neutral CPU framebuffer as an owned SKSurface to compose into (disposed on present).</summary>
	public static SkiaPresentSession ForSoftware(ISoftwareRenderTarget target, IDrawingFactory factory)
	{
		var colorType = SkiaDrawingFactory.ToColorType(target.ColorFormat);
		// An opaque color type has no alpha channel to premultiply into; pairing it with Premul is invalid in Skia.
		var alphaType = colorType == SKColorType.Rgb888x ? SKAlphaType.Opaque : SKAlphaType.Premul;
		var info = new SKImageInfo(target.Width, target.Height, colorType, alphaType);
		return new SkiaPresentSession(SKSurface.Create(info, target.Pixels, target.RowBytes), flushContext: null, ownsSurface: true, factory);
	}

	/// <summary>Wraps a per-frame GPU-texture SKSurface (e.g. Metal) the session owns; present flushes+submits the GRContext.</summary>
	public static SkiaPresentSession ForGpuTexture(SKSurface ownedSurface, GRContext flushContext, IDrawingFactory factory)
		=> new SkiaPresentSession(ownedSurface, flushContext, ownsSurface: true, factory);

	/// <summary>Wraps a renderer-cached GPU SKSurface (e.g. the Vulkan render image); present flushes+submits the
	/// GRContext but does NOT dispose the surface (the renderer reuses it until the image/size changes).</summary>
	public static SkiaPresentSession ForCachedGpuSurface(SKSurface cachedSurface, GRContext flushContext, IDrawingFactory factory)
		=> new SkiaPresentSession(cachedSurface, flushContext, ownsSurface: false, factory);

	/// <summary>Composes into a backend-owned offscreen blitted onto <paramref name="swapchain"/> at present, so a
	/// host whose swapchain discards its contents still carries the previous frame forward.</summary>
	public static SkiaPresentSession ForRetainedLayer(SKSurface layer, SKSurface swapchain, GRContext flushContext, IDrawingFactory factory)
		=> new SkiaPresentSession(layer, flushContext, ownsSurface: false, factory, swapchain);

	public bool PreservesContents => _presentTo is not null;

	// Restore any state the composition (frame replay + overlay) left behind, flush the composition surface, then
	// submit the GPU.
	public void Dispose()
	{
		Canvas.RestoreToCount(_saveCount);
		Canvas.Flush();
		if (_presentTo is { } swapchain)
		{
			_surface!.Draw(swapchain.Canvas, 0, 0, null);
			swapchain.Canvas.Flush();
		}

		// Submit, not just flush: the host's present runs on its own command buffer and can otherwise blit the
		// texture before Skia's recorded work has been sent to the GPU.
		_flushContext?.Flush(submit: true);
		if (_ownsSurface) { _surface?.Dispose(); }
	}
}
