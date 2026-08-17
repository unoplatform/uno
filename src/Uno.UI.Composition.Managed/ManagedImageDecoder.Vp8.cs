#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

internal static partial class ManagedImageDecoder
{
	// WebP lossy: VP8 key-frame (intra) decoder — boolean entropy decode, coefficient tokens, dequant,
	// inverse WHT/DCT, intra prediction, optional in-loop deblocking, YUV->RGB. Ported from RFC 6386 / libwebp.
	private static bool TryDecodeVp8(byte[] data, int offset, int length, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out DecodedImage? decoded)
	{
		decoded = null;
		try
		{
			var dec = new Vp8FullDecoder(data, offset, length);
			var bgra = dec.Decode();
			if (bgra is null)
			{
				return false;
			}

			decoded = new DecodedImage(dec.Width, dec.Height, new[] { bgra }, new[] { 0 });
			return true;
		}
		catch
		{
			return false;
		}
	}

	private sealed class Vp8FullDecoder
	{
		private readonly byte[] _data;
		private readonly int _offset;
		private readonly int _length;

		public int Width { get; private set; }
		public int Height { get; private set; }

		private int _mbW, _mbH;
		private Vp8BoolDecoder _bd = null!;             // first partition (header + modes)
		private Vp8BoolDecoder[] _tokens = null!;       // token partitions (coefficients)

		// Coefficient probabilities (mutable copy of the defaults, updated by the header).
		private readonly byte[,,,] _coeffProba = new byte[4, 8, 3, 11];

		// Segmentation.
		private bool _segEnabled, _segUpdateMap, _segAbsDelta;
		private readonly int[] _segQuant = new int[4];
		private readonly int[] _segFilter = new int[4];
		private readonly byte[] _segTreeProbs = { 255, 255, 255 };

		private int _baseQ;
		private int _y1DcDelta, _y2DcDelta, _y2AcDelta, _uvDcDelta, _uvAcDelta;

		// Per-segment dequant factors [seg][ y1dc,y1ac,y2dc,y2ac,uvdc,uvac ].
		private readonly int[,] _dq = new int[4, 6];

		private bool _skipEnabled;
		private int _skipProb;

		// Filter header.
		private bool _filterSimple;
		private int _filterLevel;
		private int _sharpness;
		private bool _lfDeltaEnabled;
		private readonly int[] _lfRefDelta = new int[4];
		private readonly int[] _lfModeDelta = new int[4];

		// Output planes (no borders; neighbours resolved with defaults).
		private byte[] _y = null!, _u = null!, _v = null!;
		private int _yStride, _cStride;

		// Non-zero contexts.
		private byte[] _aboveY = null!, _aboveU = null!, _aboveV = null!, _aboveY2 = null!;
		private readonly byte[] _leftY = new byte[4];
		private readonly byte[] _leftU = new byte[2];
		private readonly byte[] _leftV = new byte[2];
		private byte _leftY2;

		// Per-MB info for the loop filter.
		private int[] _mbSegment = null!;
		private bool[] _mbSkip = null!;
		private bool[] _mbHasNonZero = null!;
		private int[] _mbYMode = null!;

		public Vp8FullDecoder(byte[] data, int offset, int length)
		{
			_data = data;
			_offset = offset;
			_length = length;
		}

		public byte[]? Decode()
		{
			if (!ParseFrameHeader())
			{
				return null;
			}

			_mbW = (Width + 15) / 16;
			_mbH = (Height + 15) / 16;
			_yStride = _mbW * 16;
			_cStride = _mbW * 8;
			_y = new byte[_yStride * _mbH * 16];
			_u = new byte[_cStride * _mbH * 8];
			_v = new byte[_cStride * _mbH * 8];

			_aboveY = new byte[_mbW * 4];
			_aboveU = new byte[_mbW * 2];
			_aboveV = new byte[_mbW * 2];
			_aboveY2 = new byte[_mbW];
			_aboveBModes = new int[_mbW * 4];
			_mbSegment = new int[_mbW * _mbH];
			_mbSkip = new bool[_mbW * _mbH];
			_mbHasNonZero = new bool[_mbW * _mbH];
			_mbYMode = new int[_mbW * _mbH];

			var coeffs = new short[25 * 16]; // 16 Y + 4 U + 4 V + 1 Y2, natural order

			for (var mbY = 0; mbY < _mbH; mbY++)
			{
				Array.Clear(_leftY, 0, 4);
				Array.Clear(_leftU, 0, 2);
				Array.Clear(_leftV, 0, 2);
				_leftY2 = 0;
				var token = _tokens[mbY % _tokens.Length];

				for (var mbX = 0; mbX < _mbW; mbX++)
				{
					DecodeMacroblock(mbX, mbY, token, coeffs);
				}
			}

			if (_filterLevel > 0)
			{
				LoopFilter();
			}

			return ToBgra();
		}

		// ---- header ----

		private bool ParseFrameHeader()
		{
			var o = _offset;
			var tag = _data[o] | (_data[o + 1] << 8) | (_data[o + 2] << 16);
			if ((tag & 1) != 0)
			{
				return false; // not a key frame
			}

			var firstPartSize = (tag >> 5) & 0x7FFFF;
			if (_data[o + 3] != 0x9D || _data[o + 4] != 0x01 || _data[o + 5] != 0x2A)
			{
				return false;
			}

			Width = (_data[o + 6] | (_data[o + 7] << 8)) & 0x3FFF;
			Height = (_data[o + 8] | (_data[o + 9] << 8)) & 0x3FFF;
			if (Width <= 0 || Height <= 0)
			{
				return false;
			}

			_bd = new Vp8BoolDecoder(_data, o + 10, firstPartSize);
			_bd.GetFlag(); // color space
			_bd.GetFlag(); // clamping

			ParseSegmentation(_bd);
			ParseFilterHeader(_bd);

			var log2Parts = _bd.GetValue(2);
			var nParts = 1 << log2Parts;

			var partStart = o + 10 + firstPartSize;
			SetupTokenPartitions(partStart, nParts);

			ParseQuant(_bd);
			_bd.GetFlag(); // refresh_entropy_probs (single frame: ignored)
			UpdateCoeffProbs(_bd);

			_skipEnabled = _bd.GetFlag() == 1;
			_skipProb = _skipEnabled ? _bd.GetValue(8) : 0;

			ComputeDequant();
			return true;
		}

