#nullable enable

using System;
using System.IO;
using System.IO.Compression;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageEncoder
{
	// 8-bit RGBA, color type 6, no interlace. Scanlines use filter 0 (None) — simple and lossless; the compressor
	// still gets most of the win. Matches what ManagedImageDecoder.Png reads back.
	private static byte[] EncodePng(byte[] rgba, int width, int height)
	{
		var raw = new byte[checked(height * (1 + width * 4))];
		for (int y = 0, o = 0; y < height; y++)
		{
			raw[o++] = 0;   // filter: None
			Array.Copy(rgba, y * width * 4, raw, o, width * 4);
			o += width * 4;
		}

		var idat = ZlibCompress(raw);

		using var ms = new MemoryStream();
		ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);   // PNG signature

		var ihdr = new byte[13];
		WriteU32BE(ihdr, 0, (uint)width);
		WriteU32BE(ihdr, 4, (uint)height);
		ihdr[8] = 8;    // bit depth
		ihdr[9] = 6;    // color type: RGBA
		ihdr[10] = 0;   // compression
		ihdr[11] = 0;   // filter
		ihdr[12] = 0;   // interlace
		WriteChunk(ms, "IHDR", ihdr);
		WriteChunk(ms, "IDAT", idat);
		WriteChunk(ms, "IEND", Array.Empty<byte>());

		return ms.ToArray();
	}

	private static void WriteChunk(Stream s, string type, byte[] data)
	{
		var len = new byte[4];
		WriteU32BE(len, 0, (uint)data.Length);
		s.Write(len, 0, 4);

		var typeBytes = new byte[4];
		for (int i = 0; i < 4; i++) { typeBytes[i] = (byte)type[i]; }
		s.Write(typeBytes, 0, 4);
		s.Write(data, 0, data.Length);

		uint crc = Crc32(typeBytes, 0xFFFFFFFF);
		crc = Crc32(data, crc);
		var crcBytes = new byte[4];
		WriteU32BE(crcBytes, 0, crc ^ 0xFFFFFFFF);
		s.Write(crcBytes, 0, 4);
	}

	// zlib stream: 2-byte header + raw DEFLATE + 4-byte big-endian Adler-32.
	private static byte[] ZlibCompress(byte[] data)
	{
		using var ms = new MemoryStream();
		ms.WriteByte(0x78);   // CMF: 32K window, deflate
		ms.WriteByte(0x01);   // FLG: no dict, fastest (checkbits make 0x7801 a valid header)
		using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
		{
			deflate.Write(data, 0, data.Length);
		}
		var adler = new byte[4];
		WriteU32BE(adler, 0, Adler32(data));
		ms.Write(adler, 0, 4);
		return ms.ToArray();
	}

	private static uint Adler32(byte[] data)
	{
		const uint mod = 65521;
		uint a = 1, b = 0;
		foreach (var v in data)
		{
			a = (a + v) % mod;
			b = (b + a) % mod;
		}
		return (b << 16) | a;
	}

	private static uint[]? _crcTable;

	private static uint Crc32(byte[] data, uint crc)
	{
		var table = _crcTable ??= BuildCrcTable();
		foreach (var v in data)
		{
			crc = table[(crc ^ v) & 0xFF] ^ (crc >> 8);
		}
		return crc;
	}

	private static uint[] BuildCrcTable()
	{
		var t = new uint[256];
		for (uint n = 0; n < 256; n++)
		{
			var c = n;
			for (int k = 0; k < 8; k++)
			{
				c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
			}
			t[n] = c;
		}
		return t;
	}

	private static void WriteU32BE(byte[] b, int o, uint v)
	{
		b[o] = (byte)(v >> 24);
		b[o + 1] = (byte)(v >> 16);
		b[o + 2] = (byte)(v >> 8);
		b[o + 3] = (byte)v;
	}
}
