#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageDecoder
{
	private static bool TryDecodeBmp(byte[] d, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;

		var pixelOffset = (int)ReadU32LE(d, 10);
		var dibSize = (int)ReadU32LE(d, 14);
		var width = (int)ReadU32LE(d, 18);
		var rawHeight = (int)ReadU32LE(d, 22);
		var topDown = rawHeight < 0;
		var height = Math.Abs(rawHeight);
		var bpp = ReadU16LE(d, 28);
		var compression = (int)ReadU32LE(d, 30);

		if (ExceedsPixelCap(width, height) || compression != 0 || bpp is not (24 or 32 or 8))
		{
			return false; // only uncompressed 8/24/32-bit BMPs (within the pixel cap); the rest fall back to the codec
		}

		byte[]? palette = null;
		if (bpp == 8)
		{
			var colorsUsed = (int)ReadU32LE(d, 46);
			if (colorsUsed == 0)
			{
				colorsUsed = 256;
			}

			var paletteOffset = 14 + dibSize;
			palette = new byte[colorsUsed * 4]; // BGRA (X)
			Array.Copy(d, paletteOffset, palette, 0, Math.Min(palette.Length, d.Length - paletteOffset));
		}

		var bytesPerPixel = bpp / 8;
		var stride = (width * bytesPerPixel + 3) & ~3; // rows padded to 4 bytes
		var bgra = new byte[width * height * 4];

		for (var row = 0; row < height; row++)
		{
			var srcRow = topDown ? row : height - 1 - row;
			var src = pixelOffset + srcRow * stride;
			var dst = row * width * 4;
			for (var x = 0; x < width; x++)
			{
				byte r, g, b, a = 255;
				if (bpp == 8)
				{
					var index = d[src + x] * 4;
					b = palette![index];
					g = palette[index + 1];
					r = palette[index + 2];
				}
				else
				{
					var o = src + x * bytesPerPixel;
					b = d[o];
					g = d[o + 1];
					r = d[o + 2];
					if (bpp == 32)
					{
						a = d[o + 3];
					}
				}

				SetPixelPremul(bgra, dst + x * 4, r, g, b, a);
			}
		}

		// 32-bit BI_RGB with an all-zero alpha channel means "no alpha" — treat as opaque.
		if (bpp == 32 && IsAlphaAllZero(bgra))
		{
			ForceOpaque(bgra);
		}

		decoded = new DecodedImage(width, height, new[] { bgra }, DecodedImage.SingleFrameDurations);
		return true;
	}

	private static bool IsAlphaAllZero(byte[] bgra)
	{
		for (var i = 3; i < bgra.Length; i += 4)
		{
			if (bgra[i] != 0)
			{
				return false;
			}
		}

		return true;
	}

	private static void ForceOpaque(byte[] bgra)
	{
		for (var i = 3; i < bgra.Length; i += 4)
		{
			bgra[i] = 255;
		}
	}

	private static uint ReadU32LE(byte[] d, int o) => (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24));
	private static int ReadU16LE(byte[] d, int o) => d[o] | (d[o + 1] << 8);
}