		private void ParseSegmentation(Vp8BoolDecoder bd)
		{
			_segEnabled = bd.GetFlag() == 1;
			if (!_segEnabled)
			{
				return;
			}

			_segUpdateMap = bd.GetFlag() == 1;
			var updateData = bd.GetFlag() == 1;
			if (updateData)
			{
				_segAbsDelta = bd.GetFlag() == 1;
				for (var i = 0; i < 4; i++)
				{
					_segQuant[i] = bd.GetFlag() == 1 ? bd.GetSigned(7) : 0;
				}

				for (var i = 0; i < 4; i++)
				{
					_segFilter[i] = bd.GetFlag() == 1 ? bd.GetSigned(6) : 0;
				}
			}

			if (_segUpdateMap)
			{
				for (var i = 0; i < 3; i++)
				{
					_segTreeProbs[i] = bd.GetFlag() == 1 ? (byte)bd.GetValue(8) : (byte)255;
				}
			}
		}

		private void ParseFilterHeader(Vp8BoolDecoder bd)
		{
			_filterSimple = bd.GetFlag() == 1;
			_filterLevel = bd.GetValue(6);
			_sharpness = bd.GetValue(3);
			_lfDeltaEnabled = bd.GetFlag() == 1;
			if (_lfDeltaEnabled && bd.GetFlag() == 1)
			{
				for (var i = 0; i < 4; i++)
				{
					if (bd.GetFlag() == 1)
					{
						_lfRefDelta[i] = bd.GetSigned(6);
					}
				}

				for (var i = 0; i < 4; i++)
				{
					if (bd.GetFlag() == 1)
					{
						_lfModeDelta[i] = bd.GetSigned(6);
					}
				}
			}
		}

		private void SetupTokenPartitions(int start, int nParts)
		{
			_tokens = new Vp8BoolDecoder[nParts];
			var sizesEnd = start + (nParts - 1) * 3;
			var p = sizesEnd;
			var frameEnd = _offset + _length;
			for (var i = 0; i < nParts; i++)
			{
				int size;
				if (i < nParts - 1)
				{
					var s = start + i * 3;
					size = _data[s] | (_data[s + 1] << 8) | (_data[s + 2] << 16);
				}
				else
				{
					size = frameEnd - p;
				}

				_tokens[i] = new Vp8BoolDecoder(_data, p, size);
				p += size;
			}
		}

		private void ParseQuant(Vp8BoolDecoder bd)
		{
			_baseQ = bd.GetValue(7);
			_y1DcDelta = ReadDelta(bd);
			_y2DcDelta = ReadDelta(bd);
			_y2AcDelta = ReadDelta(bd);
			_uvDcDelta = ReadDelta(bd);
			_uvAcDelta = ReadDelta(bd);
		}

		private static int ReadDelta(Vp8BoolDecoder bd) => bd.GetFlag() == 1 ? bd.GetSigned(4) : 0;

		private void UpdateCoeffProbs(Vp8BoolDecoder bd)
		{
			for (var t = 0; t < 4; t++)
			{
				for (var b = 0; b < 8; b++)
				{
					for (var c = 0; c < 3; c++)
					{
						for (var p = 0; p < 11; p++)
						{
							var prob = bd.GetBit(Vp8CoeffUpdateProba[t, b, c, p]) == 1
								? bd.GetValue(8)
								: Vp8DefaultCoeffProba[t, b, c, p];
							_coeffProba[t, b, c, p] = (byte)prob;
						}
					}
				}
			}
		}

		private void ComputeDequant()
		{
			for (var s = 0; s < 4; s++)
			{
				var q = _baseQ;
				if (_segEnabled)
				{
					q = _segAbsDelta ? _segQuant[s] : _baseQ + _segQuant[s];
				}

				_dq[s, 0] = Vp8DcQuant(q + _y1DcDelta);
				_dq[s, 1] = Vp8AcQuant(q);
				_dq[s, 2] = Vp8DcQuant(q + _y2DcDelta) * 2;
				var y2ac = Vp8AcQuant(q + _y2AcDelta) * 155 / 100;
				_dq[s, 3] = y2ac < 8 ? 8 : y2ac;
				var uvdc = Vp8DcQuant(q + _uvDcDelta);
				_dq[s, 4] = uvdc > 132 ? 132 : uvdc;
				_dq[s, 5] = Vp8AcQuant(q + _uvAcDelta);
			}
		}

		private static int Vp8DcQuant(int q) => kDcTable[Math.Clamp(q, 0, 127)];
		private static int Vp8AcQuant(int q) => kAcTable[Math.Clamp(q, 0, 127)];

		// ---- macroblock decode ----

