#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Minimal CFF / Type2 charstring reader used by <see cref="ManagedFont"/> to extract glyph outlines from
/// OpenType-PostScript fonts. Handles the full Type2 operator set (moves/lines/curves/flex, global+local subrs,
/// hint operators, CID FDArray/FDSelect); it reads geometry only, so hinting and width values are skipped.
/// </summary>
internal sealed class CffTable
{
	private readonly byte[] _data;
	private readonly CffIndex _charStrings;
	private readonly CffIndex _globalSubrs;
	private readonly int _globalBias;

	// Non-CID: a single set of local subrs. CID: one set per font-dict, selected via FDSelect.
	private readonly CffIndex? _localSubrs;
	private readonly int _localBias;
	private readonly CffIndex[]? _fdLocalSubrs;
	private readonly int[]? _fdLocalBias;
	private readonly byte[]? _fdSelect; // glyph -> font-dict index

	private CffTable(byte[] data, CffIndex charStrings, CffIndex globalSubrs, CffIndex? localSubrs, CffIndex[]? fdLocalSubrs, int[]? fdLocalBias, byte[]? fdSelect)
	{
		_data = data;
		_charStrings = charStrings;
		_globalSubrs = globalSubrs;
		_globalBias = Bias(globalSubrs.Count);
		_localSubrs = localSubrs;
		_localBias = localSubrs is null ? 0 : Bias(localSubrs.Value.Count);
		_fdLocalSubrs = fdLocalSubrs;
		_fdLocalBias = fdLocalBias;
		_fdSelect = fdSelect;
	}

	public int GlyphCount => _charStrings.Count;

	public static CffTable? Parse(byte[] d, int cffBase)
	{
		var hdrSize = d[cffBase + 2];
		var p = cffBase + hdrSize;

		p = SkipIndex(d, p);              // Name INDEX
		var topDicts = ReadIndex(d, ref p); // Top DICT INDEX
		var stringIndexEnd = SkipIndex(d, p); // String INDEX
		var globalSubrsStart = stringIndexEnd;
		var gp = globalSubrsStart;
		var globalSubrs = ReadIndex(d, ref gp); // Global Subr INDEX

		if (topDicts.Count == 0)
		{
			return null;
		}

		var topDict = ParseDict(d, topDicts.Start(0), topDicts.End(0));
		if (!topDict.TryGetValue(17, out var charStringsOp)) // CharStrings offset
		{
			return null;
		}

		var cp = cffBase + (int)charStringsOp[0];
		var charStrings = ReadIndex(d, ref cp);

		if (topDict.ContainsKey(1236)) // FDArray -> CID-keyed font
		{
			return ParseCid(d, cffBase, topDict, charStrings, globalSubrs);
		}

		CffIndex? localSubrs = null;
		if (topDict.TryGetValue(18, out var priv) && priv.Length == 2) // Private DICT [size, offset]
		{
			var privStart = cffBase + (int)priv[1];
			var privDict = ParseDict(d, privStart, privStart + (int)priv[0]);
			if (privDict.TryGetValue(19, out var subrsOp)) // local Subrs offset (relative to Private DICT)
			{
				var sp = privStart + (int)subrsOp[0];
				localSubrs = ReadIndex(d, ref sp);
			}
		}

		return new CffTable(d, charStrings, globalSubrs, localSubrs, null, null, null);
	}

	private static CffTable? ParseCid(byte[] d, int cffBase, Dictionary<int, double[]> topDict, CffIndex charStrings, CffIndex globalSubrs)
	{
		var fdArrayStart = cffBase + (int)topDict[1236][0];
		var fdArray = ReadIndex(d, ref fdArrayStart);

		var fdLocalSubrs = new CffIndex[fdArray.Count];
		var fdLocalBias = new int[fdArray.Count];
		for (var i = 0; i < fdArray.Count; i++)
		{
			var fontDict = ParseDict(d, fdArray.Start(i), fdArray.End(i));
			var subrs = CffIndex.Empty;
			if (fontDict.TryGetValue(18, out var priv) && priv.Length == 2)
			{
				var privStart = cffBase + (int)priv[1];
				var privDict = ParseDict(d, privStart, privStart + (int)priv[0]);
				if (privDict.TryGetValue(19, out var subrsOp))
				{
					var sp = privStart + (int)subrsOp[0];
					subrs = ReadIndex(d, ref sp);
				}
			}

			fdLocalSubrs[i] = subrs;
			fdLocalBias[i] = Bias(subrs.Count);
		}

		var fdSelect = new byte[charStrings.Count];
		if (topDict.TryGetValue(1237, out var fdSelectOp)) // FDSelect
		{
			ParseFdSelect(d, cffBase + (int)fdSelectOp[0], fdSelect);
		}

		return new CffTable(d, charStrings, globalSubrs, null, fdLocalSubrs, fdLocalBias, fdSelect);
	}

