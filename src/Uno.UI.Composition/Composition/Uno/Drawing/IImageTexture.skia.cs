#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A backend-specific, GPU-resident form of an image (a wgpu texture, an <c>SKImage</c>, a GL texture), created
/// once from a neutral <see cref="IImage"/>'s pixels by the device-bound <see cref="IDrawingFactory"/>. Unlike
/// <see cref="IImage"/> (neutral decoded pixels), this is opaque and cast back by the producing backend when
/// drawn. Its lifetime is owned by the framework (the composition resource that holds the image) and released
/// deterministically via <see cref="IDisposable.Dispose"/> — no GC-driven texture caching.
/// </summary>
public interface IImageTexture : IDisposable
{
	int PixelWidth { get; }

	int PixelHeight { get; }

	/// <summary>
	/// Neutral pixel readback (BGRA8888 premultiplied, tightly packed), used only as a cross-backend fallback:
	/// when a session whose backend didn't create this texture needs to draw it (e.g. a Skia RenderTargetBitmap
	/// pass in a WebGPU app), it reads the pixels and materializes its own resource. The matched backend never
	/// calls this — it casts the texture to its concrete type.
	/// </summary>
	void CopyPixels(Span<byte> destination);
}