		private void DecodeMacroblock(int mbX, int mbY, Vp8BoolDecoder token, short[] coeffs)
		{
			var mbIndex = mbY * _mbW + mbX;

			var segment = 0;
			if (_segEnabled && _segUpdateMap)
			{
				segment = ReadTree(_bd, _segmentTree, _segTreeProbs, 0);
			}

			_mbSegment[mbIndex] = segment;

			var skip = _skipEnabled && _bd.GetBit(_skipProb) == 1;
			_mbSkip[mbIndex] = skip;

			// Y mode (keyframe): B_PRED or a 16x16 mode.
			var yMode = ReadTree(_bd, Vp8KfYModeTree, Vp8KfYModeProb, 0);
			_mbYMode[mbIndex] = yMode;

			var bModes = _bModes;
			if (yMode == B_PRED)
			{
				for (var i = 0; i < 16; i++)
				{
					var x = i & 3;
					var yy = i >> 2;
					var above = yy == 0 ? _aboveBModes[mbX * 4 + x] : bModes[i - 4];
					var left = x == 0 ? _leftBModes[yy] : bModes[i - 1];
					bModes[i] = ReadTree(_bd, Vp8BModeTree, GetBModeProbs(above, left), 0);
				}
			}
			else
			{
				var equiv = yMode switch { DC_PRED => B_DC_PRED, V_PRED => B_VE_PRED, H_PRED => B_HE_PRED, _ => B_TM_PRED };
				for (var i = 0; i < 16; i++)
				{
					bModes[i] = equiv;
				}
			}

			// Persist B-mode context for neighbours.
			for (var x = 0; x < 4; x++)
			{
				_aboveBModes[mbX * 4 + x] = bModes[12 + x];
			}

			for (var yy = 0; yy < 4; yy++)
			{
				_leftBModes[yy] = bModes[yy * 4 + 3];
			}

			var uvMode = ReadTree(_bd, Vp8UvModeTree, Vp8KfUvModeProb, 0);

			Array.Clear(coeffs, 0, coeffs.Length);
			var hasNonZero = false;
			if (!skip)
			{
				hasNonZero = DecodeResiduals(mbX, token, coeffs, yMode != B_PRED, segment);
			}
			else
			{
				// Reset non-zero contexts for a skipped MB.
				_leftY[0] = _leftY[1] = _leftY[2] = _leftY[3] = 0;
				_aboveY[mbX * 4] = _aboveY[mbX * 4 + 1] = _aboveY[mbX * 4 + 2] = _aboveY[mbX * 4 + 3] = 0;
				_leftU[0] = _leftU[1] = _leftV[0] = _leftV[1] = 0;
				_aboveU[mbX * 2] = _aboveU[mbX * 2 + 1] = _aboveV[mbX * 2] = _aboveV[mbX * 2 + 1] = 0;
				if (yMode != B_PRED)
				{
					_leftY2 = _aboveY2[mbX] = 0;
				}
			}

			_mbHasNonZero[mbIndex] = hasNonZero;

			ReconstructLuma(mbX, mbY, yMode, bModes, coeffs);
			ReconstructChroma(mbX, mbY, uvMode, coeffs);
		}

		private byte[] GetBModeProbs(int above, int left)
		{
			var probs = _bModeProbsScratch;
			for (var i = 0; i < 9; i++)
			{
				probs[i] = Vp8KfBModeProba[above, left, i];
			}

			return probs;
		}

		// Decode all residual blocks of the MB; returns whether any coefficient is non-zero.
		private bool DecodeResiduals(int mbX, Vp8BoolDecoder token, short[] coeffs, bool hasY2, int segment)
		{
			var any = false;
			var firstCoeff = 0;

			if (hasY2)
			{
				var ctx = _leftY2 + _aboveY2[mbX];
				var y2 = new short[16];
				var nz = DecodeBlock(token, 1, ctx, _dq[segment, 2], _dq[segment, 3], y2, 0);
				var flag = nz > 0;
				_leftY2 = _aboveY2[mbX] = (byte)(flag ? 1 : 0);
				any |= flag;

				InverseWht(y2, coeffs); // scatter 16 DC values into each Y block's coeff[0]
				firstCoeff = 1;
			}

			// Luma 16 blocks.
			for (var i = 0; i < 16; i++)
			{
				var x = i & 3;
				var yy = i >> 2;
				var ctx = _leftY[yy] + _aboveY[mbX * 4 + x];
				var block = new short[16];
				var savedDc = coeffs[i * 16]; // DC from WHT (if hasY2)
				var nz = DecodeBlock(token, hasY2 ? 0 : 3, ctx, _dq[segment, 0], _dq[segment, 1], block, firstCoeff);
				var flag = nz > firstCoeff;
				_leftY[yy] = _aboveY[mbX * 4 + x] = (byte)(flag ? 1 : 0);
				any |= flag || (hasY2 && savedDc != 0);

				for (var k = 0; k < 16; k++)
				{
					coeffs[i * 16 + k] = block[k];
				}

				if (hasY2)
				{
					coeffs[i * 16] = savedDc;
				}
			}

			// Chroma: U (blocks 16..19), V (blocks 20..23).
			for (var plane = 0; plane < 2; plane++)
			{
				var above = plane == 0 ? _aboveU : _aboveV;
				var left = plane == 0 ? _leftU : _leftV;
				for (var i = 0; i < 4; i++)
				{
					var x = i & 1;
					var yy = i >> 1;
					var ctx = left[yy] + above[mbX * 2 + x];
					var block = new short[16];
					var nz = DecodeBlock(token, 2, ctx, _dq[segment, 4], _dq[segment, 5], block, 0);
					var flag = nz > 0;
					left[yy] = above[mbX * 2 + x] = (byte)(flag ? 1 : 0);
					any |= flag;
					var dst = (16 + plane * 4 + i) * 16;
					for (var k = 0; k < 16; k++)
					{
						coeffs[dst + k] = block[k];
					}
				}
			}

			return any;
		}

		// Decode a single 4x4 block's coefficients (natural order into `out`); returns the coeff count (EOB position).
		private int DecodeBlock(Vp8BoolDecoder bd, int type, int ctx, int dqDc, int dqAc, short[] outNatural, int first)
		{
			var n = first;
			for (; n < 16; n++)
			{
				var band = Vp8CoeffBands[n];
				if (bd.GetBit(_coeffProba[type, band, ctx, 0]) == 0)
				{
					return n; // EOB
				}

				while (bd.GetBit(_coeffProba[type, band, ctx, 1]) == 0)
				{
					ctx = 0;
					if (++n == 16)
					{
						return 16;
					}

					band = Vp8CoeffBands[n];
				}

				int v;
				if (bd.GetBit(_coeffProba[type, band, ctx, 2]) == 0)
				{
					v = 1;
					ctx = 1;
				}
				else
				{
					v = GetLargeValue(bd, type, band, ctx);
					ctx = 2;
				}

				var value = bd.GetBit(128) == 1 ? -v : v;
				outNatural[Vp8Zigzag[n]] = (short)(value * (n == 0 ? dqDc : dqAc));
			}

			return 16;
		}

