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
	// Non-null for the retained-offscreen case: the swapchain framebuffer to blit the composed offscreen into on
	// present (the offscreen — `_surface` here, not owned — keeps the previous frame's pixels for partial repaint).
	private readonly SKSurface? _blitTarget;
	// True when the blit target is a per-frame surface (e.g. a Metal drawable) this session must dispose on present.
	private readonly bool _ownsBlitTarget;

	public bool PreservesContents { get; }

	public SkiaPresentSession(SKCanvas canvas) : base(canvas)
		=> _saveCount = canvas.Save();

	private SkiaPresentSession(SKSurface surface, GRContext? flushContext, bool ownsSurface, bool preservesContents = false, SKSurface? blitTarget = null, bool ownsBlitTarget = false) : base(surface.Canvas)
	{
		_surface = surface;
		_flushContext = flushContext;
		_ownsSurface = ownsSurface;
		_blitTarget = blitTarget;
		_ownsBlitTarget = ownsBlitTarget;
		PreservesContents = preservesContents;
		_saveCount = surface.Canvas.Save();
	}

	/// <summary>Wraps a neutral CPU framebuffer as an owned SKSurface to compose into (disposed on present). Reports
	/// <paramref name="preservesContents"/> so the compositor can repaint only the damaged region when the host
	/// reuses one persistent buffer across frames.</summary>
	public static SkiaPresentSession ForSoftware(ISoftwareRenderTarget target)
	{
		var colorType = target.ColorFormat == GraphicsColorFormat.Rgba8888 ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
		var info = new SKImageInfo(target.Width, target.Height, colorType, SKAlphaType.Premul);
		return new SkiaPresentSession(SKSurface.Create(info, target.Pixels, target.RowBytes), flushContext: null, ownsSurface: true, preservesContents: target.PreservesContents);
	}

	/// <summary>Wraps a per-frame GPU-texture SKSurface (e.g. Metal) the session owns; present flushes+submits the GRContext.</summary>
	public static SkiaPresentSession ForGpuTexture(SKSurface ownedSurface, GRContext flushContext)
		=> new SkiaPresentSession(ownedSurface, flushContext, ownsSurface: true);

	/// <summary>Wraps a renderer-cached GPU SKSurface (e.g. the Vulkan render image); present flushes+submits the
	/// GRContext but does NOT dispose the surface (the renderer reuses it until the image/size changes). Because the
	/// surface is stable across frames (the host blits it whole to the swapchain each present), it keeps the previous
	/// frame's pixels, so the compositor may repaint only the damaged region.</summary>
	public static SkiaPresentSession ForCachedGpuSurface(SKSurface cachedSurface, GRContext flushContext)
		=> new SkiaPresentSession(cachedSurface, flushContext, ownsSurface: false, preservesContents: true);

	/// <summary>Composes into a persistent, backend-cached GPU <paramref name="offscreen"/> (retains the previous
	/// frame's pixels, so partial repaint is valid), then on present blits the whole offscreen onto the swapchain
	/// <paramref name="framebuffer"/> and flushes. Neither surface is disposed here (both are cached by the backend
	/// until the size changes). The host swaps the framebuffer afterwards.</summary>
	public static SkiaPresentSession ForRetainedGpuOffscreen(SKSurface offscreen, SKSurface framebuffer, GRContext flushContext, bool ownsFramebuffer = false)
		=> new SkiaPresentSession(offscreen, flushContext, ownsSurface: false, preservesContents: true, blitTarget: framebuffer, ownsBlitTarget: ownsFramebuffer);

	// Restore any state the composition (frame replay + overlay) left behind, then flush the result to the surface.
	public void Dispose()
	{
		Canvas.RestoreToCount(_saveCount);
		Canvas.Flush();
		if (_blitTarget is { } framebuffer && _surface is { } offscreen)
		{
			// Copy the (mostly retained) offscreen onto the swapchain framebuffer, then flush so the blit lands
			// before the host swaps.
			framebuffer.Canvas.DrawSurface(offscreen, 0, 0);
			framebuffer.Canvas.Flush();
		}
		_flushContext?.Flush();
		if (_ownsSurface) { _surface?.Dispose(); }
		if (_ownsBlitTarget) { _blitTarget?.Dispose(); }
	}
}
