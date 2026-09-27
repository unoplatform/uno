#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageEncoder
{
	// Uncompressed 32bpp BMP (BGRA, bottom-up). 32bpp rows are inherently 4-byte aligned, so no padding. The header
	// is a BITMAPV4HEADER with BI_BITFIELDS masks because a BI_RGB one cannot state that the fourth byte is alpha,
	// leaving a reader to guess (and guess wrong for a fully transparent image).
	private static byte[] EncodeBmp(byte[] rgba, int width, int height)
	{
		const int fileHeader = 14;
		const int infoHeader = 108; // BITMAPV4HEADER
		var pixelBytes = checked(width * height * 4);
		var size = fileHeader + infoHeader + pixelBytes;
		var buf = new byte[size];

		// BITMAPFILEHEADER
		buf[0] = (byte)'B';
		buf[1] = (byte)'M';
		WriteU32(buf, 2, (uint)size);
		WriteU32(buf, 10, fileHeader + infoHeader);   // pixel data offset

		// BITMAPV4HEADER
		WriteU32(buf, 14, infoHeader);
		WriteU32(buf, 18, (uint)width);
		WriteU32(buf, 22, (uint)height);              // positive → bottom-up
		WriteU16(buf, 26, 1);                         // planes
		WriteU16(buf, 28, 32);                        // bpp
		WriteU32(buf, 30, 3);                         // BI_BITFIELDS
		WriteU32(buf, 34, (uint)pixelBytes);
		WriteU32(buf, 54, 0x00FF0000);                // red mask
		WriteU32(buf, 58, 0x0000FF00);                // green mask
		WriteU32(buf, 62, 0x000000FF);                // blue mask
		WriteU32(buf, 66, 0xFF000000);                // alpha mask
		WriteU32(buf, 70, 0x73524742);                // LCS_sRGB

		var o = fileHeader + infoHeader;
		for (int y = height - 1; y >= 0; y--)         // bottom-up
		{
			var row = y * width * 4;
			for (int x = 0; x < width; x++)
			{
				var s = row + x * 4;
				buf[o++] = rgba[s + 2];   // B
				buf[o++] = rgba[s + 1];   // G
				buf[o++] = rgba[s + 0];   // R
				buf[o++] = rgba[s + 3];   // A
			}
		}

		return buf;
	}

	private static void WriteU16(byte[] b, int o, ushort v)
	{
		b[o] = (byte)v;
		b[o + 1] = (byte)(v >> 8);
	}

	private static void WriteU32(byte[] b, int o, uint v)
	{
		b[o] = (byte)v;
		b[o + 1] = (byte)(v >> 8);
		b[o + 2] = (byte)(v >> 16);
		b[o + 3] = (byte)(v >> 24);
	}
}