		private int GetLargeValue(Vp8BoolDecoder bd, int type, int band, int ctx)
		{
			byte P(int i) => _coeffProba[type, band, ctx, i];

			if (bd.GetBit(P(3)) == 0)
			{
				return bd.GetBit(P(4)) == 0 ? 2 : 3 + bd.GetBit(P(5));
			}

			int cat;
			if (bd.GetBit(P(6)) == 0)
			{
				cat = bd.GetBit(P(7)); // 0 -> cat1, 1 -> cat2
			}
			else
			{
				var bit1 = bd.GetBit(P(8));
				var bit0 = bd.GetBit(P(9 + bit1));
				cat = 2 + 2 * bit1 + bit0; // cat3..cat6
			}

			var probs = Vp8CatProbs[cat];
			var value = 0;
			foreach (var prob in probs)
			{
				value = (value << 1) | bd.GetBit(prob);
			}

			return Vp8CatBase[cat] + value;
		}

		private static int ReadTree(Vp8BoolDecoder bd, sbyte[] tree, byte[] probs, int start)
		{
			var i = start;
			while ((i = tree[i + bd.GetBit(probs[i >> 1])]) > 0)
			{
			}

			return -i;
		}

		// ---- inverse transforms ----

		private static void InverseWht(short[] input, short[] outputCoeffs)
		{
			Span<int> tmp = stackalloc int[16];
			for (var i = 0; i < 4; i++)
			{
				var a = input[i] + input[12 + i];
				var b = input[4 + i] + input[8 + i];
				var c = input[4 + i] - input[8 + i];
				var dd = input[i] - input[12 + i];
				tmp[i] = a + b;
				tmp[4 + i] = dd + c;
				tmp[8 + i] = a - b;
				tmp[12 + i] = dd - c;
			}

			for (var i = 0; i < 4; i++)
			{
				var a = tmp[i * 4] + 3;
				var b = tmp[i * 4 + 3];
				var c = tmp[i * 4 + 1];
				var dd = tmp[i * 4 + 2];
				var a1 = a + b;
				var b1 = a - b;
				var c1 = c + dd; // note: matches libwebp WHT ordering below
				var d1 = c - dd;
				// libwebp distributes to each block's DC (coeff index 0) in raster block order.
				outputCoeffs[(i * 4 + 0) * 16] = (short)((a1 + c1) >> 3);
				outputCoeffs[(i * 4 + 1) * 16] = (short)((d1 + b1) >> 3);
				outputCoeffs[(i * 4 + 2) * 16] = (short)((a1 - c1) >> 3);
				outputCoeffs[(i * 4 + 3) * 16] = (short)((b1 - d1) >> 3);
			}
		}

		private static void InverseDct(short[] block, int blockOffset, byte[] plane, int dst, int stride)
		{
			// Mirrors libwebp TransformOne: vertical pass over columns into tmp[i*4+j], then horizontal pass.
			Span<int> tmp = stackalloc int[16];
			for (var i = 0; i < 4; i++)
			{
				var c0 = block[blockOffset + i];
				var c8 = block[blockOffset + i + 8];
				var c4 = block[blockOffset + i + 4];
				var c12 = block[blockOffset + i + 12];
				var a = c0 + c8;
				var b = c0 - c8;
				var c = Mul2(c4) - Mul1(c12);
				var d = Mul1(c4) + Mul2(c12);
				tmp[i * 4 + 0] = a + d;
				tmp[i * 4 + 1] = b + c;
				tmp[i * 4 + 2] = b - c;
				tmp[i * 4 + 3] = a - d;
			}

			for (var i = 0; i < 4; i++)
			{
				var dc = tmp[i] + 4;
				var a = dc + tmp[i + 8];
				var b = dc - tmp[i + 8];
				var c = Mul2(tmp[i + 4]) - Mul1(tmp[i + 12]);
				var d = Mul1(tmp[i + 4]) + Mul2(tmp[i + 12]);
				var row = dst + i * stride;
				Store(plane, row, (a + d) >> 3);
				Store(plane, row + 1, (b + c) >> 3);
				Store(plane, row + 2, (b - c) >> 3);
				Store(plane, row + 3, (a - d) >> 3);
			}
		}

		private static int Mul1(int a) => ((a * 20091) >> 16) + a;
		private static int Mul2(int a) => (a * 35468) >> 16;

		private static void Store(byte[] plane, int i, int residual)
		{
			plane[i] = Clip8(plane[i] + residual);
		}

		private static byte Clip8(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);

		// ---- reconstruction (prediction + residual add) ----

		private void ReconstructLuma(int mbX, int mbY, int yMode, int[] bModes, short[] coeffs)
		{
			var x0 = mbX * 16;
			var y0 = mbY * 16;
			if (yMode == B_PRED)
			{
				for (var i = 0; i < 16; i++)
				{
					var sx = x0 + (i & 3) * 4;
					var sy = y0 + (i >> 2) * 4;
					PredictB(sx, sy, bModes[i]);
					InverseDct(coeffs, i * 16, _y, sy * _yStride + sx, _yStride);
				}
			}
			else
			{
				Predict16(x0, y0, yMode);
				for (var i = 0; i < 16; i++)
				{
					var sx = x0 + (i & 3) * 4;
					var sy = y0 + (i >> 2) * 4;
					InverseDct(coeffs, i * 16, _y, sy * _yStride + sx, _yStride);
				}
			}
		}

		private void ReconstructChroma(int mbX, int mbY, int uvMode, short[] coeffs)
		{
			ReconstructChromaPlane(_u, mbX, mbY, uvMode, coeffs, 16);
			ReconstructChromaPlane(_v, mbX, mbY, uvMode, coeffs, 20);
		}

