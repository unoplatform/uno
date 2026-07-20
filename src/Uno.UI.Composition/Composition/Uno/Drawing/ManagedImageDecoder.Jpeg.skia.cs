#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageDecoder
{
	// Baseline (sequential DCT, Huffman) JPEG decode. Progressive JPEGs (SOF2) and arithmetic coding fall back to Skia.
	private static bool TryDecodeJpeg(byte[] d, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;

		var quant = new int[4][];
		var huffDc = new JpegHuffman?[4];
		var huffAc = new JpegHuffman?[4];
		var restartInterval = 0;

		int width = 0, height = 0;
		var orientation = 1;
		JpegComponent[]? components = null;

		var p = 2; // past SOI
		while (p + 1 < d.Length)
		{
			if (d[p] != 0xFF)
			{
				p++;
				continue;
			}

			var marker = d[p + 1];
			p += 2;

			if (marker is 0xD9 or 0x01 || (marker >= 0xD0 && marker <= 0xD7))
			{
				continue; // EOI / standalone
			}

			var segLen = (d[p] << 8) | d[p + 1];
			var segStart = p + 2;
			var segEnd = p + segLen;

			switch (marker)
			{
				case 0xDB: // DQT
				{
					var q = segStart;
					while (q < segEnd)
					{
						var pqTq = d[q++];
						var precision16 = (pqTq >> 4) != 0;
						var table = new int[64];
						for (var i = 0; i < 64; i++)
						{
							table[i] = precision16 ? (d[q] << 8) | d[q + 1] : d[q];
							q += precision16 ? 2 : 1;
						}

						quant[pqTq & 0x0F] = table;
					}
					break;
				}
				case 0xC2: // progressive
				case 0xC3: // lossless
				case 0xC9:
				case 0xCA:
				case 0xCB:
					return false;
				case 0xC0: // baseline
				case 0xC1: // extended sequential
				{
					var q = segStart;
					q++; // sample precision (assume 8)
					height = (d[q] << 8) | d[q + 1];
					width = (d[q + 2] << 8) | d[q + 3];
					var count = d[q + 4];
					q += 5;
					components = new JpegComponent[count];
					for (var i = 0; i < count; i++)
					{
						var id = d[q];
						var sampling = d[q + 1];
						components[i] = new JpegComponent
						{
							Id = id,
							H = sampling >> 4,
							V = sampling & 0x0F,
							QuantId = d[q + 2],
						};
						q += 3;
					}
					break;
				}
				case 0xC4: // DHT
				{
					var q = segStart;
					while (q < segEnd)
					{
						var tcTh = d[q++];
						var counts = new byte[16];
						var total = 0;
						for (var i = 0; i < 16; i++)
						{
							counts[i] = d[q++];
							total += counts[i];
						}

						var symbols = new byte[total];
						Array.Copy(d, q, symbols, 0, total);
						q += total;

						var table = new JpegHuffman(counts, symbols);
						if ((tcTh >> 4) == 0)
						{
							huffDc[tcTh & 0x0F] = table;
						}
						else
						{
							huffAc[tcTh & 0x0F] = table;
						}
					}
					break;
				}
				case 0xE1: // APP1 (Exif)
					orientation = TryReadExifOrientation(d, segStart, segEnd) ?? orientation;
					break;
				case 0xDD: // DRI
					restartInterval = (d[segStart] << 8) | d[segStart + 1];
					break;
				case 0xDA: // SOS
				{
					var q = segStart;
					var scanCount = d[q++];
					for (var i = 0; i < scanCount; i++)
					{
						var selector = d[q];
						var tables = d[q + 1];
						q += 2;
						foreach (var c in components!)
						{
							if (c.Id == selector)
							{
								c.DcTable = tables >> 4;
								c.AcTable = tables & 0x0F;
							}
						}
					}

					var entropyStart = segEnd; // Ss/Se/Ah-Al already inside segLen
					decoded = DecodeScan(d, entropyStart, width, height, components!, quant, huffDc, huffAc, restartInterval);
					if (decoded is not null && orientation > 1)
					{
						decoded = ApplyExifOrientation(decoded, orientation);
					}

					return decoded is not null;
				}
			}

			p = segEnd;
		}

		return false;
	}

	private static DecodedImage? DecodeScan(byte[] d, int start, int width, int height, JpegComponent[] components, int[][] quant, JpegHuffman?[] huffDc, JpegHuffman?[] huffAc, int restartInterval)
	{
		if (width <= 0 || height <= 0 || components.Length is not (1 or 3))
		{
			return null;
		}

		var hMax = 1;
		var vMax = 1;
		foreach (var c in components)
		{
			hMax = Math.Max(hMax, c.H);
			vMax = Math.Max(vMax, c.V);
		}

		var mcusPerRow = (width + 8 * hMax - 1) / (8 * hMax);
		var mcusPerCol = (height + 8 * vMax - 1) / (8 * vMax);

		foreach (var c in components)
		{
			c.PlaneWidth = mcusPerRow * c.H * 8;
			c.PlaneHeight = mcusPerCol * c.V * 8;
			c.Plane = new byte[c.PlaneWidth * c.PlaneHeight];
		}

		var reader = new JpegBitReader(d, start);
		var block = new int[64];
		var spatial = new byte[64];
		var mcuCount = 0;

		for (var my = 0; my < mcusPerCol; my++)
		{
			for (var mx = 0; mx < mcusPerRow; mx++)
			{
				if (restartInterval > 0 && mcuCount > 0 && mcuCount % restartInterval == 0)
				{
					reader.Restart();
					foreach (var c in components)
					{
						c.DcPredictor = 0;
					}
				}

				foreach (var c in components)
				{
					var dc = huffDc[c.DcTable]!;
					var ac = huffAc[c.AcTable]!;
					var qt = quant[c.QuantId];
					for (var by = 0; by < c.V; by++)
					{
						for (var bx = 0; bx < c.H; bx++)
						{
							DecodeBlock(reader, dc, ac, qt, block, ref c.DcPredictor);
							Idct(block, spatial);

							var px = (mx * c.H + bx) * 8;
							var py = (my * c.V + by) * 8;
							for (var yy = 0; yy < 8; yy++)
							{
								var row = (py + yy) * c.PlaneWidth + px;
								for (var xx = 0; xx < 8; xx++)
								{
									c.Plane![row + xx] = spatial[yy * 8 + xx];
								}
							}
						}
					}
				}

				mcuCount++;
			}
		}

		return ToRgb(width, height, components, hMax, vMax);
	}

	private static void DecodeBlock(JpegBitReader reader, JpegHuffman dc, JpegHuffman ac, int[] qt, int[] block, ref int dcPredictor)
	{
		Array.Clear(block, 0, 64);

		var t = dc.Decode(reader);
		var diff = t == 0 ? 0 : reader.ReceiveExtend(t);
		dcPredictor += diff;
		block[0] = dcPredictor * qt[0];

		var k = 1;
		while (k < 64)
		{
			var rs = ac.Decode(reader);
			var r = rs >> 4;
			var s = rs & 0x0F;
			if (s == 0)
			{
				if (r != 15)
				{
					break; // EOB
				}

				k += 16; // ZRL
				continue;
			}

			k += r;
			if (k >= 64)
			{
				break;
			}

			var value = reader.ReceiveExtend(s);
			block[ZigZag[k]] = value * qt[k];
			k++;
		}
	}

	private static DecodedImage ToRgb(int width, int height, JpegComponent[] components, int hMax, int vMax)
	{
		var bgra = new byte[width * height * 4];
		var grayscale = components.Length == 1;
		var y = components[0];
		var cb = grayscale ? null : components[1];
		var cr = grayscale ? null : components[2];

		for (var py = 0; py < height; py++)
		{
			for (var px = 0; px < width; px++)
			{
				var yVal = Sample(y, px, py, hMax, vMax);
				byte r, g, b;
				if (grayscale)
				{
					r = g = b = (byte)yVal;
				}
				else
				{
					var cbVal = Sample(cb!, px, py, hMax, vMax) - 128;
					var crVal = Sample(cr!, px, py, hMax, vMax) - 128;
					r = Clamp(yVal + 1.402 * crVal);
					g = Clamp(yVal - 0.344136 * cbVal - 0.714136 * crVal);
					b = Clamp(yVal + 1.772 * cbVal);
				}

				SetPixelPremul(bgra, (py * width + px) * 4, r, g, b, 255);
			}
		}

		return new DecodedImage(width, height, new[] { bgra }, new[] { 0 });
	}

	private static int? TryReadExifOrientation(byte[] d, int start, int end)
	{
		// "Exif\0\0" then a TIFF header (byte-order, 0x002A, IFD0 offset), all offsets relative to the TIFF start.
		if (end - start < 14 || d[start] != 'E' || d[start + 1] != 'x' || d[start + 2] != 'i' || d[start + 3] != 'f' || d[start + 4] != 0)
		{
			return null;
		}

		var tiff = start + 6;
		var little = d[tiff] == 'I' && d[tiff + 1] == 'I';
		var ifd = tiff + (int)ReadExif32(d, tiff + 4, little);
		if (ifd + 2 > end)
		{
			return null;
		}

		var count = ReadExif16(d, ifd, little);
		for (var i = 0; i < count; i++)
		{
			var entry = ifd + 2 + i * 12;
			if (entry + 12 > end)
			{
				break;
			}

			if (ReadExif16(d, entry, little) == 0x0112) // Orientation
			{
				var value = ReadExif16(d, entry + 8, little);
				return value is >= 1 and <= 8 ? value : null;
			}
		}

		return null;
	}

	private static DecodedImage ApplyExifOrientation(DecodedImage source, int orientation)
	{
		var w = source.Width;
		var h = source.Height;
		var swap = orientation >= 5;
		var nw = swap ? h : w;
		var nh = swap ? w : h;
		var src = source.Frames[0];
		var dst = new byte[nw * nh * 4];

		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				int dx, dy;
				switch (orientation)
				{
					case 2: dx = w - 1 - x; dy = y; break;
					case 3: dx = w - 1 - x; dy = h - 1 - y; break;
					case 4: dx = x; dy = h - 1 - y; break;
					case 5: dx = y; dy = x; break;
					case 6: dx = h - 1 - y; dy = x; break;
					case 7: dx = h - 1 - y; dy = w - 1 - x; break;
					default: dx = y; dy = w - 1 - x; break; // 8
				}

				var s = (y * w + x) * 4;
				var o = (dy * nw + dx) * 4;
				dst[o] = src[s];
				dst[o + 1] = src[s + 1];
				dst[o + 2] = src[s + 2];
				dst[o + 3] = src[s + 3];
			}
		}

		return new DecodedImage(nw, nh, new[] { dst }, source.DurationsMs);
	}

	private static int ReadExif16(byte[] d, int o, bool little) => little ? d[o] | (d[o + 1] << 8) : (d[o] << 8) | d[o + 1];

	private static uint ReadExif32(byte[] d, int o, bool little) => little
		? (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24))
		: (uint)((d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3]);

	private static int Sample(JpegComponent c, int x, int y, int hMax, int vMax)
	{
		// Bilinear chroma upsampling (reduces block-edge error vs nearest; exact for full-res luma where H==hMax).
		var fx = (x + 0.5) * c.H / hMax - 0.5;
		var fy = (y + 0.5) * c.V / vMax - 0.5;
		if (fx < 0) fx = 0;
		if (fy < 0) fy = 0;

		var x0 = Math.Min((int)fx, c.PlaneWidth - 1);
		var y0 = Math.Min((int)fy, c.PlaneHeight - 1);
		var x1 = Math.Min(x0 + 1, c.PlaneWidth - 1);
		var y1 = Math.Min(y0 + 1, c.PlaneHeight - 1);
		var tx = fx - x0;
		var ty = fy - y0;

		var plane = c.Plane!;
		var w = c.PlaneWidth;
		var top = plane[y0 * w + x0] * (1 - tx) + plane[y0 * w + x1] * tx;
		var bottom = plane[y1 * w + x0] * (1 - tx) + plane[y1 * w + x1] * tx;
		return (int)(top * (1 - ty) + bottom * ty + 0.5);
	}

	private static byte Clamp(double v) => (byte)Math.Clamp((int)(v + 0.5), 0, 255);

	private static readonly double[,] _idctCos = BuildIdctCos();

	private static double[,] BuildIdctCos()
	{
		var table = new double[8, 8];
		for (var x = 0; x < 8; x++)
		{
			for (var u = 0; u < 8; u++)
			{
				var cu = u == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
				table[x, u] = cu * Math.Cos((2 * x + 1) * u * Math.PI / 16);
			}
		}

		return table;
	}

	private static void Idct(int[] block, byte[] output)
	{
		Span<double> tmp = stackalloc double[64];

		// Rows.
		for (var yy = 0; yy < 8; yy++)
		{
			for (var xx = 0; xx < 8; xx++)
			{
				var sum = 0.0;
				for (var u = 0; u < 8; u++)
				{
					sum += _idctCos[xx, u] * block[yy * 8 + u];
				}

				tmp[yy * 8 + xx] = sum / 2;
			}
		}

		// Columns + level shift.
		for (var xx = 0; xx < 8; xx++)
		{
			for (var yy = 0; yy < 8; yy++)
			{
				var sum = 0.0;
				for (var v = 0; v < 8; v++)
				{
					sum += _idctCos[yy, v] * tmp[v * 8 + xx];
				}

				output[yy * 8 + xx] = (byte)Math.Clamp((int)(sum / 2 + 128.5), 0, 255);
			}
		}
	}

	private static readonly int[] ZigZag =
	{
		0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5,
		12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28,
		35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51,
		58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63,
	};

	private sealed class JpegComponent
	{
		public int Id;
		public int H;
		public int V;
		public int QuantId;
		public int DcTable;
		public int AcTable;
		public int DcPredictor;
		public int PlaneWidth;
		public int PlaneHeight;
		public byte[]? Plane;
	}

	private sealed class JpegHuffman
	{
		private readonly int[] _minCode = new int[17];
		private readonly int[] _maxCode = new int[17];
		private readonly int[] _valPtr = new int[17];
		private readonly byte[] _values;

		public JpegHuffman(byte[] counts, byte[] values)
		{
			_values = values;
			var code = 0;
			var k = 0;
			for (var l = 1; l <= 16; l++)
			{
				_valPtr[l] = k;
				_minCode[l] = code;
				code += counts[l - 1];
				_maxCode[l] = counts[l - 1] > 0 ? code - 1 : -1;
				code <<= 1;
				k += counts[l - 1];
			}
		}

		public int Decode(JpegBitReader reader)
		{
			var code = 0;
			for (var l = 1; l <= 16; l++)
			{
				code = (code << 1) | reader.ReadBit();
				if (_maxCode[l] >= 0 && code <= _maxCode[l])
				{
					return _values[_valPtr[l] + code - _minCode[l]];
				}
			}

			return 0;
		}
	}

	private sealed class JpegBitReader
	{
		private readonly byte[] _d;
		private int _pos;
		private int _bitBuffer;
		private int _bitCount;

		public JpegBitReader(byte[] d, int start)
		{
			_d = d;
			_pos = start;
		}

		public int ReadBit()
		{
			if (_bitCount == 0)
			{
				if (_pos >= _d.Length)
				{
					return 0;
				}

				var b = _d[_pos++];
				if (b == 0xFF)
				{
					var next = _pos < _d.Length ? _d[_pos] : 0;
					if (next == 0x00)
					{
						_pos++; // stuffed FF
					}
					else
					{
						// Marker reached (e.g. restart/EOI) — stop feeding bits until Restart()/end.
						_pos--;
						return 0;
					}
				}

				_bitBuffer = b;
				_bitCount = 8;
			}

			_bitCount--;
			return (_bitBuffer >> _bitCount) & 1;
		}

		public int ReceiveExtend(int size)
		{
			var value = 0;
			for (var i = 0; i < size; i++)
			{
				value = (value << 1) | ReadBit();
			}

			// Sign-extend.
			if (value < (1 << (size - 1)))
			{
				value += (-1 << size) + 1;
			}

			return value;
		}

		public void Restart()
		{
			_bitCount = 0;
			_bitBuffer = 0;

			// Skip to and past the RSTn marker.
			while (_pos + 1 < _d.Length)
			{
				if (_d[_pos] == 0xFF && _d[_pos + 1] >= 0xD0 && _d[_pos + 1] <= 0xD7)
				{
					_pos += 2;
					return;
				}

				_pos++;
			}
		}
	}
}
