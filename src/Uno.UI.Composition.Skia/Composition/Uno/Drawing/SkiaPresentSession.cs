#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IPresentSession"/>; disposing flushes the composed frame to the surface.</summary>
internal sealed class SkiaPresentSession : SkiaDrawingSession, IPresentSession
{
	private readonly int _saveCount;
	// The SKSurface this session composes into; disposed on present only when this session owns it (_ownsSurface).
	// The host-canvas case borrows an SKCanvas (no surface); the retained cases compose into a backend-cached surface.
	private readonly SKSurface? _surface;
	private readonly bool _ownsSurface;
	// Non-null for a GPU-texture present (Metal/Vulkan): the GRContext to submit on present so the render lands
	// in the texture/image before the host commits/blits it. The GRContext itself is cached by the renderer.
	private readonly GRContext? _flushContext;
	// Non-null for the retained-GPU-offscreen case: the swapchain framebuffer to blit the composed offscreen into on
	// present (the offscreen — `_surface` here, not owned — keeps the previous frame's pixels for partial repaint).
	private readonly SKSurface? _blitTarget;
	// True when the blit target is a per-frame surface (e.g. a Metal drawable) this session must dispose on present.
	private readonly bool _ownsBlitTarget;

	// Retained-software case: compose into a backend-cached CPU offscreen (`_surface`, keeps the previous frame's
	// pixels), then read it back into the host's framebuffer on present. The offscreen is the source of truth; the
	// host buffer need not persist since it is overwritten in full each present.
	private readonly bool _isSoftwareReadback;
	private readonly nint _readbackDstPixels;
	private readonly int _readbackDstRowBytes;
	private readonly SKImageInfo _readbackDstInfo;

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

	private SkiaPresentSession(SKSurface offscreen, ISoftwareRenderTarget dest, SKImageInfo dstInfo) : base(offscreen.Canvas)
	{
		_surface = offscreen;
		_ownsSurface = false;   // cached by the factory across frames
		_isSoftwareReadback = true;
		_readbackDstPixels = dest.Pixels;
		_readbackDstRowBytes = dest.RowBytes;
		_readbackDstInfo = dstInfo;
		PreservesContents = true;
		_saveCount = offscreen.Canvas.Save();
	}

	/// <summary>Composes into a persistent backend-cached CPU <paramref name="offscreen"/> (retains the previous
	/// frame's pixels, so partial repaint is valid), then on present reads it back in full into the host's neutral
	/// framebuffer (<paramref name="target"/>). The offscreen is not disposed here (the backend reuses it until the
	/// size changes); the host then blits its framebuffer to the window.</summary>
	public static SkiaPresentSession ForRetainedSoftware(SKSurface offscreen, ISoftwareRenderTarget target)
	{
		var colorType = target.ColorFormat == GraphicsColorFormat.Rgba8888 ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
		var info = new SKImageInfo(target.Width, target.Height, colorType, SKAlphaType.Premul);
		return new SkiaPresentSession(offscreen, target, info);
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
	/// <paramref name="framebuffer"/> and flushes. The offscreen is never disposed here (cached by the backend until
	/// the size changes); the framebuffer is disposed only when <paramref name="ownsFramebuffer"/> (a per-frame
	/// drawable). The host swaps the framebuffer afterwards.</summary>
	public static SkiaPresentSession ForRetainedGpuOffscreen(SKSurface offscreen, SKSurface framebuffer, GRContext flushContext, bool ownsFramebuffer = false)
		=> new SkiaPresentSession(offscreen, flushContext, ownsSurface: false, preservesContents: true, blitTarget: framebuffer, ownsBlitTarget: ownsFramebuffer);

	// Restore any state the composition (frame replay + overlay) left behind, then flush the result to the surface.
	public void Dispose()
	{
		Canvas.RestoreToCount(_saveCount);
		Canvas.Flush();
		if (_isSoftwareReadback && _surface is { } offscreen)
		{
			// Copy the (mostly retained) CPU offscreen into the host's framebuffer.
			offscreen.ReadPixels(_readbackDstInfo, _readbackDstPixels, _readbackDstRowBytes, 0, 0);
		}
		else if (_blitTarget is { } framebuffer && _surface is { } gpuOffscreen)
		{
			// Copy the (mostly retained) GPU offscreen onto the swapchain framebuffer, then flush so the blit lands
			// before the host swaps.
			framebuffer.Canvas.DrawSurface(gpuOffscreen, 0, 0);
			framebuffer.Canvas.Flush();
		}
		_flushContext?.Flush();
		if (_ownsSurface) { _surface?.Dispose(); }
		if (_ownsBlitTarget) { _blitTarget?.Dispose(); }
	}
}