		private void ReconstructChromaPlane(byte[] plane, int mbX, int mbY, int uvMode, short[] coeffs, int blockBase)
		{
			var x0 = mbX * 8;
			var y0 = mbY * 8;
			Predict8(plane, x0, y0, uvMode);
			for (var i = 0; i < 4; i++)
			{
				var sx = x0 + (i & 1) * 4;
				var sy = y0 + (i >> 1) * 4;
				InverseDct(coeffs, (blockBase + i) * 16, plane, sy * _cStride + sx, _cStride);
			}
		}

		// 16x16 / 8x8 whole-block prediction.
		private void Predict16(int x0, int y0, int mode) => PredictBlock(_y, _yStride, x0, y0, 16, mode);
		private void Predict8(byte[] plane, int x0, int y0, int mode) => PredictBlock(plane, _cStride, x0, y0, 8, mode);

		private static void PredictBlock(byte[] plane, int stride, int x0, int y0, int size, int mode)
		{
			var hasTop = y0 > 0;
			var hasLeft = x0 > 0;

			switch (mode)
			{
				case V_PRED:
					for (var yy = 0; yy < size; yy++)
					{
						for (var xx = 0; xx < size; xx++)
						{
							plane[(y0 + yy) * stride + x0 + xx] = hasTop ? plane[(y0 - 1) * stride + x0 + xx] : (byte)127;
						}
					}
					break;
				case H_PRED:
					for (var yy = 0; yy < size; yy++)
					{
						var l = hasLeft ? plane[(y0 + yy) * stride + x0 - 1] : (byte)129;
						for (var xx = 0; xx < size; xx++)
						{
							plane[(y0 + yy) * stride + x0 + xx] = l;
						}
					}
					break;
				case TM_PRED:
					var corner = hasTop ? (hasLeft ? plane[(y0 - 1) * stride + x0 - 1] : (byte)129) : (byte)127;
					for (var yy = 0; yy < size; yy++)
					{
						var l = hasLeft ? plane[(y0 + yy) * stride + x0 - 1] : (byte)129;
						for (var xx = 0; xx < size; xx++)
						{
							var t = hasTop ? plane[(y0 - 1) * stride + x0 + xx] : (byte)127;
							plane[(y0 + yy) * stride + x0 + xx] = Clip8(l + t - corner);
						}
					}
					break;
				default: // DC_PRED
					var sum = 0;
					var count = 0;
					if (hasTop)
					{
						for (var xx = 0; xx < size; xx++)
						{
							sum += plane[(y0 - 1) * stride + x0 + xx];
						}

						count += size;
					}

					if (hasLeft)
					{
						for (var yy = 0; yy < size; yy++)
						{
							sum += plane[(y0 + yy) * stride + x0 - 1];
						}

						count += size;
					}

					var dc = count > 0 ? (sum + count / 2) / count : 128;
					for (var yy = 0; yy < size; yy++)
					{
						for (var xx = 0; xx < size; xx++)
						{
							plane[(y0 + yy) * stride + x0 + xx] = (byte)dc;
						}
					}
					break;
			}
		}

		// 4x4 intra prediction (B modes). Uses a 13-sample context: corner, top[0..3], top-right[0..3], left[0..3].
		private void PredictB(int x0, int y0, int mode)
		{
			Span<int> a = stackalloc int[13]; // [0]=corner, [1..8]=top+topright, [9..12]=left
			var hasTop = y0 > 0;
			var hasLeft = x0 > 0;

			a[0] = hasTop ? (hasLeft ? _y[(y0 - 1) * _yStride + x0 - 1] : 129) : 127;
			for (var i = 0; i < 4; i++)
			{
				a[1 + i] = hasTop ? _y[(y0 - 1) * _yStride + x0 + i] : 127;
			}

			// Top-right 4 samples: available from the row above when within bounds; else replicate the last top sample.
			for (var i = 0; i < 4; i++)
			{
				var tx = x0 + 4 + i;
				if (hasTop && tx < _yStride)
				{
					a[5 + i] = _y[(y0 - 1) * _yStride + tx];
				}
				else
				{
					a[5 + i] = a[4]; // replicate top[3]
				}
			}

			for (var i = 0; i < 4; i++)
			{
				a[9 + i] = hasLeft ? _y[(y0 + i) * _yStride + x0 - 1] : 129;
			}

			Span<byte> pred = stackalloc byte[16];
			PredictB4x4(mode, a, pred);
			for (var yy = 0; yy < 4; yy++)
			{
				for (var xx = 0; xx < 4; xx++)
				{
					_y[(y0 + yy) * _yStride + x0 + xx] = pred[yy * 4 + xx];
				}
			}
		}

