#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="ITexture"/>: an owned <see cref="SKImage"/>, uploaded from neutral pixels or
/// wrapping a <see cref="SkiaImage"/>'s pixels.</summary>
internal sealed class SkiaTexture : DrawingResource, ITexture
{
	public SkiaTexture(SKImage image) => Image = image;

	public SKImage Image { get; }

	public int PixelWidth => Image.Width;

	public int PixelHeight => Image.Height;

	protected override void Free() => Image.Dispose();

	/// <summary>A texture over <paramref name="source"/>'s pixels, without copying them; null when they can't be
	/// read in place.</summary>
	public static SkiaTexture? TryWrap(SkiaImage source)
	{
		using var pixmap = source.Image.PeekPixels();
		if (pixmap is null)
		{
			return null;
		}

		// The wrapper holds a reference on the source until Skia releases the pixels, which it does once, when the
		// last native reference goes: this texture's Free, a recorded picture that drew it, or its finalizer.
		source.AddRef();
		var image = SKImage.FromPixels(pixmap, static (_, context) => ((SkiaImage)context!).Release(), source);
		if (image is null)
		{
			// Skia doesn't call the release proc for a pixmap it rejects.
			source.Release();
			return null;
		}

		return new SkiaTexture(image);
	}
}
