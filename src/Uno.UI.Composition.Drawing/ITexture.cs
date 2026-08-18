#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A backend-specific, GPU-resident form of an image (a wgpu texture, an <c>SKImage</c>, a GL texture), created
/// once from a neutral <see cref="IImage"/>'s pixels by the <see cref="IDrawingFactory"/>. Opaque and cast back by
/// the producing backend when drawn. Its lifetime is framework-owned and released deterministically via
/// <see cref="IDisposable.Dispose"/> — no GC-driven texture caching.
/// </summary>
public interface ITexture : IDisposable
{
	int PixelWidth { get; }

	int PixelHeight { get; }

	/// <summary>
	/// Neutral pixel readback (BGRA8888 premultiplied, tightly packed), used only as a cross-backend fallback: a
	/// session whose backend didn't create this texture reads the pixels and materializes its own resource. The
	/// matched backend never calls this — it casts the texture to its concrete type.
	/// </summary>
	void CopyPixels(Span<byte> destination);
}