	private static void ParseFdSelect(byte[] d, int p, byte[] fdSelect)
	{
		var format = d[p++];
		if (format == 0)
		{
			for (var i = 0; i < fdSelect.Length; i++)
			{
				fdSelect[i] = d[p + i];
			}
		}
		else if (format == 3)
		{
			var nRanges = ManagedFont.U16(d, p);
			p += 2;
			for (var r = 0; r < nRanges; r++)
			{
				var first = ManagedFont.U16(d, p);
				var fd = d[p + 2];
				var next = ManagedFont.U16(d, p + 3);
				for (var g = first; g < next && g < fdSelect.Length; g++)
				{
					fdSelect[g] = fd;
				}

				p += 3;
			}
		}
	}

	public void EmitGlyph(IPathBuilder builder, ushort glyph, float originX, float originY, float scale)
	{
		if (glyph >= _charStrings.Count)
		{
			return;
		}

		CffIndex localSubrs;
		int localBias;
		if (_fdLocalSubrs is not null && _fdSelect is not null)
		{
			var fd = _fdSelect[glyph];
			localSubrs = _fdLocalSubrs[fd];
			localBias = _fdLocalBias![fd];
		}
		else
		{
			localSubrs = _localSubrs ?? CffIndex.Empty;
			localBias = _localBias;
		}

		var interpreter = new Type2Interpreter(_data, builder, _globalSubrs, _globalBias, localSubrs, localBias, originX, originY, scale);
		interpreter.Run(_charStrings.Start(glyph), _charStrings.End(glyph));
		interpreter.Finish();
	}

	private static int Bias(int count) => count < 1240 ? 107 : count < 33900 ? 1131 : 32768;

	private static Dictionary<int, double[]> ParseDict(byte[] d, int start, int end)
	{
		var dict = new Dictionary<int, double[]>();
		var operands = new List<double>();
		var p = start;
		while (p < end)
		{
			var b0 = d[p];
			if (b0 <= 21)
			{
				p++;
				int key = b0;
				if (b0 == 12)
				{
					key = 1200 + d[p++];
				}

				dict[key] = operands.ToArray();
				operands.Clear();
			}
			else if (b0 == 28)
			{
				operands.Add((short)ManagedFont.U16(d, p + 1));
				p += 3;
			}
			else if (b0 == 29)
			{
				operands.Add((int)ManagedFont.U32(d, p + 1));
				p += 5;
			}
			else if (b0 == 30) // real (BCD)
			{
				p++;
				var done = false;
				while (p < end && !done)
				{
					var octet = d[p++];
					for (var shift = 4; shift >= 0; shift -= 4)
					{
						var nibble = (octet >> shift) & 0xF;
						if (nibble == 0xF)
						{
							done = true;
							break;
						}
					}
				}

				operands.Add(0); // real operands are never used for the offsets we read
			}
			else if (b0 >= 32 && b0 <= 246)
			{
				operands.Add(b0 - 139);
				p++;
			}
			else if (b0 >= 247 && b0 <= 250)
			{
				operands.Add((b0 - 247) * 256 + d[p + 1] + 108);
				p += 2;
			}
			else if (b0 >= 251 && b0 <= 254)
			{
				operands.Add(-(b0 - 251) * 256 - d[p + 1] - 108);
				p += 2;
			}
			else
			{
				p++;
			}
		}

		return dict;
	}