		private static void PredictB4x4(int mode, Span<int> a, Span<byte> o)
		{
			// Naming: L=left (a9..12), T=top (a1..4), TR=top-right (a5..8), C=corner (a0).
			int C = a[0];
			int T0 = a[1], T1 = a[2], T2 = a[3], T3 = a[4];
			int R0 = a[5], R1 = a[6], R2 = a[7], R3 = a[8];
			int L0 = a[9], L1 = a[10], L2 = a[11], L3 = a[12];

			static byte Avg3(int x, int y, int z) => (byte)((x + 2 * y + z + 2) >> 2);
			static byte Avg2(int x, int y) => (byte)((x + y + 1) >> 1);

			switch (mode)
			{
				case B_TM_PRED:
					for (var yy = 0; yy < 4; yy++)
					{
						var l = yy == 0 ? L0 : yy == 1 ? L1 : yy == 2 ? L2 : L3;
						o[yy * 4 + 0] = Clip8(l + T0 - C);
						o[yy * 4 + 1] = Clip8(l + T1 - C);
						o[yy * 4 + 2] = Clip8(l + T2 - C);
						o[yy * 4 + 3] = Clip8(l + T3 - C);
					}
					break;
				case B_VE_PRED:
				{
					var x0v = Avg3(C, T0, T1);
					var x1v = Avg3(T0, T1, T2);
					var x2v = Avg3(T1, T2, T3);
					var x3v = Avg3(T2, T3, R0);
					for (var yy = 0; yy < 4; yy++) { o[yy * 4] = x0v; o[yy * 4 + 1] = x1v; o[yy * 4 + 2] = x2v; o[yy * 4 + 3] = x3v; }
					break;
				}
				case B_HE_PRED:
				{
					var y0h = Avg3(C, L0, L1);
					var y1h = Avg3(L0, L1, L2);
					var y2h = Avg3(L1, L2, L3);
					var y3h = Avg3(L2, L3, L3);
					for (var xx = 0; xx < 4; xx++) { o[xx] = y0h; o[4 + xx] = y1h; o[8 + xx] = y2h; o[12 + xx] = y3h; }
					break;
				}
				case B_LD_PRED:
					o[0] = Avg3(T0, T1, T2);
					o[1] = Avg3(T1, T2, T3); o[4] = o[1];
					o[2] = Avg3(T2, T3, R0); o[5] = o[2]; o[8] = o[2];
					o[3] = Avg3(T3, R0, R1); o[6] = o[3]; o[9] = o[3]; o[12] = o[3];
					o[7] = Avg3(R0, R1, R2); o[10] = o[7]; o[13] = o[7];
					o[11] = Avg3(R1, R2, R3); o[14] = o[11];
					o[15] = Avg3(R2, R3, R3);
					break;
				case B_RD_PRED:
					o[12] = Avg3(L3, L2, L1);
					o[8] = Avg3(L2, L1, L0); o[13] = o[8];
					o[4] = Avg3(L1, L0, C); o[9] = o[4]; o[14] = o[4];
					o[0] = Avg3(L0, C, T0); o[5] = o[0]; o[10] = o[0]; o[15] = o[0];
					o[1] = Avg3(C, T0, T1); o[6] = o[1]; o[11] = o[1];
					o[2] = Avg3(T0, T1, T2); o[7] = o[2];
					o[3] = Avg3(T1, T2, T3);
					break;
				case B_VR_PRED:
					o[12] = Avg3(L2, L1, L0);
					o[8] = Avg3(L1, L0, C);
					o[4] = Avg3(L0, C, T0); o[13] = o[4];
					o[0] = Avg2(C, T0); o[9] = o[0];
					o[5] = Avg3(C, T0, T1); o[14] = o[5];
					o[1] = Avg2(T0, T1); o[10] = o[1];
					o[6] = Avg3(T0, T1, T2); o[15] = o[6];
					o[2] = Avg2(T1, T2); o[11] = o[2];
					o[7] = Avg3(T1, T2, T3);
					o[3] = Avg2(T2, T3);
					break;
				case B_VL_PRED:
					o[0] = Avg2(T0, T1);
					o[4] = Avg3(T0, T1, T2);
					o[8] = Avg2(T1, T2); o[1] = o[8];
					o[12] = Avg3(T1, T2, T3); o[5] = o[12];
					o[9] = Avg2(T2, T3); o[2] = o[9];
					o[13] = Avg3(T2, T3, R0); o[6] = o[13];
					o[10] = Avg2(T3, R0); o[3] = o[10];
					o[14] = Avg3(T3, R0, R1); o[7] = o[14];
					o[11] = Avg3(R0, R1, R2);
					o[15] = Avg3(R1, R2, R3);
					break;
				case B_HD_PRED:
					o[12] = Avg2(L3, L2);
					o[13] = Avg3(L3, L2, L1);
					o[8] = Avg2(L2, L1); o[14] = o[8];
					o[9] = Avg3(L2, L1, L0); o[15] = o[9];
					o[4] = Avg2(L1, L0); o[10] = o[4];
					o[5] = Avg3(L1, L0, C); o[11] = o[5];
					o[0] = Avg2(L0, C); o[6] = o[0];
					o[1] = Avg3(L0, C, T0); o[7] = o[1];
					o[2] = Avg3(C, T0, T1);
					o[3] = Avg3(T0, T1, T2);
					break;
				case B_HU_PRED:
					o[0] = Avg2(L0, L1);
					o[1] = Avg3(L0, L1, L2);
					o[2] = Avg2(L1, L2); o[4] = o[2];
					o[3] = Avg3(L1, L2, L3); o[5] = o[3];
					o[6] = Avg2(L2, L3); o[8] = o[6];
					o[7] = Avg3(L2, L3, L3); o[9] = o[7];
					o[10] = (byte)L3; o[11] = (byte)L3; o[12] = (byte)L3; o[13] = (byte)L3; o[14] = (byte)L3; o[15] = (byte)L3;
					break;
				default: // B_DC_PRED
					var sum = 4;
					for (var i = 0; i < 4; i++) { sum += a[1 + i] + a[9 + i]; }
					var dc = (byte)(sum >> 3);
					for (var i = 0; i < 16; i++) { o[i] = dc; }
					break;
			}
		}

		// ---- loop filter ----

		private void LoopFilter()
		{
			for (var mbY = 0; mbY < _mbH; mbY++)
			{
				for (var mbX = 0; mbX < _mbW; mbX++)
				{
					var idx = mbY * _mbW + mbX;
					var level = FilterLevelFor(idx);
					if (level == 0)
					{
						continue;
					}

					var interior = level;
					if (_sharpness > 0)
					{
						interior >>= _sharpness > 4 ? 2 : 1;
						if (interior > 9 - _sharpness)
						{
							interior = 9 - _sharpness;
						}
					}

					if (interior < 1)
					{
						interior = 1;
					}

					var hev = level >= 40 ? 2 : level >= 15 ? 1 : 0;
					var mbEdgeLimit = (level + 2) * 2 + interior;
					var innerEdgeLimit = level * 2 + interior;
					var filterInner = !_mbSkip[idx] || _mbYMode[idx] == B_PRED;

					if (_filterSimple)
					{
						SimpleFilterMb(mbX, mbY, mbEdgeLimit, innerEdgeLimit, filterInner);
					}
					else
					{
						NormalFilterMb(mbX, mbY, mbEdgeLimit, innerEdgeLimit, interior, hev, filterInner);
					}
				}
			}
		}

