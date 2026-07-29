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

	public SkiaPresentSession(SKCanvas canvas) : base(canvas)
		=> _saveCount = canvas.Save();

	private SkiaPresentSession(SKSurface ownedSurface) : base(ownedSurface.Canvas)
	{
		_ownedSurface = ownedSurface;
		_saveCount = ownedSurface.Canvas.Save();
	}

	/// <summary>Wraps a neutral CPU framebuffer as an owned SKSurface to compose into (disposed on present).</summary>
	public static SkiaPresentSession ForSoftware(ISoftwareRenderTarget target)
	{
		var info = new SKImageInfo(target.Width, target.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		return new SkiaPresentSession(SKSurface.Create(info, target.Pixels, target.RowBytes));
	}

	// Restore any state the composition (frame replay + overlay) left behind, then flush the result to the surface.
	public void Dispose()
	{
		Canvas.RestoreToCount(_saveCount);
		Canvas.Flush();
		_ownedSurface?.Dispose();
	}
}