	private static CffIndex ReadIndex(byte[] d, ref int p)
	{
		var count = ManagedFont.U16(d, p);
		p += 2;
		if (count == 0)
		{
			return CffIndex.Empty;
		}

		var offSize = d[p++];
		var offsets = new int[count + 1];
		var dataStart = p + (count + 1) * offSize - 1;
		for (var i = 0; i <= count; i++)
		{
			var v = 0;
			for (var b = 0; b < offSize; b++)
			{
				v = (v << 8) | d[p++];
			}

			offsets[i] = dataStart + v;
		}

		p = offsets[count];
		return new CffIndex(d, offsets, count);
	}

	private static int SkipIndex(byte[] d, int p)
	{
		ReadIndex(d, ref p);
		return p;
	}

	/// <summary>A CFF INDEX: item <c>i</c> occupies <c>[Start(i), End(i))</c> in the backing data.</summary>
	private readonly struct CffIndex
	{
		private readonly int[] _offsets;

		public CffIndex(byte[] data, int[] offsets, int count)
		{
			_offsets = offsets;
			Count = count;
		}

		public static CffIndex Empty { get; } = new(Array.Empty<byte>(), new[] { 0 }, 0);

		public int Count { get; }

		public int Start(int i) => _offsets[i];
		public int End(int i) => _offsets[i + 1];
	}

	/// <summary>Executes a Type2 charstring, emitting the outline through <see cref="IPathBuilder"/>.</summary>
	private sealed class Type2Interpreter
	{
		private readonly byte[] _data;
		private readonly IPathBuilder _builder;
		private readonly CffIndex _globalSubrs;
		private readonly int _globalBias;
		private readonly CffIndex _localSubrs;
		private readonly int _localBias;
		private readonly float _ox;
		private readonly float _oy;
		private readonly float _scale;

		private readonly double[] _stack = new double[48];
		private int _sp;
		private double _x;
		private double _y;
		private int _nStems;
		private bool _widthParsed;
		private bool _open;
		private bool _stopped;

		public Type2Interpreter(byte[] data, IPathBuilder builder, CffIndex globalSubrs, int globalBias, CffIndex localSubrs, int localBias, float ox, float oy, float scale)
		{
			_data = data;
			_builder = builder;
			_globalSubrs = globalSubrs;
			_globalBias = globalBias;
			_localSubrs = localSubrs;
			_localBias = localBias;
			_ox = ox;
			_oy = oy;
			_scale = scale;
		}

		public void Finish()
		{
			if (_open)
			{
				_builder.Close();
				_open = false;
			}
		}

