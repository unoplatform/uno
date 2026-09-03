#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageDecoder
{
	private static bool TryDecodeGif(byte[] d, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;

		var p = 6; // skip "GIF87a"/"GIF89a"
		var screenWidth = ReadU16LE(d, p);
		var screenHeight = ReadU16LE(d, p + 2);
		var packed = d[p + 4];
		p += 7; // logical screen descriptor

		byte[]? globalTable = null;
		if ((packed & 0x80) != 0)
		{
			var size = 2 << (packed & 0x07);
			globalTable = new byte[size * 3];
			Array.Copy(d, p, globalTable, 0, globalTable.Length);
			p += globalTable.Length;
		}

		if (screenWidth <= 0 || screenHeight <= 0)
		{
			return false;
		}

		var frames = new List<byte[]>();
		var durations = new List<int>();

		// Persistent canvas that each frame composites onto (per GIF disposal semantics).
		var canvas = new byte[screenWidth * screenHeight * 4];
		byte[]? previousCanvas = null;

		var transparentIndex = -1;
		var delayMs = 100;
		var disposalMethod = 0;

		while (p < d.Length)
		{
			var block = d[p++];
			if (block == 0x3B) // trailer
			{
				break;
			}

			if (block == 0x21) // extension
			{
				var label = d[p++];
				if (label == 0xF9) // graphic control extension
				{
					var blockSize = d[p++];
					var flags = d[p];
					disposalMethod = (flags >> 2) & 0x07;
					transparentIndex = (flags & 0x01) != 0 ? d[p + 3] : -1;
					var delayCentis = ReadU16LE(d, p + 1);
					delayMs = delayCentis > 0 ? delayCentis * 10 : 100;
					p += blockSize;
				}

				p = SkipToNextBlock(d, p); // skip remaining sub-blocks + terminator
				continue;
			}

			if (block != 0x2C) // not an image descriptor — stop
			{
				break;
			}

			var left = ReadU16LE(d, p);
			var top = ReadU16LE(d, p + 2);
			var frameWidth = ReadU16LE(d, p + 4);
			var frameHeight = ReadU16LE(d, p + 6);
			var imgPacked = d[p + 8];
			p += 9;

			var localTable = globalTable;
			if ((imgPacked & 0x80) != 0)
			{
				var size = 2 << (imgPacked & 0x07);
				localTable = new byte[size * 3];
				Array.Copy(d, p, localTable, 0, localTable.Length);
				p += localTable.Length;
			}

			var interlaced = (imgPacked & 0x40) != 0;
			var indices = DecodeLzw(d, ref p, frameWidth * frameHeight);
			if (localTable is null)
			{
				return false;
			}

			// Snapshot for "restore to previous" disposal.
			if (disposalMethod == 3)
			{
				previousCanvas = (byte[])canvas.Clone();
			}

			CompositeGifFrame(canvas, screenWidth, screenHeight, indices, interlaced, left, top, frameWidth, frameHeight, localTable, transparentIndex);

			frames.Add((byte[])canvas.Clone());
			durations.Add(delayMs);

			ApplyDisposal(canvas, screenWidth, disposalMethod, left, top, frameWidth, frameHeight, previousCanvas);

			// Reset per-frame graphic control state.
			transparentIndex = -1;
			disposalMethod = 0;
		}

		if (frames.Count == 0)
		{
			return false;
		}

		decoded = new DecodedImage(screenWidth, screenHeight, frames.ToArray(), durations.ToArray());
		return true;
	}

	private static void CompositeGifFrame(byte[] canvas, int canvasWidth, int canvasHeight, byte[] indices, bool interlaced, int left, int top, int frameWidth, int frameHeight, byte[] table, int transparentIndex)
	{
		var colors = table.Length / 3;
		for (var y = 0; y < frameHeight; y++)
		{
			var srcRow = interlaced ? DeinterlacedRow(y, frameHeight) : y;
			var canvasY = top + srcRow;
			if (canvasY < 0 || canvasY >= canvasHeight)
			{
				continue;
			}

			for (var x = 0; x < frameWidth; x++)
			{
				var index = indices[y * frameWidth + x];
				if (index == transparentIndex)
				{
					continue; // transparent — keep whatever's underneath
				}

				var canvasX = left + x;
				if (canvasX < 0 || canvasX >= canvasWidth || index >= colors)
				{
					continue;
				}

				var c = index * 3;
				SetPixelPremul(canvas, (canvasY * canvasWidth + canvasX) * 4, table[c], table[c + 1], table[c + 2], 255);
			}
		}
	}

	private static void ApplyDisposal(byte[] canvas, int canvasWidth, int disposalMethod, int left, int top, int frameWidth, int frameHeight, byte[]? previousCanvas)
	{
		switch (disposalMethod)
		{
			case 2: // restore to background (transparent)
				for (var y = top; y < top + frameHeight; y++)
				{
					for (var x = left; x < left + frameWidth; x++)
					{
						var o = (y * canvasWidth + x) * 4;
						if (o >= 0 && o + 3 < canvas.Length)
						{
							canvas[o] = canvas[o + 1] = canvas[o + 2] = canvas[o + 3] = 0;
						}
					}
				}
				break;
			case 3: // restore to previous
				if (previousCanvas is not null)
				{
					Array.Copy(previousCanvas, canvas, canvas.Length);
				}
				break;
		}
	}

	private static int DeinterlacedRow(int y, int height)
	{
		// GIF interlace passes: rows 0,8,16..; 4,12..; 2,6..; 1,3..
		var pass0 = (height + 7) / 8;
		var pass1 = (height + 3) / 8;
		var pass2 = (height + 1) / 4;
		if (y < pass0) return y * 8;
		y -= pass0;
		if (y < pass1) return y * 8 + 4;
		y -= pass1;
		if (y < pass2) return y * 4 + 2;
		y -= pass2;
		return y * 2 + 1;
	}

	private static byte[] DecodeLzw(byte[] d, ref int p, int pixelCount)
	{
		var minCodeSize = d[p++];

		// Gather the image's LZW sub-blocks into one contiguous buffer and advance p past the terminator,
		// so bit-reading below is decoupled from the sub-block framing.
		var dataLength = 0;
		var scan = p;
		while (scan < d.Length)
		{
			var len = d[scan++];
			if (len == 0)
			{
				break;
			}

			dataLength += len;
			scan += len;
		}

		var buffer = new byte[dataLength];
		var bufPos = 0;
		while (p < d.Length)
		{
			var len = d[p++];
			if (len == 0)
			{
				break;
			}

			Array.Copy(d, p, buffer, bufPos, len);
			bufPos += len;
			p += len;
		}

		var clearCode = 1 << minCodeSize;
		var endCode = clearCode + 1;

		var output = new byte[pixelCount];
		var outPos = 0;

		var prefix = new int[4096];
		var suffix = new byte[4096];
		var firstByte = new byte[4096];
		var stack = new byte[4096];
		for (var i = 0; i < clearCode; i++)
		{
			prefix[i] = -1;
			suffix[i] = (byte)i;
			firstByte[i] = (byte)i;
		}

		var codeSize = minCodeSize + 1;
		var next = endCode + 1;
		var previousCode = -1;

		var bitBuffer = 0;
		var bitCount = 0;
		var pos = 0;

		while (true)
		{
			while (bitCount < codeSize)
			{
				if (pos >= buffer.Length)
				{
					return output; // truncated data — return what we have
				}

				bitBuffer |= buffer[pos++] << bitCount;
				bitCount += 8;
			}

			var code = bitBuffer & ((1 << codeSize) - 1);
			bitBuffer >>= codeSize;
			bitCount -= codeSize;

			if (code == clearCode)
			{
				codeSize = minCodeSize + 1;
				next = endCode + 1;
				previousCode = -1;
				continue;
			}

			if (code == endCode)
			{
				break;
			}

			var sp = 0;
			var currentCode = code;
			if (code >= next)
			{
				stack[sp++] = firstByte[previousCode]; // KwKwK
				currentCode = previousCode;
			}

			while (currentCode >= clearCode)
			{
				stack[sp++] = suffix[currentCode];
				currentCode = prefix[currentCode];
			}

			var first = (byte)currentCode;
			stack[sp++] = first;

			while (sp > 0 && outPos < output.Length)
			{
				output[outPos++] = stack[--sp];
			}

			if (previousCode != -1 && next < 4096)
			{
				prefix[next] = previousCode;
				suffix[next] = first;
				firstByte[next] = firstByte[previousCode];
				next++;
				if (next == (1 << codeSize) && codeSize < 12)
				{
					codeSize++;
				}
			}

			previousCode = code;
		}

		return output;
	}

	private static int SkipToNextBlock(byte[] d, int p)
	{
		while (p < d.Length && d[p] != 0)
		{
			p += d[p] + 1;
		}

		return p < d.Length ? p + 1 : p; // skip terminator
	}
}
