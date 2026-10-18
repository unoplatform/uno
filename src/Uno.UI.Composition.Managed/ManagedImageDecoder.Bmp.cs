#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageDecoder
{
	/// <summary>What a BMP's file and DIB headers declare.</summary>
	private struct BmpHeader
	{
		public int Width, Height, Bpp, DibSize, PixelOffset, Stride;
		public bool TopDown, CoreHeader;
		public uint AlphaMask;
	}

	/// <summary>Reads a BMP's headers and applies every refusal <see cref="TryDecodeBmp"/> makes: all of them are here.</summary>
	private static bool TryReadBmpHeader(ImageBytes d, out BmpHeader header)
	{
		header = default;

		if (d.Length < 26)
		{
			return false;
		}

		header.PixelOffset = (int)ReadU32LE(d, 10);
		var dibSize = header.DibSize = (int)ReadU32LE(d, 14);

		// BITMAPCOREHEADER (OS/2 v1) stores 16-bit dimensions, has no compression field, and uses 3-byte
		// palette entries; reading it with the BITMAPINFOHEADER layout yields garbage.
		var coreHeader = header.CoreHeader = dibSize == 12;

		int width, height, bpp, compression;
		if (coreHeader)
		{
			width = ReadU16LE(d, 18);
			height = ReadU16LE(d, 20);
			bpp = ReadU16LE(d, 24);
			compression = 0;
		}
		else
		{
			if (d.Length < 34)
			{
				return false;
			}

			width = (int)ReadU32LE(d, 18);
			var rawHeight = (int)ReadU32LE(d, 22);
			header.TopDown = rawHeight < 0;
			height = rawHeight == int.MinValue ? 0 : Math.Abs(rawHeight);
			bpp = ReadU16LE(d, 28);
			compression = (int)ReadU32LE(d, 30);
		}

		// BI_BITFIELDS channel masks — and the alpha mask a BITMAPV4/V5 header carries — are explicit statements
		// about the fourth byte, so they are read before deciding anything about alpha.
		uint redMask = 0, greenMask = 0, blueMask = 0, alphaMask = 0;
		if (!coreHeader && compression == 3 && d.Length >= 66)
		{
			redMask = ReadU32LE(d, 54);
			greenMask = ReadU32LE(d, 58);
			blueMask = ReadU32LE(d, 62);
			if (dibSize >= 108 && d.Length >= 70)
			{
				alphaMask = ReadU32LE(d, 66);
			}
		}

		var bitfieldsBgra = compression == 3 && bpp == 32
			&& redMask == 0x00FF0000 && greenMask == 0x0000FF00 && blueMask == 0x000000FF
			&& alphaMask is 0 or 0xFF000000;

		if (ExceedsPixelCap(width, height) || bpp is not (24 or 32 or 8) || (compression != 0 && !bitfieldsBgra))
		{
			return false; // only uncompressed 8/24/32-bit BMPs (BGRA bit-fields included), within the pixel cap
		}

		var stride = (width * (bpp / 8) + 3) & ~3; // rows padded to 4 bytes
		if (header.PixelOffset < 0 || (long)header.PixelOffset + (long)stride * height > d.Length)
		{
			return false;
		}

		header.Width = width;
		header.Height = height;
		header.Bpp = bpp;
		header.Stride = stride;
		header.AlphaMask = alphaMask;
		return true;
	}

	private static bool TryDecodeBmp(byte[] d, ImageBytes bytes, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;
		if (!TryReadBmpHeader(bytes, out var header))
		{
			return false;
		}

		int width = header.Width, height = header.Height, bpp = header.Bpp, pixelOffset = header.PixelOffset, stride = header.Stride;
		var coreHeader = header.CoreHeader;
		var topDown = header.TopDown;
		var alphaMask = header.AlphaMask;

		byte[]? palette = null; // BGR triples, always 256 entries so an out-of-range index can't read past the end
		if (bpp == 8)
		{
			var entrySize = coreHeader ? 3 : 4;
			var colorsUsed = coreHeader || d.Length < 50 ? 256 : (int)ReadU32LE(bytes, 46);
			if (colorsUsed is <= 0 or > 256)
			{
				colorsUsed = 256;
			}

			var paletteOffset = 14 + header.DibSize;
			palette = new byte[256 * 3];
			for (var i = 0; i < colorsUsed; i++)
			{
				var p = paletteOffset + i * entrySize;
				if (p < 0 || p + 3 > d.Length)
				{
					break;
				}

				palette[i * 3] = d[p];
				palette[i * 3 + 1] = d[p + 1];
				palette[i * 3 + 2] = d[p + 2];
			}
		}

		var bytesPerPixel = bpp / 8;

		// An explicit alpha mask says the fourth byte IS alpha (that is what our own encoder writes), so it wins.
		// Only 32-bit BI_RGB leaves the byte undefined — most writers (MS Paint, a BitBlt capture) leave it at 0 —
		// and there the bytes are all we have: premultiplying an all-zero alpha destroys the colour, and restoring
		// A = 255 afterwards can only produce black.
		var ignoreAlpha = bpp != 32
			|| (alphaMask != 0xFF000000 && IsSourceAlphaAllZero(d, pixelOffset, stride, width, height));
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
					var index = d[src + x] * 3;
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
					if (!ignoreAlpha)
					{
						a = d[o + 3];
					}
				}

				SetPixelPremul(bgra, dst + x * 4, r, g, b, a);
			}
		}

		decoded = new DecodedImage(width, height, new[] { bgra }, DecodedImage.SingleFrameDurations);
		return true;
	}

	private static bool IsSourceAlphaAllZero(byte[] d, int pixelOffset, int stride, int width, int height)
	{
		for (var row = 0; row < height; row++)
		{
			var o = pixelOffset + row * stride + 3;
			for (var x = 0; x < width; x++, o += 4)
			{
				if (d[o] != 0)
				{
					return false;
				}
			}
		}

		return true;
	}
}