		public void Run(int start, int end)
		{
			var p = start;
			while (p < end && !_stopped)
			{
				var b0 = _data[p++];
				if (b0 >= 32 || b0 == 28)
				{
					double value;
					if (b0 == 28)
					{
						value = (short)ManagedFont.U16(_data, p);
						p += 2;
					}
					else if (b0 < 247)
					{
						value = b0 - 139;
					}
					else if (b0 < 251)
					{
						value = (b0 - 247) * 256 + _data[p++] + 108;
					}
					else if (b0 < 255)
					{
						value = -(b0 - 251) * 256 - _data[p++] - 108;
					}
					else // 255: 16.16 fixed
					{
						value = (int)ManagedFont.U32(_data, p) / 65536.0;
						p += 4;
					}

					Push(value);
					continue;
				}

				switch (b0)
				{
					case 1: // hstem
					case 3: // vstem
					case 18: // hstemhm
					case 23: // vstemhm
						CountStems();
						break;
					case 19: // hintmask
					case 20: // cntrmask
						CountStems();
						p += (_nStems + 7) / 8;
						break;
					case 21: // rmoveto
						PeelWidth(2);
						MoveTo(_x + _stack[_sp - 2], _y + _stack[_sp - 1]);
						_sp = 0;
						break;
					case 22: // hmoveto
						PeelWidth(1);
						MoveTo(_x + _stack[_sp - 1], _y);
						_sp = 0;
						break;
					case 4: // vmoveto
						PeelWidth(1);
						MoveTo(_x, _y + _stack[_sp - 1]);
						_sp = 0;
						break;
					case 5: // rlineto
						for (var i = 0; i + 1 < _sp; i += 2)
						{
							LineTo(_x + _stack[i], _y + _stack[i + 1]);
						}
						_sp = 0;
						break;
					case 6: // hlineto
						AlternatingLines(horizontalFirst: true);
						break;
					case 7: // vlineto
						AlternatingLines(horizontalFirst: false);
						break;
					case 8: // rrcurveto
						for (var i = 0; i + 5 < _sp; i += 6)
						{
							RelativeCurve(_stack[i], _stack[i + 1], _stack[i + 2], _stack[i + 3], _stack[i + 4], _stack[i + 5]);
						}
						_sp = 0;
						break;
					case 24: // rcurveline
						{
							var i = 0;
							for (; i + 5 < _sp - 2; i += 6)
							{
								RelativeCurve(_stack[i], _stack[i + 1], _stack[i + 2], _stack[i + 3], _stack[i + 4], _stack[i + 5]);
							}
							LineTo(_x + _stack[i], _y + _stack[i + 1]);
							_sp = 0;
							break;
						}
					case 25: // rlinecurve
						{
							var i = 0;
							for (; i + 1 < _sp - 6; i += 2)
							{
								LineTo(_x + _stack[i], _y + _stack[i + 1]);
							}
							RelativeCurve(_stack[i], _stack[i + 1], _stack[i + 2], _stack[i + 3], _stack[i + 4], _stack[i + 5]);
							_sp = 0;
							break;
						}
					case 26: // vvcurveto
						VvCurve();
						break;
					case 27: // hhcurveto
						HhCurve();
						break;
					case 30: // vhcurveto
						AlternatingCurves(horizontalFirst: false);
						break;
					case 31: // hvcurveto
						AlternatingCurves(horizontalFirst: true);
						break;
					case 10: // callsubr
						{
							var index = (int)_stack[--_sp] + _localBias;
							if (index >= 0 && index < _localSubrs.Count)
							{
								Run(_localSubrs.Start(index), _localSubrs.End(index));
							}
							break;
						}
					case 29: // callgsubr
						{
							var index = (int)_stack[--_sp] + _globalBias;
							if (index >= 0 && index < _globalSubrs.Count)
							{
								Run(_globalSubrs.Start(index), _globalSubrs.End(index));
							}
							break;
						}
					case 11: // return
						return;
					case 14: // endchar
						_stopped = true;
						return;
					case 12: // escape
						Escape(_data[p++]);
						break;
					default:
						_sp = 0;
						break;
				}
			}
		}

		private void Escape(byte b1)
		{
			switch (b1)
			{
				case 34: // hflex
					{
						var y0 = _y;
						RelativeCurve(_stack[0], 0, _stack[1], _stack[2], _stack[3], 0);
						RelativeCurve(_stack[4], 0, _stack[5], y0 - _y, _stack[6], 0);
						break;
					}
				case 36: // hflex1
					{
						var y0 = _y;
						RelativeCurve(_stack[0], _stack[1], _stack[2], _stack[3], _stack[4], 0);
						RelativeCurve(_stack[5], 0, _stack[6], _stack[7], _stack[8], y0 - _y);
						break;
					}
				case 35: // flex
					RelativeCurve(_stack[0], _stack[1], _stack[2], _stack[3], _stack[4], _stack[5]);
					RelativeCurve(_stack[6], _stack[7], _stack[8], _stack[9], _stack[10], _stack[11]);
					break;
				case 37: // flex1
					{
						var x0 = _x;
						var y0 = _y;
						var dx = _stack[0] + _stack[2] + _stack[4] + _stack[6] + _stack[8];
						var dy = _stack[1] + _stack[3] + _stack[5] + _stack[7] + _stack[9];
						RelativeCurve(_stack[0], _stack[1], _stack[2], _stack[3], _stack[4], _stack[5]);
						if (Math.Abs(dx) > Math.Abs(dy))
						{
							RelativeCurve(_stack[6], _stack[7], _stack[8], _stack[9], _stack[10], y0 - (_y + _stack[7] + _stack[9]));
						}
						else
						{
							RelativeCurve(_stack[6], _stack[7], _stack[8], _stack[9], x0 - (_x + _stack[6] + _stack[8]), _stack[10]);
						}
						break;
					}
			}

			_sp = 0;
		}

