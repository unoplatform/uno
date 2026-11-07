#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A backend-specific, GPU-resident form of an image (a wgpu texture, an <c>SKImage</c>, a GL texture), created
/// once from a neutral <see cref="IImage"/>'s pixels by the <see cref="IDrawingFactory"/>. Opaque and cast back by
/// the producing backend when drawn. Its lifetime is framework-owned and released deterministically via
/// <see cref="IDisposable.Dispose"/> — no GC-driven texture caching.
/// </summary>
public interface ITexture : IDrawingResource
{
	int PixelWidth { get; }

	int PixelHeight { get; }

	/// <summary>
	/// True when this texture draws the pixels of the <see cref="IImage"/> it was created from, through the reference
	/// it holds on that image (see <see cref="IDrawingFactory.CreateTexture(IImage)"/>), rather than pixels of its own.
	/// Uno then counts those pixels once, as the image's, toward the memory undisplayed images may keep
	/// (<c>FeatureConfiguration.Image.MaxUndisplayedImageBytes</c>), so a texture that holds a copy and returns true leaves
	/// the copy uncounted. False by default.
	/// </summary>
	bool SharesImagePixels => false;
}