		private int FilterLevelFor(int idx)
		{
			var level = _filterLevel;
			if (_segEnabled)
			{
				var seg = _mbSegment[idx];
				level = _segAbsDelta ? _segFilter[seg] : _filterLevel + _segFilter[seg];
			}

			if (_lfDeltaEnabled)
			{
				level += _lfRefDelta[0]; // intra frame => ref 0
				if (_mbYMode[idx] == B_PRED)
				{
					level += _lfModeDelta[0];
				}
			}

			return Math.Clamp(level, 0, 63);
		}

		private void SimpleFilterMb(int mbX, int mbY, int mbLimit, int innerLimit, bool inner)
		{
			var x0 = mbX * 16;
			var y0 = mbY * 16;
			if (mbX > 0)
			{
				for (var i = 0; i < 16; i++) { SimpleFilterH(_y, (y0 + i) * _yStride + x0, mbLimit); }
			}

			if (inner)
			{
				for (var b = 4; b < 16; b += 4)
				{
					for (var i = 0; i < 16; i++) { SimpleFilterH(_y, (y0 + i) * _yStride + x0 + b, innerLimit); }
				}
			}

			if (mbY > 0)
			{
				for (var i = 0; i < 16; i++) { SimpleFilterV(_y, y0 * _yStride + x0 + i, _yStride, mbLimit); }
			}

			if (inner)
			{
				for (var b = 4; b < 16; b += 4)
				{
					for (var i = 0; i < 16; i++) { SimpleFilterV(_y, (y0 + b) * _yStride + x0 + i, _yStride, innerLimit); }
				}
			}
		}

		private static void SimpleFilterH(byte[] p, int i, int limit)
		{
			var p1 = p[i - 2]; var p0 = p[i - 1]; var q0 = p[i]; var q1 = p[i + 1];
			if (Math.Abs(p0 - q0) * 2 + (Math.Abs(p1 - q1) >> 1) <= limit)
			{
				CommonAdjust(p, i, 1, true);
			}
		}

		private static void SimpleFilterV(byte[] p, int i, int stride, int limit)
		{
			var p1 = p[i - 2 * stride]; var p0 = p[i - stride]; var q0 = p[i]; var q1 = p[i + stride];
			if (Math.Abs(p0 - q0) * 2 + (Math.Abs(p1 - q1) >> 1) <= limit)
			{
				CommonAdjust(p, i, stride, true);
			}
		}

		private static int CommonAdjust(byte[] p, int i, int step, bool useOuter)
		{
			var p1 = Sclip(p[i - 2 * step]);
			var p0 = Sclip(p[i - step]);
			var q0 = Sclip(p[i]);
			var q1 = Sclip(p[i + step]);
			var a = (useOuter ? 3 * (q0 - p0) : 0) + Clamp127(p1 - q1);
			var a1 = Clamp127(a + 4) >> 3;
			var a2 = Clamp127(a + 3) >> 3;
			p[i] = (byte)(Sclip(q0 - a1) + 128);
			p[i - step] = (byte)(Sclip(p0 + a2) + 128);
			return a1;
		}

		private static int Sclip(int v) => v - 128;
		private static int Clamp127(int v) => v < -128 ? -128 : v > 127 ? 127 : v;

		private void NormalFilterMb(int mbX, int mbY, int mbLimit, int innerLimit, int interior, int hevThresh, bool inner)
		{
			var yx = mbX * 16; var yy = mbY * 16;
			var cx = mbX * 8; var cy = mbY * 8;

			if (mbX > 0)
			{
				for (var i = 0; i < 16; i++) { FilterH(_y, (yy + i) * _yStride + yx, 1, mbLimit, interior, hevThresh, true); }
				for (var i = 0; i < 8; i++) { FilterH(_u, (cy + i) * _cStride + cx, 1, mbLimit, interior, hevThresh, true); FilterH(_v, (cy + i) * _cStride + cx, 1, mbLimit, interior, hevThresh, true); }
			}

			if (inner)
			{
				for (var b = 4; b < 16; b += 4)
				{
					for (var i = 0; i < 16; i++) { FilterH(_y, (yy + i) * _yStride + yx + b, 1, innerLimit, interior, hevThresh, false); }
				}

				for (var i = 0; i < 8; i++) { FilterH(_u, (cy + i) * _cStride + cx + 4, 1, innerLimit, interior, hevThresh, false); FilterH(_v, (cy + i) * _cStride + cx + 4, 1, innerLimit, interior, hevThresh, false); }
			}

			if (mbY > 0)
			{
				for (var i = 0; i < 16; i++) { FilterH(_y, yy * _yStride + yx + i, _yStride, mbLimit, interior, hevThresh, true); }
				for (var i = 0; i < 8; i++) { FilterH(_u, cy * _cStride + cx + i, _cStride, mbLimit, interior, hevThresh, true); FilterH(_v, cy * _cStride + cx + i, _cStride, mbLimit, interior, hevThresh, true); }
			}

			if (inner)
			{
				for (var b = 4; b < 16; b += 4)
				{
					for (var i = 0; i < 16; i++) { FilterH(_y, (yy + b) * _yStride + yx + i, _yStride, innerLimit, interior, hevThresh, false); }
				}

				for (var i = 0; i < 8; i++) { FilterH(_u, (cy + 4) * _cStride + cx + i, _cStride, innerLimit, interior, hevThresh, false); FilterH(_v, (cy + 4) * _cStride + cx + i, _cStride, innerLimit, interior, hevThresh, false); }
			}
		}

