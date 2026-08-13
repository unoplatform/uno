#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A CPU-framebuffer <see cref="IRenderTarget"/> (BGRA8888, premultiplied) that a software
/// <see cref="IGraphicsContext"/> hands over from <see cref="IGraphicsContext.AcquireRenderTarget"/>: the
/// matched backend wraps <see cref="Pixels"/> / <see cref="IRenderTarget.Width"/> / <see cref="RowBytes"/> into
/// its own surface, with no GPU-library type crossing the boundary. The buffer is owned by the context (valid
/// until the next acquire/present cycle).
/// </summary>
public interface ISoftwareRenderTarget : IRenderTarget
{
	/// <summary>Pointer to the top-left pixel of the framebuffer.</summary>
	nint Pixels { get; }

	/// <summary>Bytes per row (stride) of the framebuffer.</summary>
	int RowBytes { get; }
}
