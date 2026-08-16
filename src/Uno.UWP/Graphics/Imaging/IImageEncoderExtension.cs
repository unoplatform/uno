#nullable enable

namespace Windows.Graphics.Imaging;

/// <summary>Backend-neutral output formats a <see cref="BitmapEncoder"/> can target.</summary>
public enum BitmapEncoderFormat
{
	Bmp,
	Gif,
	Jpeg,
	Png,
	Heif,
}

/// <summary>
/// Render-backend seam for encoding raw pixels to a compressed image. The generic <see cref="BitmapEncoder"/>
/// resolves the implementation through <see cref="Uno.Foundation.Extensibility.ApiExtensibility"/> so it stays
/// free of any imaging library; the drawing backend provides the concrete encoder.
/// </summary>
public interface IImageEncoderExtension
{
	/// <summary>Encodes the given premultiplication-tagged pixel buffer to <paramref name="format"/>.</summary>
	byte[] Encode(byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, BitmapEncoderFormat format, int quality);
}
