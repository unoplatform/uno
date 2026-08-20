#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageDecoder
{
	private static bool TryDecodePng(byte[] d, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;

		var p = 8; // skip signature
		int width = 0, height = 0, bitDepth = 0, colorType = 0, interlace = 0;
		byte[]? palette = null;   // RGB triplets
		byte[]? paletteAlpha = null;
		using var idat = new MemoryStream();
		var sawHeader = false;

		while (p + 8 <= d.Length)
		{
			var rawLength = ReadU32(d, p);
			var type = ReadU32(d, p + 4);
			var chunk = p + 8;
			// Reject a chunk whose data + CRC overruns the buffer. Reading the length unsigned and bounds-checking
			// also guarantees the loop makes forward progress: a crafted huge length can no longer wrap `p`.
			if (rawLength > int.MaxValue || (long)chunk + rawLength + 4 > d.Length)
			{
				return false;
			}
			var length = (int)rawLength;
			p = chunk + length + 4; // skip data + CRC

			switch (type)
			{
				case 0x49484452: // IHDR
					width = (int)ReadU32(d, chunk);
					height = (int)ReadU32(d, chunk + 4);
					bitDepth = d[chunk + 8];
					colorType = d[chunk + 9];
					interlace = d[chunk + 12];
					sawHeader = true;
					break;
				case 0x504C5445: // PLTE
					palette = new byte[length];
					Array.Copy(d, chunk, palette, 0, length);
					break;
				case 0x74524E53: // tRNS
					paletteAlpha = new byte[length];
					Array.Copy(d, chunk, paletteAlpha, 0, length);
					break;
				case 0x49444154: // IDAT
					idat.Write(d, chunk, length);
					break;
				case 0x49454E44: // IEND
					p = d.Length;
					break;
			}
		}

		if (!sawHeader || ExceedsPixelCap(width, height) || interlace is not (0 or 1))
		{
			return false;
		}

		var channels = colorType switch
		{
			0 => 1, // grayscale
			2 => 3, // RGB
			3 => 1, // palette index
			4 => 2, // grayscale + alpha
			6 => 4, // RGBA
			_ => 0,
		};
		if (channels == 0 || (bitDepth != 8 && bitDepth != 16 && !(colorType is 0 or 3 && bitDepth is 1 or 2 or 4)))
		{
			return false;
		}

		idat.Position = 0;
		using var inflate = new ZLibStream(idat, CompressionMode.Decompress);
		var bitsPerPixel = channels * bitDepth;
		var bytesPerPixel = Math.Max(1, bitsPerPixel / 8);
		var bgra = new byte[width * height * 4];

		if (interlace == 0)
		{
			var stride = (width * bitsPerPixel + 7) / 8;
			var raw = new byte[(stride + 1) * height];
			ReadExactly(inflate, raw);
			DecodePass(raw, 0, width, height, 0, 0, 1, 1, bgra, width, bitsPerPixel, bytesPerPixel, bitDepth, colorType, palette, paletteAlpha);
		}
		else
		{
			// Adam7: (startX, startY, stepX, stepY) for the 7 passes.
			ReadOnlySpan<int> passes = stackalloc int[]
			{
				0, 0, 8, 8,  4, 0, 8, 8,  0, 4, 4, 8,  2, 0, 4, 4,  0, 2, 2, 4,  1, 0, 2, 2,  0, 1, 1, 2,
			};

			var total = 0;
			for (var i = 0; i < 7; i++)
			{
				var (pw, ph) = PassSize(width, height, passes[i * 4], passes[i * 4 + 1], passes[i * 4 + 2], passes[i * 4 + 3]);
				if (pw > 0 && ph > 0)
				{
					total += (((pw * bitsPerPixel + 7) / 8) + 1) * ph;
				}
			}

			var raw = new byte[total];
			ReadExactly(inflate, raw);

			var offset = 0;
			for (var i = 0; i < 7; i++)
			{
				int startX = passes[i * 4], startY = passes[i * 4 + 1], stepX = passes[i * 4 + 2], stepY = passes[i * 4 + 3];
				var (pw, ph) = PassSize(width, height, startX, startY, stepX, stepY);
				if (pw > 0 && ph > 0)
				{
					offset = DecodePass(raw, offset, pw, ph, startX, startY, stepX, stepY, bgra, width, bitsPerPixel, bytesPerPixel, bitDepth, colorType, palette, paletteAlpha);
				}
			}
		}

		decoded = new DecodedImage(width, height, new[] { bgra }, DecodedImage.SingleFrameDurations);
		return true;
	}

	private static (int width, int height) PassSize(int width, int height, int startX, int startY, int stepX, int stepY)
		=> (width > startX ? (width - startX + stepX - 1) / stepX : 0, height > startY ? (height - startY + stepY - 1) / stepY : 0);

	private static int DecodePass(byte[] raw, int offset, int passW, int passH, int startX, int startY, int stepX, int stepY,
		byte[] bgra, int width, int bitsPerPixel, int bytesPerPixel, int bitDepth, int colorType, byte[]? palette, byte[]? paletteAlpha)
	{
		var stride = (passW * bitsPerPixel + 7) / 8;
		var previous = new byte[stride];
		var current = new byte[stride];
		for (var py = 0; py < passH; py++)
		{
			var filter = raw[offset++];
			Array.Copy(raw, offset, current, 0, stride);
			offset += stride;
			Unfilter(filter, current, previous, bytesPerPixel);

			var outY = startY + py * stepY;
			for (var px = 0; px < passW; px++)
			{
				var outX = startX + px * stepX;
				EmitPixel(current, px, bgra, (outY * width + outX) * 4, bitDepth, colorType, palette, paletteAlpha);
			}

			(previous, current) = (current, previous);
		}

		return offset;
	}

	private static void Unfilter(byte filter, byte[] cur, byte[] prev, int bpp)
	{
		switch (filter)
		{
			case 1: // Sub
				for (var i = bpp; i < cur.Length; i++)
				{
					cur[i] = (byte)(cur[i] + cur[i - bpp]);
				}
				break;
			case 2: // Up
				for (var i = 0; i < cur.Length; i++)
				{
					cur[i] = (byte)(cur[i] + prev[i]);
				}
				break;
			case 3: // Average
				for (var i = 0; i < cur.Length; i++)
				{
					var a = i >= bpp ? cur[i - bpp] : 0;
					cur[i] = (byte)(cur[i] + (a + prev[i]) / 2);
				}
				break;
			case 4: // Paeth
				for (var i = 0; i < cur.Length; i++)
				{
					var a = i >= bpp ? cur[i - bpp] : 0;
					var b = prev[i];
					var c = i >= bpp ? prev[i - bpp] : 0;
					cur[i] = (byte)(cur[i] + Paeth(a, b, c));
				}
				break;
		}
	}

	private static int Paeth(int a, int b, int c)
	{
		var pp = a + b - c;
		var pa = Math.Abs(pp - a);
		var pb = Math.Abs(pp - b);
		var pc = Math.Abs(pp - c);
		return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
	}

	private static void EmitPixel(byte[] line, int x, byte[] bgra, int outOffset, int bitDepth, int colorType, byte[]? palette, byte[]? paletteAlpha)
	{
		byte r, g, b, a = 255;
		switch (colorType)
		{
			case 0: // grayscale
				r = g = b = SampleChannel(line, x, 0, 1, bitDepth);
				break;
			case 2: // RGB (8/16-bit)
				r = SampleChannel(line, x, 0, 3, bitDepth);
				g = SampleChannel(line, x, 1, 3, bitDepth);
				b = SampleChannel(line, x, 2, 3, bitDepth);
				break;
			case 3: // palette
				{
					var index = ReadIndex(line, x, bitDepth);
					r = palette![index * 3];
					g = palette[index * 3 + 1];
					b = palette[index * 3 + 2];
					a = paletteAlpha is not null && index < paletteAlpha.Length ? paletteAlpha[index] : (byte)255;
					break;
				}
			case 4: // grayscale + alpha
				r = g = b = SampleChannel(line, x, 0, 2, bitDepth);
				a = SampleChannel(line, x, 1, 2, bitDepth);
				break;
			default: // 6: RGBA
				r = SampleChannel(line, x, 0, 4, bitDepth);
				g = SampleChannel(line, x, 1, 4, bitDepth);
				b = SampleChannel(line, x, 2, 4, bitDepth);
				a = SampleChannel(line, x, 3, 4, bitDepth);
				break;
		}

		SetPixelPremul(bgra, outOffset, r, g, b, a);
	}

	private static byte SampleChannel(byte[] line, int x, int channel, int channels, int bitDepth)
	{
		if (bitDepth == 8)
		{
			return line[x * channels + channel];
		}

		if (bitDepth == 16)
		{
			return line[(x * channels + channel) * 2]; // high byte
		}

		// Sub-byte grayscale (1/2/4) — scale to 0..255.
		var value = ReadSubByte(line, x * channels + channel, bitDepth);
		var max = (1 << bitDepth) - 1;
		return (byte)(value * 255 / max);
	}

	private static int ReadIndex(byte[] line, int x, int bitDepth) =>
		bitDepth == 8 ? line[x] : ReadSubByte(line, x, bitDepth);

	private static int ReadSubByte(byte[] line, int sampleIndex, int bitDepth)
	{
		var samplesPerByte = 8 / bitDepth;
		var b = line[sampleIndex / samplesPerByte];
		var withinByte = sampleIndex % samplesPerByte;
		var shift = 8 - bitDepth * (withinByte + 1);
		var mask = (1 << bitDepth) - 1;
		return (b >> shift) & mask;
	}

	private static void ReadExactly(Stream stream, byte[] buffer)
	{
		var read = 0;
		while (read < buffer.Length)
		{
			var n = stream.Read(buffer, read, buffer.Length - read);
			if (n <= 0)
			{
				break; // IncompleteInput — leave the rest zero-filled.
			}

			read += n;
		}
	}

	private static uint ReadU32(byte[] d, int o) => ((uint)d[o] << 24) | ((uint)d[o + 1] << 16) | ((uint)d[o + 2] << 8) | d[o + 3];
}
