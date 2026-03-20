using System;
using System.IO;

namespace Uno.UI.Runtime.Skia.Win32;

// PNG encoding helper methods for icon-to-bitmap conversion
internal partial class Win32DragDropExtension
{
	private static void WriteIhdrChunk(MemoryStream stream, int width, int height)
	{
		using var chunkData = new MemoryStream();

		// Width (4 bytes, big-endian)
		chunkData.Write(ToBigEndian(width), 0, 4);
		// Height (4 bytes, big-endian)
		chunkData.Write(ToBigEndian(height), 0, 4);
		// Bit depth (1 byte) - 8 bits per channel
		chunkData.WriteByte(8);
		// Color type (1 byte) - 6 = RGBA
		chunkData.WriteByte(6);
		// Compression method (1 byte) - 0 = deflate
		chunkData.WriteByte(0);
		// Filter method (1 byte) - 0 = adaptive
		chunkData.WriteByte(0);
		// Interlace method (1 byte) - 0 = no interlace
		chunkData.WriteByte(0);

		WritePngChunk(stream, "IHDR"u8, chunkData.ToArray());
	}

	private static void WriteIdatChunk(MemoryStream stream, byte[] pixelData, int width, int height)
	{
		// Convert BGRA to RGBA and add filter bytes
		var stride = width * 4;
		var rawData = new byte[(stride + 1) * height]; // +1 for filter byte per row

		for (var y = 0; y < height; y++)
		{
			// Filter byte (0 = None)
			rawData[y * (stride + 1)] = 0;

			for (var x = 0; x < width; x++)
			{
				var srcIndex = y * stride + x * 4;
				var dstIndex = y * (stride + 1) + 1 + x * 4;

				// Convert BGRA to RGBA
				rawData[dstIndex + 0] = pixelData[srcIndex + 2]; // R
				rawData[dstIndex + 1] = pixelData[srcIndex + 1]; // G
				rawData[dstIndex + 2] = pixelData[srcIndex + 0]; // B
				rawData[dstIndex + 3] = pixelData[srcIndex + 3]; // A
			}
		}

		// Compress with zlib (deflate with zlib header)
		using var compressedStream = new MemoryStream();
		using (var deflateStream = new System.IO.Compression.ZLibStream(compressedStream, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
		{
			deflateStream.Write(rawData, 0, rawData.Length);
		}

		WritePngChunk(stream, "IDAT"u8, compressedStream.ToArray());
	}

	private static void WriteIendChunk(MemoryStream stream)
	{
		WritePngChunk(stream, "IEND"u8, []);
	}

	private static void WritePngChunk(MemoryStream stream, ReadOnlySpan<byte> chunkType, byte[] data)
	{
		// Length (4 bytes, big-endian)
		stream.Write(ToBigEndian(data.Length), 0, 4);

		// Chunk type (4 bytes)
		stream.Write(chunkType);

		// Chunk data
		if (data.Length > 0)
		{
			stream.Write(data, 0, data.Length);
		}

		// CRC32 (4 bytes, big-endian) - calculated over chunk type + data
		var crcData = new byte[4 + data.Length];
		chunkType.CopyTo(crcData);
		Array.Copy(data, 0, crcData, 4, data.Length);
		var crc = CalculateCrc32(crcData);
		stream.Write(ToBigEndian((int)crc), 0, 4);
	}

	private static byte[] ToBigEndian(int value)
	{
		var bytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return bytes;
	}

	private static uint CalculateCrc32(byte[] data)
	{
		// PNG CRC32 polynomial
		const uint polynomial = 0xEDB88320;

		var crc = 0xFFFFFFFF;
		foreach (var b in data)
		{
			crc ^= b;
			for (var i = 0; i < 8; i++)
			{
				if ((crc & 1) != 0)
				{
					crc = (crc >> 1) ^ polynomial;
				}
				else
				{
					crc >>= 1;
				}
			}
		}
		return crc ^ 0xFFFFFFFF;
	}
}
