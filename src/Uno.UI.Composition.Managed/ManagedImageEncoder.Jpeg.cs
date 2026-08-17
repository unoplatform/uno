#nullable enable

using System;
using System.IO;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageEncoder
{
	// Baseline JPEG (SOF0), 4:4:4 (no chroma subsampling — the simplest layout: one 8x8 block per component per MCU).
	// RGB→YCbCr, float FDCT, quality-scaled standard (Annex K) quantization, standard Annex K Huffman tables. Alpha is
	// discarded (JPEG has none). Byte-for-byte compatible with what ManagedImageDecoder.Jpeg reads back.
	private static byte[] EncodeJpeg(byte[] rgba, int width, int height, int quality)
	{
		quality = Math.Clamp(quality, 1, 100);
		var scale = quality < 50 ? 5000 / quality : 200 - quality * 2;

		var lumaQuant = ScaleQuantZigZag(StdLumaQuant, scale);
		var chromaQuant = ScaleQuantZigZag(StdChromaQuant, scale);

		var (y, cb, cr) = RgbToYCbCr(rgba, width, height);

		var (dcLumaCode, dcLumaSize) = BuildHuffTable(DcLumaBits, DcLumaValues);
		var (acLumaCode, acLumaSize) = BuildHuffTable(AcLumaBits, AcLumaValues);
		var (dcChromaCode, dcChromaSize) = BuildHuffTable(DcChromaBits, DcChromaValues);
		var (acChromaCode, acChromaSize) = BuildHuffTable(AcChromaBits, AcChromaValues);

		using var ms = new MemoryStream();
		WriteJpegHeaders(ms, width, height, lumaQuant, chromaQuant);

		var writer = new JpegBitWriter(ms);
		var pY = 0;
		var pCb = 0;
		var pCr = 0;
		var mcuCols = (width + 7) / 8;
		var mcuRows = (height + 7) / 8;
		Span<double> block = stackalloc double[64];
		var zz = new int[64];

		for (var my = 0; my < mcuRows; my++)
		{
			for (var mx = 0; mx < mcuCols; mx++)
			{
				var px = mx * 8;
				var py = my * 8;

				ExtractBlock(y, width, height, px, py, block);
				FdctQuantize(block, lumaQuant, zz);
				EncodeBlockEntropy(writer, zz, ref pY, dcLumaCode, dcLumaSize, acLumaCode, acLumaSize);

				ExtractBlock(cb, width, height, px, py, block);
				FdctQuantize(block, chromaQuant, zz);
				EncodeBlockEntropy(writer, zz, ref pCb, dcChromaCode, dcChromaSize, acChromaCode, acChromaSize);

				ExtractBlock(cr, width, height, px, py, block);
				FdctQuantize(block, chromaQuant, zz);
				EncodeBlockEntropy(writer, zz, ref pCr, dcChromaCode, dcChromaSize, acChromaCode, acChromaSize);
			}
		}

		writer.Flush();
		ms.WriteByte(0xFF);
		ms.WriteByte(0xD9); // EOI
		return ms.ToArray();
	}

	private static (byte[] y, byte[] cb, byte[] cr) RgbToYCbCr(byte[] rgba, int width, int height)
	{
		var count = width * height;
		var y = new byte[count];
		var cb = new byte[count];
		var cr = new byte[count];
		for (var i = 0; i < count; i++)
		{
			double r = rgba[i * 4];
			double g = rgba[i * 4 + 1];
			double b = rgba[i * 4 + 2];
			y[i] = ClampByte(0.299 * r + 0.587 * g + 0.114 * b);
			cb[i] = ClampByte(-0.168736 * r - 0.331264 * g + 0.5 * b + 128);
			cr[i] = ClampByte(0.5 * r - 0.418688 * g - 0.081312 * b + 128);
		}

		return (y, cb, cr);
	}

	// 8x8 samples for the block at (px,py), level-shifted by -128; edges are clamped (replicated) for partial blocks.
	private static void ExtractBlock(byte[] plane, int width, int height, int px, int py, Span<double> block)
	{
		for (var yy = 0; yy < 8; yy++)
		{
			var sy = Math.Min(py + yy, height - 1);
			for (var xx = 0; xx < 8; xx++)
			{
				var sx = Math.Min(px + xx, width - 1);
				block[yy * 8 + xx] = plane[sy * width + sx] - 128.0;
			}
		}
	}

	private static readonly double[,] _fdctCos = BuildFdctCos();

	private static double[,] BuildFdctCos()
	{
		var t = new double[8, 8];
		for (var freq = 0; freq < 8; freq++)
		{
			for (var spatial = 0; spatial < 8; spatial++)
			{
				t[freq, spatial] = Math.Cos((2 * spatial + 1) * freq * Math.PI / 16);
			}
		}

		return t;
	}

	// Separable float FDCT, then quantize into zig-zag order (index k → natural position ZigZag[k]).
	private static void FdctQuantize(Span<double> block, int[] quantZigZag, int[] zz)
	{
		Span<double> tmp = stackalloc double[64];
		for (var row = 0; row < 8; row++)
		{
			for (var u = 0; u < 8; u++)
			{
				var sum = 0.0;
				for (var x = 0; x < 8; x++)
				{
					sum += block[row * 8 + x] * _fdctCos[u, x];
				}

				tmp[row * 8 + u] = sum;
			}
		}

		Span<double> coef = stackalloc double[64];
		for (var v = 0; v < 8; v++)
		{
			var cv = v == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
			for (var u = 0; u < 8; u++)
			{
				var sum = 0.0;
				for (var row = 0; row < 8; row++)
				{
					sum += tmp[row * 8 + u] * _fdctCos[v, row];
				}

				var cu = u == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
				coef[v * 8 + u] = cu * cv * sum / 4.0;
			}
		}

		for (var k = 0; k < 64; k++)
		{
			var natural = ZigZag[k];
			zz[k] = (int)Math.Round(coef[natural] / quantZigZag[k], MidpointRounding.AwayFromZero);
		}
	}

	private static void EncodeBlockEntropy(JpegBitWriter w, int[] zz, ref int pred, int[] dcCode, int[] dcSize, int[] acCode, int[] acSize)
	{
		var diff = Math.Clamp(zz[0] - pred, -2047, 2047); // DC category ∈ 0..11
		pred += diff;

		var s = BitLength(diff);
		w.WriteBits(dcCode[s], dcSize[s]);
		if (s > 0)
		{
			w.WriteBits(Extend(diff, s), s);
		}

		var run = 0;
		for (var k = 1; k < 64; k++)
		{
			// AC magnitude clamped to category ≤10 so the (run,size) symbol is always defined by the standard table.
			var coef = Math.Clamp(zz[k], -1023, 1023);
			if (coef == 0)
			{
				run++;
				continue;
			}

			while (run > 15)
			{
				w.WriteBits(acCode[0xF0], acSize[0xF0]); // ZRL
				run -= 16;
			}

			var size = BitLength(coef);
			var rs = (run << 4) | size;
			w.WriteBits(acCode[rs], acSize[rs]);
			w.WriteBits(Extend(coef, size), size);
			run = 0;
		}

		if (run > 0)
		{
			w.WriteBits(acCode[0x00], acSize[0x00]); // EOB
		}
	}

	private static int BitLength(int v)
	{
		v = Math.Abs(v);
		var n = 0;
		while (v > 0)
		{
			n++;
			v >>= 1;
		}

		return n;
	}

	// The `size`-bit magnitude representation JpegBitReader.ReceiveExtend inverts.
	private static int Extend(int v, int size) => v >= 0 ? v : v + (1 << size) - 1;

	private static void WriteJpegHeaders(Stream ms, int width, int height, int[] lumaQuant, int[] chromaQuant)
	{
		ms.WriteByte(0xFF);
		ms.WriteByte(0xD8); // SOI

		// APP0 / JFIF
		WriteMarker(ms, 0xE0, new byte[] { 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00 });

		WriteDqt(ms, 0, lumaQuant);
		WriteDqt(ms, 1, chromaQuant);

		// SOF0: 8-bit precision, 3 components, all 4:4:4 (H=V=1); Y→quant 0, Cb/Cr→quant 1.
		var sof = new byte[]
		{
			8,
			(byte)(height >> 8), (byte)height,
			(byte)(width >> 8), (byte)width,
			3,
			1, 0x11, 0,
			2, 0x11, 1,
			3, 0x11, 1,
		};
		WriteMarker(ms, 0xC0, sof);

		WriteDht(ms, 0x00, DcLumaBits, DcLumaValues);
		WriteDht(ms, 0x10, AcLumaBits, AcLumaValues);
		WriteDht(ms, 0x01, DcChromaBits, DcChromaValues);
		WriteDht(ms, 0x11, AcChromaBits, AcChromaValues);

		// SOS: 3 components; Y→DC0/AC0, Cb/Cr→DC1/AC1; Ss=0, Se=63, Ah=Al=0.
		var sos = new byte[] { 3, 1, 0x00, 2, 0x11, 3, 0x11, 0, 63, 0 };
		WriteMarker(ms, 0xDA, sos);
	}

	private static void WriteMarker(Stream ms, byte marker, byte[] payload)
	{
		ms.WriteByte(0xFF);
		ms.WriteByte(marker);
		var len = payload.Length + 2;
		ms.WriteByte((byte)(len >> 8));
		ms.WriteByte((byte)len);
		ms.Write(payload, 0, payload.Length);
	}

	private static void WriteDqt(Stream ms, int id, int[] quantZigZag)
	{
		var payload = new byte[1 + 64];
		payload[0] = (byte)id; // 8-bit precision (Pq=0), table id Tq
		for (var k = 0; k < 64; k++)
		{
			payload[1 + k] = (byte)Math.Clamp(quantZigZag[k], 1, 255);
		}

		WriteMarker(ms, 0xDB, payload);
	}

	private static void WriteDht(Stream ms, int id, byte[] bits, byte[] values)
	{
		var payload = new byte[1 + 16 + values.Length];
		payload[0] = (byte)id; // Tc (class) high nibble, Th (table id) low nibble
		Array.Copy(bits, 0, payload, 1, 16);
		Array.Copy(values, 0, payload, 17, values.Length);
		WriteMarker(ms, 0xC4, payload);
	}

	// Standard libjpeg quality scaling, emitted in zig-zag order (matching ManagedImageDecoder.Jpeg's qt[k] indexing).
	private static int[] ScaleQuantZigZag(int[] naturalTable, int scale)
	{
		var result = new int[64];
		for (var k = 0; k < 64; k++)
		{
			var value = (naturalTable[ZigZag[k]] * scale + 50) / 100;
			result[k] = Math.Clamp(value, 1, 255);
		}

		return result;
	}

	private static (int[] codes, int[] sizes) BuildHuffTable(byte[] bits, byte[] values)
	{
		var codes = new int[256];
		var sizes = new int[256];
		var code = 0;
		var k = 0;
		for (var len = 1; len <= 16; len++)
		{
			for (var i = 0; i < bits[len - 1]; i++)
			{
				var sym = values[k++];
				codes[sym] = code;
				sizes[sym] = len;
				code++;
			}

			code <<= 1;
		}

		return (codes, sizes);
	}

	private static byte ClampByte(double v) => (byte)Math.Clamp((int)Math.Round(v), 0, 255);

	// Single source of truth lives on the paired decoder (same assembly); avoid a second drifting copy.
	private static readonly int[] ZigZag = ManagedImageDecoder.ZigZag;

	private static readonly int[] StdLumaQuant =
	{
		16, 11, 10, 16, 24, 40, 51, 61,
		12, 12, 14, 19, 26, 58, 60, 55,
		14, 13, 16, 24, 40, 57, 69, 56,
		14, 17, 22, 29, 51, 87, 80, 62,
		18, 22, 37, 56, 68, 109, 103, 77,
		24, 35, 55, 64, 81, 104, 113, 92,
		49, 64, 78, 87, 103, 121, 120, 101,
		72, 92, 95, 98, 112, 100, 103, 99,
	};

	private static readonly int[] StdChromaQuant =
	{
		17, 18, 24, 47, 99, 99, 99, 99,
		18, 21, 26, 66, 99, 99, 99, 99,
		24, 26, 56, 99, 99, 99, 99, 99,
		47, 66, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
	};

	private static readonly byte[] DcLumaBits = { 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };
	private static readonly byte[] DcLumaValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

	private static readonly byte[] DcChromaBits = { 0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 };
	private static readonly byte[] DcChromaValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

	private static readonly byte[] AcLumaBits = { 0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7D };
	private static readonly byte[] AcLumaValues =
	{
		0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12,
		0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
		0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
		0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
		0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16,
		0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
		0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
		0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
		0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
		0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
		0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
		0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
		0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98,
		0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
		0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6,
		0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
		0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4,
		0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
		0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA,
		0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
		0xF9, 0xFA,
	};

	private static readonly byte[] AcChromaBits = { 0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77 };
	private static readonly byte[] AcChromaValues =
	{
		0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21,
		0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
		0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91,
		0xA1, 0xB1, 0xC1, 0x09, 0x23, 0x33, 0x52, 0xF0,
		0x15, 0x62, 0x72, 0xD1, 0x0A, 0x16, 0x24, 0x34,
		0xE1, 0x25, 0xF1, 0x17, 0x18, 0x19, 0x1A, 0x26,
		0x27, 0x28, 0x29, 0x2A, 0x35, 0x36, 0x37, 0x38,
		0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
		0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
		0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
		0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
		0x79, 0x7A, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
		0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96,
		0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5,
		0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4,
		0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3,
		0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2,
		0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
		0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9,
		0xEA, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
		0xF9, 0xFA,
	};

	private sealed class JpegBitWriter
	{
		private readonly Stream _stream;
		private int _buffer;
		private int _count;

		public JpegBitWriter(Stream stream) => _stream = stream;

		public void WriteBits(int value, int length)
		{
			for (var i = length - 1; i >= 0; i--)
			{
				_buffer = (_buffer << 1) | ((value >> i) & 1);
				if (++_count == 8)
				{
					EmitByte(_buffer & 0xFF);
					_buffer = 0;
					_count = 0;
				}
			}
		}

		public void Flush()
		{
			if (_count > 0)
			{
				_buffer = (_buffer << (8 - _count)) | ((1 << (8 - _count)) - 1); // pad with 1-bits
				EmitByte(_buffer & 0xFF);
				_buffer = 0;
				_count = 0;
			}
		}

		private void EmitByte(int b)
		{
			_stream.WriteByte((byte)b);
			if (b == 0xFF)
			{
				_stream.WriteByte(0x00); // byte stuffing
			}
		}
	}
}
