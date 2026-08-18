#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IPresentSession"/>; disposing flushes the composed frame to the surface.</summary>
internal sealed class SkiaPresentSession : SkiaDrawingSession, IPresentSession
{
	private readonly int _saveCount;
	// The SKSurface this session composes into, disposed on present only when owned (_ownsSurface). In the retained
	// case it is instead the swapchain surface the layer blits onto (composition targets _retainedLayer's canvas).
	private readonly SKSurface? _surface;
	private readonly bool _ownsSurface;
	// Non-null for a GPU-texture present (Metal/Vulkan): the GRContext to submit on present so the render lands
	// in the texture/image before the host commits/blits it. The GRContext itself is cached by the renderer.
	private readonly GRContext? _flushContext;
	// Non-null when the frame is composed into a persistent retained layer (GL / Metal partial repaint); disposing
	// blits the layer onto _surface (the swapchain) instead of flushing the composition surface directly.
	private readonly RetainedLayer? _retainedLayer;

	public SkiaPresentSession(SKCanvas canvas, IDrawingFactory factory) : base(canvas, factory)
		=> _saveCount = canvas.Save();

	private SkiaPresentSession(SKSurface surface, GRContext? flushContext, bool ownsSurface, IDrawingFactory factory) : base(surface.Canvas, factory)
	{
		_surface = surface;
		_flushContext = flushContext;
		_ownsSurface = ownsSurface;
		_saveCount = surface.Canvas.Save();
	}

	private SkiaPresentSession(RetainedLayer layer, SKSurface swapchainSurface, GRContext? flushContext, bool ownsSwapchainSurface, IDrawingFactory factory) : base(layer.Surface!.Canvas, factory)
	{
		_retainedLayer = layer;
		_surface = swapchainSurface;
		_flushContext = flushContext;
		_ownsSurface = ownsSwapchainSurface;
		_saveCount = layer.Surface!.Canvas.Save();
	}

	/// <summary>Wraps the host's neutral CPU framebuffer as an owned SKSurface to compose into (disposed on present).</summary>
	public static SkiaPresentSession ForSoftware(ISoftwareRenderTarget target, IDrawingFactory factory)
	{
		var colorType = target.ColorFormat == GraphicsColorFormat.Rgba8888 ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
		var info = new SKImageInfo(target.Width, target.Height, colorType, SKAlphaType.Premul);
		return new SkiaPresentSession(SKSurface.Create(info, target.Pixels, target.RowBytes), flushContext: null, ownsSurface: true, factory);
	}

	/// <summary>Wraps a per-frame GPU-texture SKSurface (e.g. Metal) the session owns; present flushes+submits the GRContext.</summary>
	public static SkiaPresentSession ForGpuTexture(SKSurface ownedSurface, GRContext flushContext, IDrawingFactory factory)
		=> new SkiaPresentSession(ownedSurface, flushContext, ownsSurface: true, factory);

	/// <summary>Wraps a renderer-cached GPU SKSurface (e.g. the Vulkan render image); present flushes+submits the
	/// GRContext but does NOT dispose the surface (the renderer reuses it until the image/size changes).</summary>
	public static SkiaPresentSession ForCachedGpuSurface(SKSurface cachedSurface, GRContext flushContext, IDrawingFactory factory)
		=> new SkiaPresentSession(cachedSurface, flushContext, ownsSurface: false, factory);

	/// <summary>Composes the frame into a persistent <see cref="RetainedLayer"/> (so only the damaged region is
	/// redrawn), then on present blits the layer onto <paramref name="swapchainSurface"/> and submits the GPU.
	/// <paramref name="ownsSwapchainSurface"/> disposes a per-frame swapchain surface (e.g. a Metal drawable).</summary>
	public static SkiaPresentSession ForRetained(RetainedLayer layer, SKSurface swapchainSurface, GRContext? flushContext, bool ownsSwapchainSurface, IDrawingFactory factory)
		=> new SkiaPresentSession(layer, swapchainSurface, flushContext, ownsSwapchainSurface, factory);

	// Restore any state the composition (frame replay + overlay) left behind, then finalize: blit the retained layer
	// onto the swapchain (retained case) or flush the composition surface directly, then submit the GPU.
	public void Dispose()
	{
		Canvas.RestoreToCount(_saveCount);
		if (_retainedLayer is { } layer)
		{
			layer.Present(_surface!);
		}
		else
		{
			Canvas.Flush();
		}
		_flushContext?.Flush();
		if (_ownsSurface) { _surface?.Dispose(); }
	}
}