		private static void FilterH(byte[] p, int i, int step, int edgeLimit, int interior, int hevThresh, bool mbEdge)
		{
			var p3 = p[i - 4 * step]; var p2 = p[i - 3 * step]; var p1 = p[i - 2 * step]; var p0 = p[i - step];
			var q0 = p[i]; var q1 = p[i + step]; var q2 = p[i + 2 * step]; var q3 = p[i + 3 * step];

			if (!(Math.Abs(p0 - q0) * 2 + (Math.Abs(p1 - q1) >> 1) <= edgeLimit
				&& Math.Abs(p3 - p2) <= interior && Math.Abs(p2 - p1) <= interior && Math.Abs(p1 - p0) <= interior
				&& Math.Abs(q3 - q2) <= interior && Math.Abs(q2 - q1) <= interior && Math.Abs(q1 - q0) <= interior))
			{
				return;
			}

			var hev = Math.Abs(p1 - p0) > hevThresh || Math.Abs(q1 - q0) > hevThresh;

			if (!mbEdge)
			{
				if (hev)
				{
					CommonAdjust(p, i, step, true);
				}
				else
				{
					var a = CommonAdjust(p, i, step, false);
					var a3 = (a + 1) >> 1;
					p[i + step] = (byte)(Sclip(Sclip(p[i + step]) + a3) + 128);
					p[i - 2 * step] = (byte)(Sclip(Sclip(p[i - 2 * step]) - a3) + 128);
				}

				return;
			}

			if (hev)
			{
				CommonAdjust(p, i, step, true);
				return;
			}

			// Mb edge, low variance: 6-tap.
			var sp1 = Sclip(p1); var sp0 = Sclip(p0); var sq0 = Sclip(q0); var sq1 = Sclip(q1);
			var w = Clamp127(Clamp127(sp1 - sq1) + 3 * (sq0 - sp0));
			var a1 = (27 * w + 63) >> 7;
			var a2 = (18 * w + 63) >> 7;
			var a3b = (9 * w + 63) >> 7;
			p[i] = (byte)(Sclip(sq0 - a1) + 128);
			p[i - step] = (byte)(Sclip(sp0 + a1) + 128);
			p[i + step] = (byte)(Sclip(sq1 - a2) + 128);
			p[i - 2 * step] = (byte)(Sclip(sp1 + a2) + 128);
			p[i + 2 * step] = (byte)(Sclip(Sclip(q2) - a3b) + 128);
			p[i - 3 * step] = (byte)(Sclip(Sclip(p2) + a3b) + 128);
		}

		// ---- output ----

		private byte[] ToBgra()
		{
			var bgra = new byte[Width * Height * 4];
			for (var y = 0; y < Height; y++)
			{
				var cy = y >> 1;
				for (var x = 0; x < Width; x++)
				{
					var yv = _y[y * _yStride + x];
					var cx = x >> 1;
					var uv = _u[cy * _cStride + cx] - 128;
					var vv = _v[cy * _cStride + cx] - 128;
					var r = Clip8((int)(yv + 1.402 * vv + 0.5));
					var g = Clip8((int)(yv - 0.344136 * uv - 0.714136 * vv + 0.5));
					var b = Clip8((int)(yv + 1.772 * uv + 0.5));
					SetPixelPremul(bgra, (y * Width + x) * 4, r, g, b, 255);
				}
			}

			return bgra;
		}

		// Mode constants.
		private const int DC_PRED = 0, V_PRED = 1, H_PRED = 2, TM_PRED = 3, B_PRED = 4;
		private const int B_DC_PRED = 0, B_TM_PRED = 1, B_VE_PRED = 2, B_HE_PRED = 3, B_LD_PRED = 4, B_RD_PRED = 5, B_VR_PRED = 6, B_VL_PRED = 7, B_HD_PRED = 8, B_HU_PRED = 9;

		private static readonly sbyte[] _segmentTree = { 2, 4, -0, -1, -2, -3 };
		private readonly int[] _bModes = new int[16];
		private readonly byte[] _bModeProbsScratch = new byte[9];
		private int[] _aboveBModes = Array.Empty<int>();
		private readonly int[] _leftBModes = new int[4];
	}

	/// <summary>VP8 boolean (arithmetic) entropy decoder (RFC 6386 §7). Invariant value &lt; range&lt;&lt;8 keeps value 16-bit.</summary>
	internal sealed class Vp8BoolDecoder
	{
		private readonly byte[] _d;
		private int _pos;
		private readonly int _end;
		private uint _value;
		private uint _range;
		private int _bitCount;

		public Vp8BoolDecoder(byte[] d, int start, int length)
		{
			_d = d;
			_pos = start;
			_end = start + length;
			_value = ((uint)NextByte() << 8) | NextByte();
			_range = 255;
			_bitCount = 0;
		}

		private uint NextByte() => _pos < _end && _pos < _d.Length ? _d[_pos++] : 0u;

		public int GetBit(int probability)
		{
			var split = 1u + (((_range - 1) * (uint)probability) >> 8);
			var bigSplit = split << 8;
			int bit;
			if (_value >= bigSplit)
			{
				bit = 1;
				_range -= split;
				_value -= bigSplit;
			}
			else
			{
				bit = 0;
				_range = split;
			}

			while (_range < 128)
			{
				_value <<= 1;
				_range <<= 1;
				if (++_bitCount == 8)
				{
					_bitCount = 0;
					_value |= NextByte();
				}
			}

			return bit;
		}

		public int GetFlag() => GetBit(128);

		public int GetValue(int bits)
		{
			var v = 0;
			for (var i = 0; i < bits; i++)
			{
				v = (v << 1) | GetBit(128);
			}

			return v;
		}

		public int GetSigned(int bits)
		{
			var v = GetValue(bits);
			return GetFlag() == 1 ? -v : v;
		}
	}

	private static readonly int[] Vp8Zigzag =
	{
		0, 1, 4, 8, 5, 2, 3, 6, 9, 12, 13, 10, 7, 11, 14, 15,
	};
}
