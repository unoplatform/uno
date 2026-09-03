#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageDecoder
{
	// WebP: decodes the lossless (VP8L) variant fully. Lossy (VP8 — a video-codec-scale intra decoder) and
	// animated WebP are routed back to the Skia codec.
	private static bool TryDecodeWebp(byte[] d, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;
		if (d.Length < 16 || d[8] != 'W' || d[9] != 'E' || d[10] != 'B' || d[11] != 'P')
		{
			return false;
		}

		var p = 12;
		while (p + 8 <= d.Length)
		{
			var id = (char)d[p] + "" + (char)d[p + 1] + (char)d[p + 2] + (char)d[p + 3];
			var size = ReadU32LE(d, p + 4);
			var chunk = p + 8;

			if (id == "VP8L")
			{
				return TryDecodeVp8l(d, chunk, out decoded);
			}

			if (id is "ANIM" or "ANMF" or "VP8 ")
			{
				return false; // animation / lossy -> Skia codec
			}

			// Unsigned size + bounds check keeps `p` moving forward; a crafted huge size can no longer wrap it.
			if (size > int.MaxValue || (long)chunk + size > d.Length)
			{
				return false;
			}

			p = chunk + (int)size + ((int)size & 1);
		}

		return false;
	}

	private static bool TryDecodeVp8l(byte[] d, int offset, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;
		var decoder = new Vp8lDecoder(d, offset);
		if (!decoder.Decode(out var argb, out var width, out var height))
		{
			return false;
		}

		var bgra = new byte[width * height * 4];
		for (var i = 0; i < argb.Length; i++)
		{
			var c = (uint)argb[i];
			SetPixelPremul(bgra, i * 4, (byte)(c >> 16), (byte)(c >> 8), (byte)c, (byte)(c >> 24));
		}

		decoded = new DecodedImage(width, height, new[] { bgra }, DecodedImage.SingleFrameDurations);
		return true;
	}

	private sealed class Vp8lDecoder
	{
		private readonly byte[] _d;
		private int _pos;
		private ulong _bitBuffer;
		private int _bitCount;
		private bool _failed;

		public Vp8lDecoder(byte[] d, int offset)
		{
			_d = d;
			_pos = offset;
		}

		private int ReadBits(int n)
		{
			while (_bitCount < n)
			{
				_bitBuffer |= (ulong)(_pos < _d.Length ? _d[_pos++] : 0) << _bitCount;
				_bitCount += 8;
			}

			var result = (int)(_bitBuffer & ((1UL << n) - 1));
			_bitBuffer >>= n;
			_bitCount -= n;
			return result;
		}

		public bool Decode(out int[] argb, out int width, out int height)
		{
			argb = Array.Empty<int>();
			width = height = 0;

			if (ReadBits(8) != 0x2F) // signature
			{
				return false;
			}

			width = ReadBits(14) + 1;
			height = ReadBits(14) + 1;
			ReadBits(1); // alpha_is_used
			if (ReadBits(3) != 0) // version
			{
				return false;
			}

			// Read transforms (applied in reverse after decoding).
			var transforms = new System.Collections.Generic.List<Transform>();
			var transformWidth = width;
			while (ReadBits(1) == 1)
			{
				var type = ReadBits(2);
				var t = new Transform { Type = type };
				switch (type)
				{
					case 0: // predictor
					case 1: // color
						t.Bits = ReadBits(3) + 2;
						var subW = SubSampleSize(transformWidth, t.Bits);
						var subH = SubSampleSize(height, t.Bits);
						t.Data = DecodeImageStream(subW, subH, isLevel0: false);
						t.SubWidth = subW;
						break;
					case 2: // subtract green
						break;
					case 3: // color indexing
						var colors = ReadBits(8) + 1;
						t.Data = DecodeImageStream(colors, 1, isLevel0: false);
						InverseCumulateColorTable(t.Data);
						t.Bits = colors <= 2 ? 3 : colors <= 4 ? 2 : colors <= 16 ? 1 : 0;
						t.ColorCount = colors;
						transformWidth = SubSampleSize(transformWidth, t.Bits);
						break;
				}

				transforms.Add(t);
			}

			var data = DecodeImageStream(transformWidth, height, isLevel0: true);
			if (data is null || _failed)
			{
				return false;
			}

			// Inverse transforms in reverse order of declaration.
			var curWidth = transformWidth;
			for (var i = transforms.Count - 1; i >= 0; i--)
			{
				if (transforms[i].Data is null && transforms[i].Type is 0 or 1 or 3)
				{
					return false; // a sub-image failed to decode
				}

				data = ApplyInverseTransform(transforms[i], data, ref curWidth, height, width);
			}

			argb = data;
			return !_failed && argb.Length == width * height;
		}

		private int[]? DecodeImageStream(int xsize, int ysize, bool isLevel0)
		{
			// Order matters: color cache is read before the meta-Huffman flag (matches libwebp).
			var colorCacheBits = 0;
			int[]? colorCache = null;
			if (ReadBits(1) == 1)
			{
				colorCacheBits = ReadBits(4);
				colorCache = new int[1 << colorCacheBits];
			}

			int[]? huffmanImage = null;
			var huffmanBits = 0;
			var huffmanXsize = 0;
			var numGroups = 1;

			if (isLevel0 && ReadBits(1) == 1)
			{
				huffmanBits = ReadBits(3) + 2;
				huffmanXsize = SubSampleSize(xsize, huffmanBits);
				var huffmanYsize = SubSampleSize(ysize, huffmanBits);
				huffmanImage = DecodeImageStream(huffmanXsize, huffmanYsize, isLevel0: false);
				if (huffmanImage is null)
				{
					return null;
				}

				var maxGroup = 0;
				for (var i = 0; i < huffmanImage.Length; i++)
				{
					var group = (huffmanImage[i] >> 8) & 0xFFFF;
					if (group > maxGroup)
					{
						maxGroup = group;
					}
				}

				numGroups = maxGroup + 1;
			}

			var alphabetSize0 = 256 + 24 + (colorCacheBits > 0 ? 1 << colorCacheBits : 0);
			var groups = new HuffmanGroup[numGroups];
			for (var g = 0; g < numGroups; g++)
			{
				groups[g] = new HuffmanGroup
				{
					Green = ReadHuffmanCode(alphabetSize0),
					Red = ReadHuffmanCode(256),
					Blue = ReadHuffmanCode(256),
					Alpha = ReadHuffmanCode(256),
					Distance = ReadHuffmanCode(40),
				};
			}

			var result = DecodeImageData(xsize, ysize, groups, huffmanImage, huffmanBits, huffmanXsize, colorCache, colorCacheBits);
			return _failed ? null : result;
		}

		private int[] DecodeImageData(int xsize, int ysize, HuffmanGroup[] groups, int[]? huffmanImage, int huffmanBits, int huffmanXsize, int[]? colorCache, int colorCacheBits)
		{
			var total = xsize * ysize;
			var data = new int[total];
			var pos = 0;
			var x = 0;
			var y = 0;

			void InsertCache(int argb)
			{
				if (colorCache is not null)
				{
					colorCache[(int)((0x1e35a7bdU * (uint)argb) >> (32 - colorCacheBits))] = argb;
				}
			}

			HuffmanGroup Group()
			{
				if (groups.Length == 1)
				{
					return groups[0];
				}

				var index = (huffmanImage![(y >> huffmanBits) * huffmanXsize + (x >> huffmanBits)] >> 8) & 0xFFFF;
				return groups[index];
			}

			while (pos < total && !_failed)
			{
				var group = Group();
				var code = group.Green.Read(this);
				if (code < 256)
				{
					var red = group.Red.Read(this);
					var blue = group.Blue.Read(this);
					var alpha = group.Alpha.Read(this);
					var argb = (alpha << 24) | (red << 16) | (code << 8) | blue;
					data[pos++] = argb;
					InsertCache(argb);
					if (++x == xsize) { x = 0; y++; }
				}
				else if (code < 256 + 24)
				{
					var length = GetCopyLength(code - 256);
					var distSymbol = group.Distance.Read(this);
					var distance = PlaneCodeToDistance(xsize, GetCopyDistance(distSymbol));
					if (distance < 1 || pos < distance)
					{
						_failed = true; // desync / unsupported stream -> caller falls back to the codec
						break;
					}

					for (var i = 0; i < length && pos < total; i++)
					{
						var argb = data[pos - distance];
						data[pos++] = argb;
						InsertCache(argb);
						if (++x == xsize) { x = 0; y++; }
					}
				}
				else
				{
					if (colorCache is null || code - 256 - 24 >= colorCache.Length)
					{
						_failed = true;
						break;
					}

					var argb = colorCache[code - 256 - 24];
					data[pos++] = argb;
					InsertCache(argb);
					if (++x == xsize) { x = 0; y++; }
				}
			}

			if (pos < total)
			{
				_failed = true;
			}

			return data;
		}

		private int GetCopyLength(int symbol) => GetPrefixValue(symbol);

		private int GetCopyDistance(int symbol) => GetPrefixValue(symbol);

		private int GetPrefixValue(int symbol)
		{
			if (symbol < 4)
			{
				return symbol + 1;
			}

			var extraBits = (symbol - 2) >> 1;
			var offset = (2 + (symbol & 1)) << extraBits;
			return offset + ReadBits(extraBits) + 1;
		}

		private static int PlaneCodeToDistance(int xsize, int planeCode)
		{
			if (planeCode > 120)
			{
				return planeCode - 120;
			}

			if (planeCode > _codeToPlane.Length)
			{
				return int.MaxValue; // near-distance code we can't map -> caller fails and falls back to the codec
			}

			var code = _codeToPlane[planeCode - 1];
			var yOffset = code >> 4;
			var xOffset = 8 - (code & 0xF);
			var dist = yOffset * xsize + xOffset;
			return dist >= 1 ? dist : 1;
		}

		private HuffmanTable ReadHuffmanCode(int alphabetSize)
		{
			var simple = ReadBits(1) == 1;
			var lengths = new int[alphabetSize];
			if (simple)
			{
				var numSymbols = ReadBits(1) + 1;
				var firstIs8Bit = ReadBits(1) == 1;
				var symbol0 = ReadBits(firstIs8Bit ? 8 : 1);
				if (numSymbols == 2)
				{
					var symbol1 = ReadBits(8);
					lengths[symbol0] = 1;
					lengths[symbol1] = 1;
				}
				else
				{
					return HuffmanTable.Single(symbol0);
				}
			}
			else
			{
				var codeLengthLengths = new int[19];
				var numCodeLengths = ReadBits(4) + 4;
				for (var i = 0; i < numCodeLengths; i++)
				{
					codeLengthLengths[_codeLengthOrder[i]] = ReadBits(3);
				}

				var codeLengthTable = HuffmanTable.FromLengths(codeLengthLengths);

				var maxSymbol = alphabetSize;
				if (ReadBits(1) == 1)
				{
					var lengthNBits = 2 + 2 * ReadBits(3);
					maxSymbol = 2 + ReadBits(lengthNBits);
				}

				var symbol = 0;
				var prevLength = 8;
				while (symbol < alphabetSize && maxSymbol-- > 0)
				{
					var codeLength = codeLengthTable.Read(this);
					if (codeLength < 16)
					{
						lengths[symbol++] = codeLength;
						if (codeLength != 0)
						{
							prevLength = codeLength;
						}
					}
					else
					{
						int repeat;
						var value = 0;
						if (codeLength == 16)
						{
							repeat = 3 + ReadBits(2);
							value = prevLength;
						}
						else if (codeLength == 17)
						{
							repeat = 3 + ReadBits(3);
						}
						else
						{
							repeat = 11 + ReadBits(7);
						}

						while (repeat-- > 0 && symbol < alphabetSize)
						{
							lengths[symbol++] = value;
						}
					}
				}
			}

			return HuffmanTable.FromLengths(lengths);
		}

		private int[] ApplyInverseTransform(Transform t, int[] data, ref int curWidth, int height, int fullWidth)
		{
			switch (t.Type)
			{
				case 0:
					InversePredictor(t, data, curWidth, height);
					return data;
				case 1:
					InverseColor(t, data, curWidth, height);
					return data;
				case 2:
					InverseSubtractGreen(data);
					return data;
				default: // color indexing
					var expanded = InverseColorIndexing(t, data, curWidth, height, fullWidth);
					curWidth = fullWidth;
					return expanded;
			}
		}

		private static void InverseSubtractGreen(int[] data)
		{
			for (var i = 0; i < data.Length; i++)
			{
				var argb = data[i];
				var green = (argb >> 8) & 0xFF;
				var red = (((argb >> 16) & 0xFF) + green) & 0xFF;
				var blue = ((argb & 0xFF) + green) & 0xFF;
				data[i] = (int)((uint)argb & 0xFF00FF00) | (red << 16) | blue;
			}
		}

		private void InversePredictor(Transform t, int[] data, int width, int height)
		{
			var bits = t.Bits;
			var tilesPerRow = t.SubWidth;
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var idx = y * width + x;
					int predicted;
					if (x == 0 && y == 0)
					{
						predicted = unchecked((int)0xFF000000);
					}
					else if (y == 0)
					{
						predicted = data[idx - 1];
					}
					else if (x == 0)
					{
						predicted = data[idx - width];
					}
					else
					{
						var mode = (t.Data![(y >> bits) * tilesPerRow + (x >> bits)] >> 8) & 0xFF;
						predicted = Predict(mode, data, idx, width);
					}

					data[idx] = AddPixels(data[idx], predicted);
				}
			}
		}

		private static int Predict(int mode, int[] data, int idx, int width)
		{
			var left = data[idx - 1];
			var top = data[idx - width];
			var topLeft = data[idx - width - 1];
			var topRight = data[idx - width + 1];
			return mode switch
			{
				0 => unchecked((int)0xFF000000),
				1 => left,
				2 => top,
				3 => topRight,
				4 => topLeft,
				5 => Average2(Average2(left, topRight), top),
				6 => Average2(left, topLeft),
				7 => Average2(left, top),
				8 => Average2(topLeft, top),
				9 => Average2(top, topRight),
				10 => Average2(Average2(left, topLeft), Average2(top, topRight)),
				11 => Select(top, left, topLeft),
				12 => ClampAddSubtractFull(left, top, topLeft),
				_ => ClampAddSubtractHalf(Average2(left, top), topLeft),
			};
		}

		private static int Average2(int a, int b)
		{
			var result = 0;
			for (var shift = 0; shift < 32; shift += 8)
			{
				var avg = (((a >> shift) & 0xFF) + ((b >> shift) & 0xFF)) / 2;
				result |= avg << shift;
			}

			return result;
		}

		private static int Select(int top, int left, int topLeft)
		{
			// Choose top or left by which is closer (per-channel L1) to the gradient point; matches libwebp.
			var sum = 0;
			for (var shift = 0; shift < 32; shift += 8)
			{
				var t = (top >> shift) & 0xFF;
				var l = (left >> shift) & 0xFF;
				var tl = (topLeft >> shift) & 0xFF;
				sum += Math.Abs(l - tl) - Math.Abs(t - tl);
			}

			return sum <= 0 ? top : left;
		}

		private static int ClampAddSubtractFull(int a, int b, int c)
		{
			var result = 0;
			for (var shift = 0; shift < 32; shift += 8)
			{
				var value = ((a >> shift) & 0xFF) + ((b >> shift) & 0xFF) - ((c >> shift) & 0xFF);
				result |= Math.Clamp(value, 0, 255) << shift;
			}

			return result;
		}

		private static int ClampAddSubtractHalf(int a, int b)
		{
			var result = 0;
			for (var shift = 0; shift < 32; shift += 8)
			{
				var av = (a >> shift) & 0xFF;
				var bv = (b >> shift) & 0xFF;
				var value = av + (av - bv) / 2;
				result |= Math.Clamp(value, 0, 255) << shift;
			}

			return result;
		}

		private static int AddPixels(int a, int b)
		{
			var result = 0;
			for (var shift = 0; shift < 32; shift += 8)
			{
				var sum = (((a >> shift) & 0xFF) + ((b >> shift) & 0xFF)) & 0xFF;
				result |= sum << shift;
			}

			return result;
		}

		private void InverseColor(Transform t, int[] data, int width, int height)
		{
			var bits = t.Bits;
			var tilesPerRow = t.SubWidth;
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var element = t.Data![(y >> bits) * tilesPerRow + (x >> bits)];
					var greenToRed = (sbyte)(element & 0xFF);
					var greenToBlue = (sbyte)((element >> 8) & 0xFF);
					var redToBlue = (sbyte)((element >> 16) & 0xFF);

					var idx = y * width + x;
					var argb = data[idx];
					var green = (argb >> 8) & 0xFF;
					var red = (argb >> 16) & 0xFF;
					var blue = argb & 0xFF;

					red = (red + ColorTransformDelta(greenToRed, (sbyte)green)) & 0xFF;
					blue = (blue + ColorTransformDelta(greenToBlue, (sbyte)green)) & 0xFF;
					blue = (blue + ColorTransformDelta(redToBlue, (sbyte)red)) & 0xFF;

					data[idx] = (int)((uint)argb & 0xFF00FF00) | (red << 16) | blue;
				}
			}
		}

		private static int ColorTransformDelta(sbyte t, sbyte c) => (t * c) >> 5;

		private static void InverseCumulateColorTable(int[]? table)
		{
			if (table is null)
			{
				return;
			}

			// The palette is stored delta-coded (subtract-green style across entries).
			for (var i = 1; i < table.Length; i++)
			{
				table[i] = AddPixels(table[i], table[i - 1]);
			}
		}

		private int[] InverseColorIndexing(Transform t, int[] data, int width, int height, int fullWidth)
		{
			var table = t.Data!;
			var colorCount = t.ColorCount;
			var output = new int[fullWidth * height];
			var bits = t.Bits;

			if (bits == 0)
			{
				for (var i = 0; i < data.Length && i < output.Length; i++)
				{
					var index = (data[i] >> 8) & 0xFF;
					output[i] = index < colorCount ? table[index] : 0;
				}

				return output;
			}

			var pixelsPerByte = 1 << bits;
			var mask = pixelsPerByte - 1;
			var bitWidth = 8 >> bits;
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < fullWidth; x++)
				{
					var packed = (data[y * width + (x >> bits)] >> 8) & 0xFF;
					var index = (packed >> (bitWidth * (x & mask))) & ((1 << bitWidth) - 1);
					output[y * fullWidth + x] = index < colorCount ? table[index] : 0;
				}
			}

			return output;
		}

		private static int SubSampleSize(int size, int samplingBits) => (size + (1 << samplingBits) - 1) >> samplingBits;

		private sealed class Transform
		{
			public int Type;
			public int Bits;
			public int SubWidth;
			public int ColorCount;
			public int[]? Data;
		}

		private sealed class HuffmanGroup
		{
			public HuffmanTable Green = null!;
			public HuffmanTable Red = null!;
			public HuffmanTable Blue = null!;
			public HuffmanTable Alpha = null!;
			public HuffmanTable Distance = null!;
		}

		private sealed class HuffmanTable
		{
			// WebP lossless packs canonical prefix codes bit-reversed (LSB-first). We key each symbol by
			// (length, reversed-code) and decode by accumulating bits LSB-first until a length matches.
			private readonly System.Collections.Generic.Dictionary<int, int> _lookup = new();
			private readonly int _maxLength;
			private readonly int _single = -1;

			private HuffmanTable(int single) => _single = single;

			private HuffmanTable(System.Collections.Generic.Dictionary<int, int> lookup, int maxLength)
			{
				_lookup = lookup;
				_maxLength = maxLength;
			}

			public static HuffmanTable Single(int symbol) => new(symbol);

			public static HuffmanTable FromLengths(int[] lengths)
			{
				var counts = new int[16];
				var used = 0;
				var lastSymbol = 0;
				var maxLength = 0;
				for (var s = 0; s < lengths.Length; s++)
				{
					var l = lengths[s];
					if (l != 0)
					{
						counts[l]++;
						used++;
						lastSymbol = s;
						if (l > maxLength)
						{
							maxLength = l;
						}
					}
				}

				if (used <= 1)
				{
					return new HuffmanTable(lastSymbol);
				}

				// Canonical code per length (MSB-first), then reverse to LSB-first for the decoder.
				var nextCode = new int[16];
				var code = 0;
				for (var l = 1; l < 16; l++)
				{
					code = (code + counts[l - 1]) << 1;
					nextCode[l] = code;
				}

				var lookup = new System.Collections.Generic.Dictionary<int, int>(used);
				for (var s = 0; s < lengths.Length; s++)
				{
					var l = lengths[s];
					if (l != 0)
					{
						var canonical = nextCode[l]++;
						var reversed = ReverseBits(canonical, l);
						lookup[(l << 16) | reversed] = s;
					}
				}

				return new HuffmanTable(lookup, maxLength);
			}

			public int Read(Vp8lDecoder decoder)
			{
				if (_single >= 0)
				{
					return _single;
				}

				var acc = 0;
				for (var l = 1; l <= _maxLength; l++)
				{
					acc |= decoder.ReadBits(1) << (l - 1);
					if (_lookup.TryGetValue((l << 16) | acc, out var symbol))
					{
						return symbol;
					}
				}

				return 0;
			}

			private static int ReverseBits(int value, int count)
			{
				var result = 0;
				for (var i = 0; i < count; i++)
				{
					result |= ((value >> i) & 1) << (count - 1 - i);
				}

				return result;
			}
		}

		private static readonly int[] _codeLengthOrder =
		{
			17, 18, 0, 1, 2, 3, 4, 5, 16, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
		};

		private static readonly byte[] _codeToPlane =
		{
			0x18, 0x07, 0x17, 0x19, 0x28, 0x06, 0x27, 0x29, 0x16, 0x1a, 0x26, 0x2a,
			0x38, 0x05, 0x37, 0x39, 0x15, 0x1b, 0x36, 0x3a, 0x25, 0x2b, 0x48, 0x04,
			0x47, 0x49, 0x14, 0x1c, 0x35, 0x3b, 0x46, 0x4a, 0x24, 0x2c, 0x58, 0x45,
			0x4b, 0x34, 0x3c, 0x03, 0x57, 0x59, 0x13, 0x1d, 0x56, 0x5a, 0x23, 0x2d,
			0x44, 0x4c, 0x55, 0x5b, 0x33, 0x3d, 0x68, 0x02, 0x67, 0x69, 0x12, 0x1e,
			0x66, 0x6a, 0x22, 0x2e, 0x54, 0x5c, 0x43, 0x4d, 0x65, 0x6b, 0x32, 0x3e,
			0x78, 0x01, 0x77, 0x79, 0x53, 0x5d, 0x11, 0x1f, 0x64, 0x6c, 0x42, 0x4e,
			0x76, 0x7a, 0x21, 0x2f, 0x75, 0x7b, 0x31, 0x3f, 0x63, 0x6d, 0x52, 0x5e,
			0x00, 0x74, 0x7c, 0x41, 0x4f, 0x10, 0x20, 0x62, 0x6e, 0x30, 0x73, 0x7d,
			0x51, 0x5f, 0x40, 0x72, 0x7e, 0x61, 0x6f, 0x50, 0x71, 0x7f, 0x60, 0x70,
		};
	}
}
