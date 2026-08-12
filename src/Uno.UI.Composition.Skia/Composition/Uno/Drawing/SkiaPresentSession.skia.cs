#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IPresentSession"/>; disposing flushes the composed frame to the surface.</summary>
internal sealed class SkiaPresentSession : SkiaDrawingSession, IPresentSession
{
	private readonly int _saveCount;
	// Non-null only when this session owns the surface (the CPU-framebuffer wrap); the host-canvas case
	// borrows an SKCanvas the host owns and must not dispose.
	private readonly SKSurface? _ownedSurface;
	// Non-null for a GPU-texture present (e.g. Metal): the GRContext to submit on present so the render lands
	// in the texture before the host commits the drawable. The GRContext itself is cached by the renderer.
	private readonly GRContext? _flushContext;

	public SkiaPresentSession(SKCanvas canvas) : base(canvas)
		=> _saveCount = canvas.Save();

	private SkiaPresentSession(SKSurface ownedSurface, GRContext? flushContext = null) : base(ownedSurface.Canvas)
	{
		_ownedSurface = ownedSurface;
		_flushContext = flushContext;
		_saveCount = ownedSurface.Canvas.Save();
	}

	/// <summary>Wraps a neutral CPU framebuffer as an owned SKSurface to compose into (disposed on present).</summary>
	public static SkiaPresentSession ForSoftware(ISoftwareRenderTarget target)
	{
		var colorType = target.ColorFormat == GraphicsColorFormat.Rgba8888 ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
		var info = new SKImageInfo(target.Width, target.Height, colorType, SKAlphaType.Premul);
		return new SkiaPresentSession(SKSurface.Create(info, target.Pixels, target.RowBytes));
	}

	/// <summary>Wraps a GPU-texture SKSurface (e.g. Metal) the session owns; present flushes+submits the GRContext.</summary>
	public static SkiaPresentSession ForGpuTexture(SKSurface ownedSurface, GRContext flushContext)
		=> new SkiaPresentSession(ownedSurface, flushContext);

	// Restore any state the composition (frame replay + overlay) left behind, then flush the result to the surface.
	public void Dispose()
	{
		Canvas.RestoreToCount(_saveCount);
		Canvas.Flush();
		_flushContext?.Flush();
		_ownedSurface?.Dispose();
	}
}
