#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IPresentSession"/>; disposing flushes the composed frame to the surface.</summary>
internal sealed class SkiaPresentSession : SkiaDrawingSession, IPresentSession
{
	private readonly int _saveCount;
	// The SKSurface this session composes into; disposed on present only when this session owns it (_ownsSurface).
	// The host-canvas case borrows an SKCanvas (no surface); the Vulkan case borrows a surface the renderer caches.
	private readonly SKSurface? _surface;
	private readonly bool _ownsSurface;
	// Non-null for a GPU-texture present (Metal/Vulkan): the GRContext to submit on present so the render lands
	// in the texture/image before the host commits/blits it. The GRContext itself is cached by the renderer.
	private readonly GRContext? _flushContext;

	public SkiaPresentSession(SKCanvas canvas) : base(canvas)
		=> _saveCount = canvas.Save();

	private SkiaPresentSession(SKSurface surface, GRContext? flushContext, bool ownsSurface) : base(surface.Canvas)
	{
		_surface = surface;
		_flushContext = flushContext;
		_ownsSurface = ownsSurface;
		_saveCount = surface.Canvas.Save();
	}

	/// <summary>Wraps the host's neutral CPU framebuffer as an owned SKSurface to compose into (disposed on present).</summary>
	public static SkiaPresentSession ForSoftware(ISoftwareRenderTarget target)
	{
		var colorType = target.ColorFormat == GraphicsColorFormat.Rgba8888 ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
		var info = new SKImageInfo(target.Width, target.Height, colorType, SKAlphaType.Premul);
		return new SkiaPresentSession(SKSurface.Create(info, target.Pixels, target.RowBytes), flushContext: null, ownsSurface: true);
	}

	/// <summary>Wraps a per-frame GPU-texture SKSurface (e.g. Metal) the session owns; present flushes+submits the GRContext.</summary>
	public static SkiaPresentSession ForGpuTexture(SKSurface ownedSurface, GRContext flushContext)
		=> new SkiaPresentSession(ownedSurface, flushContext, ownsSurface: true);

	/// <summary>Wraps a renderer-cached GPU SKSurface (e.g. the Vulkan render image); present flushes+submits the
	/// GRContext but does NOT dispose the surface (the renderer reuses it until the image/size changes).</summary>
	public static SkiaPresentSession ForCachedGpuSurface(SKSurface cachedSurface, GRContext flushContext)
		=> new SkiaPresentSession(cachedSurface, flushContext, ownsSurface: false);

	// Restore any state the composition (frame replay + overlay) left behind, then flush the result to the surface.
	public void Dispose()
	{
		Canvas.RestoreToCount(_saveCount);
		Canvas.Flush();
		_flushContext?.Flush();
		if (_ownsSurface) { _surface?.Dispose(); }
	}
}
