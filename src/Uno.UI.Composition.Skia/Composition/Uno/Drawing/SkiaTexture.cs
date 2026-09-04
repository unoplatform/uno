#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="ITexture"/> — an owned <see cref="SKImage"/> uploaded from neutral pixels.</summary>
internal sealed class SkiaTexture : DrawingResource, ITexture
{
	public SkiaTexture(SKImage image) => Image = image;

	public SKImage Image { get; }

	public int PixelWidth => Image.Width;

	public int PixelHeight => Image.Height;

	protected override void Free() => Image.Dispose();
}
