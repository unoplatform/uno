#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IImage"/> wrapping an <see cref="SKImage"/>.</summary>
internal sealed class SkiaImage : IImage
{
	public SkiaImage(SKImage image) => Image = image;

	public SKImage Image { get; }

	public int PixelWidth => Image.Width;

	public int PixelHeight => Image.Height;
}
