#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageEncoder
{
	private const int GifAlphaThreshold = 128;

	// GIF89a, single frame: quantize to ≤256 colours (median-cut), reserve a transparent palette index when any pixel
	// is (nearly) transparent, then LZW-compress the index stream. Compatible with what ManagedImageDecoder.Gif reads.
	private static byte[] EncodeGif(byte[] rgba, int width, int height)
	{
		var count = width * height;

		var opaqueColors = new Dictionary<int, int>();
		var hasTransparent = false;
		for (var i = 0; i < count; i++)
		{
			if (rgba[i * 4 + 3] < GifAlphaThreshold)
			{
				hasTransparent = true;
				continue;
			}

			var key = (rgba[i * 4] << 16) | (rgba[i * 4 + 1] << 8) | rgba[i * 4 + 2];
			opaqueColors[key] = opaqueColors.TryGetValue(key, out var c) ? c + 1 : 1;
		}

		var maxColors = hasTransparent ? 255 : 256;
		var palette = BuildPalette(opaqueColors, maxColors, out var colorToIndex);

		var transparentIndex = -1;
		if (hasTransparent)
		{
			transparentIndex = palette.Count;
			palette.Add((0, 0, 0));
		}

		var indices = new byte[count];
		for (var i = 0; i < count; i++)
		{
			if (rgba[i * 4 + 3] < GifAlphaThreshold)
			{
				indices[i] = (byte)transparentIndex;
				continue;
			}

			var key = (rgba[i * 4] << 16) | (rgba[i * 4 + 1] << 8) | rgba[i * 4 + 2];
			indices[i] = (byte)colorToIndex[key];
		}

		var gctBits = Math.Max(1, BitsFor(palette.Count));
		var gctEntries = 1 << gctBits;
		var minCodeSize = Math.Max(2, gctBits);

		using var ms = new MemoryStream();
		ms.Write(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' }, 0, 6);

		// Logical Screen Descriptor.
		WriteU16Le(ms, width);
		WriteU16Le(ms, height);
		ms.WriteByte((byte)(0x80 | ((gctBits - 1) << 4) | (gctBits - 1))); // GCT present, colour resolution, GCT size
		ms.WriteByte(0); // background colour index
		ms.WriteByte(0); // pixel aspect ratio

		// Global Colour Table, padded to a power of two.
		for (var i = 0; i < gctEntries; i++)
		{
			if (i < palette.Count)
			{
				ms.WriteByte(palette[i].r);
				ms.WriteByte(palette[i].g);
				ms.WriteByte(palette[i].b);
			}
			else
			{
				ms.WriteByte(0);
				ms.WriteByte(0);
				ms.WriteByte(0);
			}
		}

		if (hasTransparent)
		{
			// Graphic Control Extension.
			ms.Write(new byte[] { 0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, (byte)transparentIndex, 0x00 }, 0, 8);
		}

		// Image Descriptor.
		ms.WriteByte(0x2C);
		WriteU16Le(ms, 0); // left
		WriteU16Le(ms, 0); // top
		WriteU16Le(ms, width);
		WriteU16Le(ms, height);
		ms.WriteByte(0); // no local colour table, not interlaced

		ms.WriteByte((byte)minCodeSize);
		WriteSubBlocks(ms, LzwCompress(indices, minCodeSize));
		ms.WriteByte(0x00); // block terminator

		ms.WriteByte(0x3B); // trailer
		return ms.ToArray();
	}

	private static List<(byte r, byte g, byte b)> BuildPalette(Dictionary<int, int> colors, int maxColors, out Dictionary<int, int> colorToIndex)
	{
		colorToIndex = new Dictionary<int, int>();
		var palette = new List<(byte r, byte g, byte b)>();

		if (colors.Count <= maxColors)
		{
			foreach (var key in colors.Keys)
			{
				colorToIndex[key] = palette.Count;
				palette.Add(((byte)(key >> 16), (byte)(key >> 8), (byte)key));
			}

			if (palette.Count == 0)
			{
				palette.Add((0, 0, 0)); // fully-transparent image still needs one GCT entry
			}

			return palette;
		}

		var boxes = new List<List<int>> { new(colors.Keys) };
		while (boxes.Count < maxColors)
		{
			var bestBox = -1;
			var bestRange = 0;
			var bestChannel = 0;
			for (var i = 0; i < boxes.Count; i++)
			{
				if (boxes[i].Count < 2)
				{
					continue;
				}

				var (channel, range) = WidestChannel(boxes[i]);
				if (range > bestRange)
				{
					bestRange = range;
					bestBox = i;
					bestChannel = channel;
				}
			}

			if (bestBox < 0)
			{
				break; // every box is a single colour
			}

			var box = boxes[bestBox];
			box.Sort((a, b) => ChannelValue(a, bestChannel).CompareTo(ChannelValue(b, bestChannel)));
			var mid = box.Count / 2;
			boxes[bestBox] = box.GetRange(0, mid);
			boxes.Add(box.GetRange(mid, box.Count - mid));
		}

		foreach (var box in boxes)
		{
			long sr = 0, sg = 0, sb = 0, sw = 0;
			foreach (var key in box)
			{
				var w = colors[key];
				sr += (long)((key >> 16) & 0xFF) * w;
				sg += (long)((key >> 8) & 0xFF) * w;
				sb += (long)(key & 0xFF) * w;
				sw += w;
			}

			var index = palette.Count;
			palette.Add(((byte)(sr / sw), (byte)(sg / sw), (byte)(sb / sw)));
			foreach (var key in box)
			{
				colorToIndex[key] = index;
			}
		}

		return palette;
	}

	private static (int channel, int range) WidestChannel(List<int> box)
	{
		int rMin = 255, rMax = 0, gMin = 255, gMax = 0, bMin = 255, bMax = 0;
		foreach (var key in box)
		{
			var r = (key >> 16) & 0xFF;
			var g = (key >> 8) & 0xFF;
			var b = key & 0xFF;
			rMin = Math.Min(rMin, r); rMax = Math.Max(rMax, r);
			gMin = Math.Min(gMin, g); gMax = Math.Max(gMax, g);
			bMin = Math.Min(bMin, b); bMax = Math.Max(bMax, b);
		}

		var rr = rMax - rMin;
		var gr = gMax - gMin;
		var br = bMax - bMin;
		if (rr >= gr && rr >= br)
		{
			return (0, rr);
		}

		return gr >= br ? (1, gr) : (2, br);
	}

	private static int ChannelValue(int key, int channel) => channel switch
	{
		0 => (key >> 16) & 0xFF,
		1 => (key >> 8) & 0xFF,
		_ => key & 0xFF,
	};

	private static int BitsFor(int colorCount)
	{
		var bits = 1;
		while ((1 << bits) < colorCount)
		{
			bits++;
		}

		return bits;
	}

	// Variable-width LZW (GIF flavour: LSB-first packing, giblib code-size growth) matching ManagedImageDecoder's reader.
	private static byte[] LzwCompress(byte[] indices, int minCodeSize)
	{
		var clearCode = 1 << minCodeSize;
		var eofCode = clearCode + 1;
		var runningBits = minCodeSize + 1;
		var maxCode = 1 << runningBits;
		var runningCode = eofCode + 1;
		var dict = new Dictionary<int, int>();

		var bytes = new List<byte>();
		var bitBuffer = 0;
		var bitCount = 0;

		void Output(int code)
		{
			bitBuffer |= code << bitCount;
			bitCount += runningBits;
			while (bitCount >= 8)
			{
				bytes.Add((byte)(bitBuffer & 0xFF));
				bitBuffer >>= 8;
				bitCount -= 8;
			}

			if (runningCode >= maxCode && runningBits < 12)
			{
				maxCode = 1 << ++runningBits;
			}
		}

		Output(clearCode);
		var current = (int)indices[0];
		for (var i = 1; i < indices.Length; i++)
		{
			int pixel = indices[i];
			var key = (current << 8) | pixel;
			if (dict.TryGetValue(key, out var next))
			{
				current = next;
				continue;
			}

			Output(current);
			if (runningCode >= 4095)
			{
				Output(clearCode);
				runningCode = eofCode + 1;
				runningBits = minCodeSize + 1;
				maxCode = 1 << runningBits;
				dict.Clear();
			}
			else
			{
				dict[key] = runningCode++;
			}

			current = pixel;
		}

		Output(current);
		Output(eofCode);

		if (bitCount > 0)
		{
			bytes.Add((byte)(bitBuffer & 0xFF));
		}

		return bytes.ToArray();
	}

	private static void WriteSubBlocks(Stream s, byte[] data)
	{
		var offset = 0;
		while (offset < data.Length)
		{
			var len = Math.Min(255, data.Length - offset);
			s.WriteByte((byte)len);
			s.Write(data, offset, len);
			offset += len;
		}
	}

	private static void WriteU16Le(Stream s, int value)
	{
		s.WriteByte((byte)value);
		s.WriteByte((byte)(value >> 8));
	}
}