		private void CountStems()
		{
			if (!_widthParsed)
			{
				_widthParsed = true;
			}

			_nStems += _sp / 2;
			_sp = 0;
		}

		private void PeelWidth(int expectedArgs)
		{
			if (!_widthParsed)
			{
				_widthParsed = true;
				if (_sp > expectedArgs)
				{
					// Drop the leading width operand.
					for (var i = 1; i < _sp; i++)
					{
						_stack[i - 1] = _stack[i];
					}

					_sp--;
				}
			}
		}

		private void VvCurve()
		{
			var i = 0;
			var dx1 = 0.0;
			if ((_sp & 1) == 1)
			{
				dx1 = _stack[0];
				i = 1;
			}

			for (; i + 3 < _sp; i += 4)
			{
				RelativeCurve(dx1, _stack[i], _stack[i + 1], _stack[i + 2], 0, _stack[i + 3]);
				dx1 = 0;
			}

			_sp = 0;
		}

		private void HhCurve()
		{
			var i = 0;
			var dy1 = 0.0;
			if ((_sp & 1) == 1)
			{
				dy1 = _stack[0];
				i = 1;
			}

			for (; i + 3 < _sp; i += 4)
			{
				RelativeCurve(_stack[i], dy1, _stack[i + 1], _stack[i + 2], _stack[i + 3], 0);
				dy1 = 0;
			}

			_sp = 0;
		}

		private void AlternatingLines(bool horizontalFirst)
		{
			var horizontal = horizontalFirst;
			for (var i = 0; i < _sp; i++)
			{
				if (horizontal)
				{
					LineTo(_x + _stack[i], _y);
				}
				else
				{
					LineTo(_x, _y + _stack[i]);
				}

				horizontal = !horizontal;
			}

			_sp = 0;
		}

		private void AlternatingCurves(bool horizontalFirst)
		{
			var horizontal = horizontalFirst;
			var i = 0;
			while (i + 4 <= _sp)
			{
				var last = _sp - (i + 4) == 1;
				if (horizontal)
				{
					var c1x = _x + _stack[i];
					var c1y = _y;
					var c2x = c1x + _stack[i + 1];
					var c2y = c1y + _stack[i + 2];
					var ex = last ? c2x + _stack[i + 4] : c2x;
					var ey = c2y + _stack[i + 3];
					Curve(c1x, c1y, c2x, c2y, ex, ey);
				}
				else
				{
					var c1x = _x;
					var c1y = _y + _stack[i];
					var c2x = c1x + _stack[i + 1];
					var c2y = c1y + _stack[i + 2];
					var ex = c2x + _stack[i + 3];
					var ey = last ? c2y + _stack[i + 4] : c2y;
					Curve(c1x, c1y, c2x, c2y, ex, ey);
				}

				i += 4;
				horizontal = !horizontal;
			}

			_sp = 0;
		}

		private void RelativeCurve(double dxa, double dya, double dxb, double dyb, double dxc, double dyc)
		{
			var c1x = _x + dxa;
			var c1y = _y + dya;
			var c2x = c1x + dxb;
			var c2y = c1y + dyb;
			var ex = c2x + dxc;
			var ey = c2y + dyc;
			Curve(c1x, c1y, c2x, c2y, ex, ey);
		}

		private void Curve(double c1x, double c1y, double c2x, double c2y, double ex, double ey)
		{
			_builder.CubicTo(Screen(c1x, c1y), Screen(c2x, c2y), Screen(ex, ey));
			_x = ex;
			_y = ey;
		}

		private void MoveTo(double nx, double ny)
		{
			if (_open)
			{
				_builder.Close();
			}

			_x = nx;
			_y = ny;
			_builder.MoveTo(Screen(_x, _y));
			_open = true;
		}

		private void LineTo(double nx, double ny)
		{
			_x = nx;
			_y = ny;
			_builder.LineTo(Screen(_x, _y));
		}

		private Vector2 Screen(double x, double y) => new((float)(_ox + x * _scale), (float)(_oy - y * _scale));

		private void Push(double value)
		{
			if (_sp < _stack.Length)
			{
				_stack[_sp++] = value;
			}
		}
	}
}
