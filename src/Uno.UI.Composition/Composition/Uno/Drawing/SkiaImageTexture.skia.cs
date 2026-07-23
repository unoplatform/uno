#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IImageTexture"/> — an owned <see cref="SKImage"/> uploaded from neutral pixels.</summary>
internal sealed class SkiaImageTexture : IImageTexture
{
	public SkiaImageTexture(SKImage image) => Image = image;

	public SKImage Image { get; }

	public int PixelWidth => Image.Width;

	public int PixelHeight => Image.Height;

	public void Dispose() => Image.Dispose();
}
