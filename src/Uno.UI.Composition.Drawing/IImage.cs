#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-created bitmap/image handle. Lifetime is owned by whatever produced it (an image
/// surface, a decode, a snapshot), so this handle is not itself disposable.
/// </summary>
public interface IImage
{
	int PixelWidth { get; }
	int PixelHeight { get; }

	/// <summary>
	/// Copies the image's pixels into <paramref name="destination"/> as BGRA8888 premultiplied, tightly packed
	/// (<see cref="PixelWidth"/> × <see cref="PixelHeight"/> × 4 bytes) — the neutral readback a backend uses to
	/// upload the image to its own GPU texture.
	/// </summary>
	void CopyPixels(Span<byte> destination);
}
