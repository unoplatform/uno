#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A SkiaSharp-free image decoder: parses the common encoded formats (PNG, GIF incl. animation, BMP, baseline JPEG)
/// straight into BGRA-premultiplied pixel frames. Format parsing is entirely managed; the backend turns the pixels
/// into drawable <see cref="IImage"/> handles. Driven by <see cref="ManagedImageDecoderBackend"/> — the
/// <see cref="IImageEncoderDecoder"/> an app registers as <see cref="ImageEncoderDecoder.Current"/> for SkiaSharp-free decoding.
/// Unsupported inputs return false.
/// </summary>
internal static partial class ManagedImageDecoder
{
	public static bool TryDecode(byte[] data, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;
		try
		{
			if (!TryDecodeCore(data, out var native))
			{
				return false;
			}

			decoded = ScaleToTargetIfNeeded(native, targetWidth, targetHeight);
			return true;
		}
		catch
		{
			// Any malformed input falls back to the Skia codec.
			decoded = null;
			return false;
		}
	}

	// Upper bound on decoded pixel count, mirroring the Skia codec's guard. Header-declared dimensions drive large
	// allocations before any pixel data is seen, so a tiny crafted header (e.g. 16384x16384) must be rejected here
	// rather than being allowed to force a multi-gigabyte allocation (an OOM crash on 32-bit WASM).
	private const long MaxPixels = 1L << 28;

	internal static bool ExceedsPixelCap(int width, int height)
		=> width <= 0 || height <= 0 || (long)width * height > MaxPixels;

	private static bool TryDecodeCore(byte[] d, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;
		if (d.Length >= 8 && d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47)
		{
			return TryDecodePng(d, out decoded);
		}

		if (d.Length >= 6 && d[0] == (byte)'G' && d[1] == (byte)'I' && d[2] == (byte)'F')
		{
			return TryDecodeGif(d, out decoded);
		}

		if (d.Length >= 2 && d[0] == (byte)'B' && d[1] == (byte)'M')
		{
			return TryDecodeBmp(d, out decoded);
		}

		if (d.Length >= 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF)
		{
			return TryDecodeJpeg(d, out decoded);
		}

		if (d.Length >= 12 && d[0] == (byte)'R' && d[1] == (byte)'I' && d[2] == (byte)'F' && d[3] == (byte)'F'
			&& d[8] == (byte)'W' && d[9] == (byte)'E' && d[10] == (byte)'B' && d[11] == (byte)'P')
		{
			return TryDecodeWebp(d, out decoded);
		}

		return false;
	}

	/// <summary>Bilinear resample of every frame to the requested target size (a memory optimization, honored like the Skia path).</summary>
	private static DecodedImage ScaleToTargetIfNeeded(DecodedImage source, int? targetWidth, int? targetHeight)
	{
		if (targetWidth is <= 0) targetWidth = null;
		if (targetHeight is <= 0) targetHeight = null;
		if (targetWidth is null && targetHeight is null)
		{
			return source;
		}

		int dstW, dstH;
		if (targetWidth is > 0 && targetHeight is > 0)
		{
			dstW = targetWidth.Value;
			dstH = targetHeight.Value;
		}
		else if (targetWidth is > 0)
		{
			dstW = targetWidth.Value;
			dstH = (int)Math.Max(1, (long)source.Height * dstW / source.Width);
		}
		else
		{
			dstH = targetHeight!.Value;
			dstW = (int)Math.Max(1, (long)source.Width * dstH / source.Height);
		}

		if (dstW == source.Width && dstH == source.Height)
		{
			return source;
		}

		var frames = new byte[source.Frames.Length][];
		for (var i = 0; i < frames.Length; i++)
		{
			frames[i] = ResampleBilinear(source.Frames[i], source.Width, source.Height, dstW, dstH);
		}

		return new DecodedImage(dstW, dstH, frames, source.DurationsMs);
	}

	private static byte[] ResampleBilinear(byte[] src, int srcW, int srcH, int dstW, int dstH)
	{
		var dst = new byte[dstW * dstH * 4];
		var scaleX = (double)srcW / dstW;
		var scaleY = (double)srcH / dstH;
		for (var y = 0; y < dstH; y++)
		{
			var sy = Math.Min(srcH - 1, (y + 0.5) * scaleY - 0.5);
			if (sy < 0) sy = 0;
			var y0 = (int)sy;
			var y1 = Math.Min(srcH - 1, y0 + 1);
			var fy = sy - y0;
			for (var x = 0; x < dstW; x++)
			{
				var sx = Math.Min(srcW - 1, (x + 0.5) * scaleX - 0.5);
				if (sx < 0) sx = 0;
				var x0 = (int)sx;
				var x1 = Math.Min(srcW - 1, x0 + 1);
				var fx = sx - x0;

				var i00 = (y0 * srcW + x0) * 4;
				var i01 = (y0 * srcW + x1) * 4;
				var i10 = (y1 * srcW + x0) * 4;
				var i11 = (y1 * srcW + x1) * 4;
				var o = (y * dstW + x) * 4;
				for (var c = 0; c < 4; c++)
				{
					var top = src[i00 + c] * (1 - fx) + src[i01 + c] * fx;
					var bottom = src[i10 + c] * (1 - fx) + src[i11 + c] * fx;
					dst[o + c] = (byte)Math.Clamp(top * (1 - fy) + bottom * fy + 0.5, 0, 255);
				}
			}
		}

		return dst;
	}

	// BGRA premultiplied straight from straight-alpha RGBA channels.
	internal static void SetPixelPremul(byte[] dst, int offset, byte r, byte g, byte b, byte a)
	{
		if (a == 255)
		{
			dst[offset] = b;
			dst[offset + 1] = g;
			dst[offset + 2] = r;
			dst[offset + 3] = 255;
		}
		else
		{
			dst[offset] = (byte)(b * a / 255);
			dst[offset + 1] = (byte)(g * a / 255);
			dst[offset + 2] = (byte)(r * a / 255);
			dst[offset + 3] = a;
		}
	}
}

/// <summary>Managed decode result: one or more BGRA-premultiplied frames (row-major, top-down) plus their durations.</summary>
internal sealed class DecodedImage
{
	public DecodedImage(int width, int height, byte[][] frames, int[] durationsMs)
	{
		Width = width;
		Height = height;
		Frames = frames;
		DurationsMs = durationsMs;
	}

	public int Width { get; }
	public int Height { get; }
	public byte[][] Frames { get; }
	public int[] DurationsMs { get; }
}
