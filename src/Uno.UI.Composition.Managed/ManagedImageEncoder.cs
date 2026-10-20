#nullable enable

using System;
using System.IO;
using Windows.Graphics.Imaging;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-free image ENCODER — the inverse of <see cref="ManagedImageDecoder"/>. Turns a raw pixel buffer into
/// a compressed image written to a stream for <see cref="ImageEncoderDecoder.Encode"/> (behind <c>BitmapEncoder</c>).
/// Per-format encoders live in the <c>ManagedImageEncoder.&lt;Format&gt;.cs</c> partials; this file owns the dispatch
/// and the input normalization (any <see cref="BitmapPixelFormat"/>/<see cref="BitmapAlphaMode"/> → straight,
/// non-premultiplied 8-bit RGBA rows, top-down) that every encoder consumes.
/// </summary>
internal static partial class ManagedImageEncoder
{
	public static void Encode(Stream destination, byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, BitmapEncoderFormat format, int quality)
	{
		if (width <= 0 || height <= 0)
		{
			throw new ArgumentException("Image dimensions must be positive.");
		}

		var rgba = NormalizeToStraightRgba8(pixels, width, height, pixelFormat, alphaMode);

		var bytes = format switch
		{
			BitmapEncoderFormat.Bmp => EncodeBmp(rgba, width, height),
			BitmapEncoderFormat.Png => EncodePng(rgba, width, height),
			BitmapEncoderFormat.Jpeg => EncodeJpeg(rgba, width, height, quality),
			BitmapEncoderFormat.Gif => EncodeGif(rgba, width, height),
			BitmapEncoderFormat.Heif => throw new NotSupportedException(
				"HEIF encoding requires an HEVC encoder and is not available in the managed image codec."),
			_ => throw new NotSupportedException($"Unknown encoder format {format}.")
		};

		destination.Write(bytes, 0, bytes.Length);
	}

	/// <summary>
	/// Converts <paramref name="pixels"/> (in <paramref name="pixelFormat"/>/<paramref name="alphaMode"/>) into
	/// tightly-packed, top-down, straight (un-premultiplied) 8-bit RGBA — 4 bytes/pixel, R,G,B,A order — the single
	/// input shape the per-format encoders take.
	/// </summary>
	private static byte[] NormalizeToStraightRgba8(byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode)
	{
		var count = checked(width * height);
		var rgba = new byte[checked(count * 4)];

		// Extract R,G,B,A per pixel from the source format.
		switch (pixelFormat)
		{
			case BitmapPixelFormat.Bgra8:
				for (int i = 0, s = 0; i < count; i++, s += 4)
				{
					rgba[i * 4 + 0] = pixels[s + 2];
					rgba[i * 4 + 1] = pixels[s + 1];
					rgba[i * 4 + 2] = pixels[s + 0];
					rgba[i * 4 + 3] = pixels[s + 3];
				}
				break;
			case BitmapPixelFormat.Rgba8:
				Array.Copy(pixels, rgba, Math.Min(pixels.Length, rgba.Length));
				break;
			case BitmapPixelFormat.Gray8:
				for (int i = 0; i < count; i++)
				{
					var g = pixels[i];
					rgba[i * 4 + 0] = g;
					rgba[i * 4 + 1] = g;
					rgba[i * 4 + 2] = g;
					rgba[i * 4 + 3] = 255;
				}
				break;
			case BitmapPixelFormat.Rgba16:
				// 16-bit little-endian per channel → take the high byte.
				for (int i = 0, s = 0; i < count; i++, s += 8)
				{
					rgba[i * 4 + 0] = pixels[s + 1];
					rgba[i * 4 + 1] = pixels[s + 3];
					rgba[i * 4 + 2] = pixels[s + 5];
					rgba[i * 4 + 3] = pixels[s + 7];
				}
				break;
			default:
				throw new NotSupportedException($"Unsupported source pixel format {pixelFormat}.");
		}

		// Un-premultiply if the source alpha was premultiplied; Ignore → force opaque.
		switch (alphaMode)
		{
			case BitmapAlphaMode.Premultiplied:
				for (int i = 0; i < count; i++)
				{
					var a = rgba[i * 4 + 3];
					if (a is not 0 and not 255)
					{
						rgba[i * 4 + 0] = (byte)Math.Min(255, rgba[i * 4 + 0] * 255 / a);
						rgba[i * 4 + 1] = (byte)Math.Min(255, rgba[i * 4 + 1] * 255 / a);
						rgba[i * 4 + 2] = (byte)Math.Min(255, rgba[i * 4 + 2] * 255 / a);
					}
				}
				break;
			case BitmapAlphaMode.Ignore:
				for (int i = 0; i < count; i++)
				{
					rgba[i * 4 + 3] = 255;
				}
				break;
			case BitmapAlphaMode.Straight:
			default:
				break;
		}

		return rgba;
	}
}
