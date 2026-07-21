#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageDecoder
{
	// JPEG decode: baseline (SOF0/1) and progressive (SOF2), Huffman only. Coefficients for every block are
	// accumulated across scans into a per-component buffer (indexed by zig-zag position), then dequantized,
	// de-zig-zagged, IDCT'd and colour-converted once all scans are read. Arithmetic/lossless fall back to Skia.
	private static bool TryDecodeJpeg(byte[] d, [NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;
		try
		{
			return new Jpeg(d).TryDecode(out decoded);
		}
		catch
		{
			decoded = null;
			return false;
		}
	}

	private sealed class Jpeg
	{
		private readonly byte[] _d;
		private readonly int[][] _quant = new int[4][];
		private readonly JpegHuffman?[] _huffDc = new JpegHuffman?[4];
		private readonly JpegHuffman?[] _huffAc = new JpegHuffman?[4];
		private int _restartInterval;
		private int _width, _height;
		private int _orientation = 1;
		private bool _progressive;
		private JpegComponent[]? _components;
		private int _hMax = 1, _vMax = 1;
		private int _mcusPerLine, _mcusPerColumn;

		// Scan state.
		private JpegBitReader _reader = null!;
		private int _eobrun;
		private int _successiveAcState;
		private int _successiveAcNextValue;

		public Jpeg(byte[] d) => _d = d;

		public bool TryDecode([NotNullWhen(true)] out DecodedImage? decoded)
		{
			decoded = null;
			var p = 2; // past SOI
			while (p + 1 < _d.Length)
			{
				if (_d[p] != 0xFF)
				{
					p++;
					continue;
				}

				var marker = _d[p + 1];
				p += 2;
				if (marker == 0xD9) // EOI
				{
					break;
				}

				if (marker is 0x01 || (marker >= 0xD0 && marker <= 0xD7))
				{
					continue; // standalone
				}

				var segLen = (_d[p] << 8) | _d[p + 1];
				var segStart = p + 2;
				var segEnd = p + segLen;

				switch (marker)
				{
					case 0xDB: ParseQuant(segStart, segEnd); break;
					case 0xC4: ParseHuffman(segStart, segEnd); break;
					case 0xDD: _restartInterval = (_d[segStart] << 8) | _d[segStart + 1]; break;
					case 0xE1: _orientation = TryReadExifOrientation(_d, segStart, segEnd) ?? _orientation; break;
					case 0xC0 or 0xC1 or 0xC2: // baseline / extended / progressive
						_progressive = marker == 0xC2;
						ParseFrame(segStart);
						break;
					case 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF:
						return false; // arithmetic / lossless / hierarchical
					case 0xDA: // SOS
						p = DecodeScan(segStart, segEnd);
						continue;
				}

				p = segEnd;
			}

			if (_components is null || _width <= 0 || _height <= 0)
			{
				return false;
			}

			decoded = Reconstruct();
			if (_orientation > 1)
			{
				decoded = ApplyExifOrientation(decoded, _orientation);
			}

			return true;
		}

		private void ParseQuant(int q, int end)
		{
			while (q < end)
			{
				var pqTq = _d[q++];
				var precision16 = (pqTq >> 4) != 0;
				var table = new int[64];
				for (var i = 0; i < 64; i++)
				{
					table[i] = precision16 ? (_d[q] << 8) | _d[q + 1] : _d[q];
					q += precision16 ? 2 : 1;
				}

				_quant[pqTq & 0x0F] = table;
			}
		}

		private void ParseHuffman(int q, int end)
		{
			while (q < end)
			{
				var tcTh = _d[q++];
				var counts = new byte[16];
				var total = 0;
				for (var i = 0; i < 16; i++)
				{
					counts[i] = _d[q++];
					total += counts[i];
				}

				var symbols = new byte[total];
				Array.Copy(_d, q, symbols, 0, total);
				q += total;

				var table = new JpegHuffman(counts, symbols);
				if ((tcTh >> 4) == 0)
				{
					_huffDc[tcTh & 0x0F] = table;
				}
				else
				{
					_huffAc[tcTh & 0x0F] = table;
				}
			}
		}

		private void ParseFrame(int q)
		{
			q++; // precision
			_height = (_d[q] << 8) | _d[q + 1];
			_width = (_d[q + 2] << 8) | _d[q + 3];
			var count = _d[q + 4];
			q += 5;

			_components = new JpegComponent[count];
			for (var i = 0; i < count; i++)
			{
				var c = new JpegComponent { Id = _d[q], H = _d[q + 1] >> 4, V = _d[q + 1] & 0x0F, QuantId = _d[q + 2] };
				_components[i] = c;
				_hMax = Math.Max(_hMax, c.H);
				_vMax = Math.Max(_vMax, c.V);
				q += 3;
			}

			_mcusPerLine = (_width + 8 * _hMax - 1) / (8 * _hMax);
			_mcusPerColumn = (_height + 8 * _vMax - 1) / (8 * _vMax);

			foreach (var c in _components)
			{
				c.BlocksPerLine = (int)Math.Ceiling((double)_width * c.H / _hMax / 8);
				c.BlocksPerColumn = (int)Math.Ceiling((double)_height * c.V / _vMax / 8);
				c.BlocksPerLineForMcu = _mcusPerLine * c.H;
				c.BlocksPerColumnForMcu = _mcusPerColumn * c.V;
				c.Coeffs = new int[c.BlocksPerLineForMcu * c.BlocksPerColumnForMcu * 64];
			}
		}

		private int DecodeScan(int q, int segEnd)
		{
			var scanCount = _d[q++];
			var scanComponents = new JpegComponent[scanCount];
			for (var i = 0; i < scanCount; i++)
			{
				var selector = _d[q];
				var tables = _d[q + 1];
				q += 2;
				foreach (var c in _components!)
				{
					if (c.Id == selector)
					{
						c.DcTable = tables >> 4;
						c.AcTable = tables & 0x0F;
						scanComponents[i] = c;
					}
				}
			}

			var ss = _d[q];
			var se = _d[q + 1];
			var ah = _d[q + 2] >> 4;
			var al = _d[q + 2] & 0x0F;

			_reader = new JpegBitReader(_d, segEnd);
			_eobrun = 0;
			_successiveAcState = 0;
			foreach (var c in scanComponents)
			{
				c.Pred = 0;
			}

			var decodeBlock = SelectDecoder(ss, se, ah, al);
			var mcu = 0;
			var interval = _restartInterval;

			if (scanComponents.Length == 1)
			{
				var c = scanComponents[0];
				var total = c.BlocksPerLine * c.BlocksPerColumn;
				for (var n = 0; n < total; n++)
				{
					if (interval > 0 && n > 0 && n % interval == 0)
					{
						RestartScan(scanComponents);
					}

					var row = n / c.BlocksPerLine;
					var col = n % c.BlocksPerLine;
					var offset = (row * c.BlocksPerLineForMcu + col) * 64;
					decodeBlock(c, offset);
				}
			}
			else
			{
				var totalMcu = _mcusPerLine * _mcusPerColumn;
				for (var m = 0; m < totalMcu; m++)
				{
					if (interval > 0 && m > 0 && m % interval == 0)
					{
						RestartScan(scanComponents);
					}

					var mcuRow = m / _mcusPerLine;
					var mcuCol = m % _mcusPerLine;
					foreach (var c in scanComponents)
					{
						for (var by = 0; by < c.V; by++)
						{
							for (var bx = 0; bx < c.H; bx++)
							{
								var row = mcuRow * c.V + by;
								var col = mcuCol * c.H + bx;
								var offset = (row * c.BlocksPerLineForMcu + col) * 64;
								decodeBlock(c, offset);
							}
						}
					}

					mcu++;
				}
			}

			return FindNextMarker(_reader.Position);
		}

		private void RestartScan(JpegComponent[] scanComponents)
		{
			_reader.Restart();
			_eobrun = 0;
			_successiveAcState = 0;
			foreach (var c in scanComponents)
			{
				c.Pred = 0;
			}
		}

		private Action<JpegComponent, int> SelectDecoder(int ss, int se, int ah, int al)
		{
			if (!_progressive)
			{
				return (c, off) => DecodeBaseline(c, off);
			}

			if (ss == 0)
			{
				return ah == 0
					? (c, off) => DecodeDcFirst(c, off, al)
					: (c, off) => DecodeDcSuccessive(c, off, al);
			}

			return ah == 0
				? (c, off) => DecodeAcFirst(c, off, ss, se, al)
				: (c, off) => DecodeAcSuccessive(c, off, ss, se, al);
		}

		private void DecodeBaseline(JpegComponent c, int offset)
		{
			var dc = _huffDc[c.DcTable]!;
			var ac = _huffAc[c.AcTable]!;
			var t = dc.Decode(_reader);
			c.Pred += t == 0 ? 0 : _reader.ReceiveExtend(t);
			c.Coeffs![offset] = c.Pred;

			var k = 1;
			while (k < 64)
			{
				var rs = ac.Decode(_reader);
				var s = rs & 15;
				var r = rs >> 4;
				if (s == 0)
				{
					if (r < 15)
					{
						break;
					}

					k += 16;
					continue;
				}

				k += r;
				if (k > 63)
				{
					break;
				}

				c.Coeffs[offset + k] = _reader.ReceiveExtend(s);
				k++;
			}
		}

		private void DecodeDcFirst(JpegComponent c, int offset, int al)
		{
			var t = _huffDc[c.DcTable]!.Decode(_reader);
			c.Pred += t == 0 ? 0 : _reader.ReceiveExtend(t);
			c.Coeffs![offset] = c.Pred << al;
		}

		private void DecodeDcSuccessive(JpegComponent c, int offset, int al)
		{
			c.Coeffs![offset] |= _reader.ReadBit() << al;
		}

		private void DecodeAcFirst(JpegComponent c, int offset, int ss, int se, int al)
		{
			if (_eobrun > 0)
			{
				_eobrun--;
				return;
			}

			var ac = _huffAc[c.AcTable]!;
			var k = ss;
			while (k <= se)
			{
				var rs = ac.Decode(_reader);
				var s = rs & 15;
				var r = rs >> 4;
				if (s == 0)
				{
					if (r < 15)
					{
						_eobrun = _reader.ReadBits(r) + (1 << r) - 1;
						break;
					}

					k += 16;
					continue;
				}

				k += r;
				if (k > se)
				{
					break;
				}

				c.Coeffs![offset + k] = _reader.ReceiveExtend(s) * (1 << al);
				k++;
			}
		}

		private void DecodeAcSuccessive(JpegComponent c, int offset, int ss, int se, int al)
		{
			var ac = _huffAc[c.AcTable]!;
			var k = ss;
			var r = 0;
			while (k <= se)
			{
				var z = offset + k;
				var sign = c.Coeffs![z] < 0 ? -1 : 1;
				switch (_successiveAcState)
				{
					case 0: // initial
						var rs = ac.Decode(_reader);
						var s = rs & 15;
						r = rs >> 4;
						if (s == 0)
						{
							if (r < 15)
							{
								_eobrun = _reader.ReadBits(r) + (1 << r);
								_successiveAcState = 4;
							}
							else
							{
								r = 16;
								_successiveAcState = 1;
							}
						}
						else
						{
							_successiveAcNextValue = _reader.ReceiveExtend(s);
							_successiveAcState = r != 0 ? 2 : 3;
						}

						continue;
					case 1:
					case 2: // skipping r zero items
						if (c.Coeffs[z] != 0)
						{
							c.Coeffs[z] += sign * (_reader.ReadBit() << al);
						}
						else if (--r == 0)
						{
							_successiveAcState = _successiveAcState == 2 ? 3 : 0;
						}

						break;
					case 3: // set value for a zero item
						if (c.Coeffs[z] != 0)
						{
							c.Coeffs[z] += sign * (_reader.ReadBit() << al);
						}
						else
						{
							c.Coeffs[z] = _successiveAcNextValue << al;
							_successiveAcState = 0;
						}

						break;
					case 4: // EOB run
						if (c.Coeffs[z] != 0)
						{
							c.Coeffs[z] += sign * (_reader.ReadBit() << al);
						}

						break;
				}

				k++;
			}

			if (_successiveAcState == 4 && --_eobrun == 0)
			{
				_successiveAcState = 0;
			}
		}

		private int FindNextMarker(int from)
		{
			var p = from;
			while (p + 1 < _d.Length)
			{
				if (_d[p] == 0xFF)
				{
					var next = _d[p + 1];
					if (next != 0x00 && !(next >= 0xD0 && next <= 0xD7))
					{
						return p;
					}
				}

				p++;
			}

			return _d.Length;
		}

		private DecodedImage Reconstruct()
		{
			var natural = new int[64];
			var spatial = new byte[64];
			foreach (var c in _components!)
			{
				var qt = _quant[c.QuantId];
				c.PlaneWidth = c.BlocksPerLineForMcu * 8;
				c.PlaneHeight = c.BlocksPerColumnForMcu * 8;
				c.Plane = new byte[c.PlaneWidth * c.PlaneHeight];

				for (var blockRow = 0; blockRow < c.BlocksPerColumnForMcu; blockRow++)
				{
					for (var blockCol = 0; blockCol < c.BlocksPerLineForMcu; blockCol++)
					{
						var baseOffset = (blockRow * c.BlocksPerLineForMcu + blockCol) * 64;
						Array.Clear(natural, 0, 64);
						for (var k = 0; k < 64; k++)
						{
							natural[ZigZag[k]] = c.Coeffs![baseOffset + k] * qt[k];
						}

						Idct(natural, spatial);

						var px = blockCol * 8;
						var py = blockRow * 8;
						for (var yy = 0; yy < 8; yy++)
						{
							var row = (py + yy) * c.PlaneWidth + px;
							for (var xx = 0; xx < 8; xx++)
							{
								c.Plane[row + xx] = spatial[yy * 8 + xx];
							}
						}
					}
				}
			}

			return ToRgb(_width, _height, _components, _hMax, _vMax);
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
		// Bilinear chroma upsampling (exact for full-res luma where H==hMax).
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
		public int Pred;
		public int BlocksPerLine;
		public int BlocksPerColumn;
		public int BlocksPerLineForMcu;
		public int BlocksPerColumnForMcu;
		public int[]? Coeffs;
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

		public int Position => _pos;

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
						_pos--; // marker reached — stop feeding bits
						return 0;
					}
				}

				_bitBuffer = b;
				_bitCount = 8;
			}

			_bitCount--;
			return (_bitBuffer >> _bitCount) & 1;
		}

		public int ReadBits(int count)
		{
			var value = 0;
			for (var i = 0; i < count; i++)
			{
				value = (value << 1) | ReadBit();
			}

			return value;
		}

		public int ReceiveExtend(int size)
		{
			var value = ReadBits(size);
			return value < (1 << (size - 1)) ? value + (-1 << size) + 1 : value;
		}

		public void Restart()
		{
			_bitCount = 0;
			_bitBuffer = 0;
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
