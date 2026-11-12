#nullable enable

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Uno.UI.RuntimeTests.Helpers;

/// <summary>Encodes solid-color PNGs without depending on a drawing backend.</summary>
internal static class TestPngEncoder
{
	private static readonly uint[] _crcTable = CreateCrcTable();

	internal static byte[] CreateSolidPng(int width, int height, Windows.UI.Color color)
	{
		using var output = new MemoryStream();
		output.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

		var header = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header, width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
		header[8] = 8; // bit depth
		header[9] = 6; // RGBA
		WriteChunk(output, "IHDR", header);

		var row = new byte[1 + width * 4]; // filter byte 0, then pixels
		for (var x = 0; x < width; x++)
		{
			row[1 + x * 4] = color.R;
			row[2 + x * 4] = color.G;
			row[3 + x * 4] = color.B;
			row[4 + x * 4] = color.A;
		}

		using (var compressed = new MemoryStream())
		{
			using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
			{
				for (var y = 0; y < height; y++)
				{
					zlib.Write(row);
				}
			}
			WriteChunk(output, "IDAT", compressed.ToArray());
		}

		WriteChunk(output, "IEND", []);
		return output.ToArray();
	}

	private static void WriteChunk(Stream output, string type, byte[] data)
	{
		Span<byte> buffer = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(buffer, data.Length);
		output.Write(buffer);

		var typeBytes = Encoding.ASCII.GetBytes(type);
		output.Write(typeBytes);
		output.Write(data);

		var crc = UpdateCrc(UpdateCrc(0xFFFFFFFFu, typeBytes), data) ^ 0xFFFFFFFFu;
		BinaryPrimitives.WriteUInt32BigEndian(buffer, crc);
		output.Write(buffer);
	}

	private static uint UpdateCrc(uint crc, byte[] data)
	{
		foreach (var b in data)
		{
			crc = _crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
		}

		return crc;
	}

	private static uint[] CreateCrcTable()
	{
		var table = new uint[256];
		for (uint n = 0; n < 256; n++)
		{
			var c = n;
			for (var k = 0; k < 8; k++)
			{
				c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
			}
			table[n] = c;
		}

		return table;
	}
}
