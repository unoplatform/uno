#nullable enable

using System;
using System.Globalization;
using System.Numerics;

namespace Uno.UI.Composition.Drawing;

/// <summary>Parses an SVG path <c>d</c> string into <see cref="IPathBuilder"/> calls (arcs converted to cubics).</summary>
internal sealed class SvgPathParser
{
	private readonly string _d;
	private readonly IPathBuilder _builder;
	private int _pos;

	private float _cx, _cy;   // current point
	private float _sx, _sy;   // subpath start
	private float _lastCx, _lastCy; // last cubic control (for S)
	private float _lastQx, _lastQy; // last quad control (for T)
	private char _lastCommand;
	private bool _open;

	public SvgPathParser(string d, IPathBuilder builder)
	{
		_d = d;
		_builder = builder;
	}

	public void Parse()
	{
		while (true)
		{
			SkipSep();
			if (_pos >= _d.Length)
			{
				break;
			}

			var c = _d[_pos];
			char command;
			if (char.IsLetter(c))
			{
				command = c;
				_pos++;
			}
			else if (_lastCommand != '\0')
			{
				// Implicit repeat of the previous command; after an M/m, subsequent coords are L/l.
				command = _lastCommand switch { 'M' => 'L', 'm' => 'l', _ => _lastCommand };
			}
			else
			{
				break;
			}

			Execute(command);
			_lastCommand = command;
		}

		if (_open)
		{
			// leave the final subpath open (fill still closes it implicitly)
		}
	}

	private void Execute(char command)
	{
		var rel = char.IsLower(command);
		switch (char.ToUpperInvariant(command))
		{
			case 'M':
			{
				var x = Num() + (rel ? _cx : 0);
				var y = Num() + (rel ? _cy : 0);
				MoveTo(x, y);
				break;
			}
			case 'L':
			{
				var x = Num() + (rel ? _cx : 0);
				var y = Num() + (rel ? _cy : 0);
				LineTo(x, y);
				break;
			}
			case 'H':
				LineTo(Num() + (rel ? _cx : 0), _cy);
				break;
			case 'V':
				LineTo(_cx, Num() + (rel ? _cy : 0));
				break;
			case 'C':
			{
				var c1 = new Vector2(Num() + (rel ? _cx : 0), Num() + (rel ? _cy : 0));
				var c2 = new Vector2(Num() + (rel ? _cx : 0), Num() + (rel ? _cy : 0));
				var e = new Vector2(Num() + (rel ? _cx : 0), Num() + (rel ? _cy : 0));
				CubicTo(c1, c2, e);
				break;
			}
			case 'S':
			{
				var c1 = char.ToUpperInvariant(_lastCommand) is 'C' or 'S'
					? new Vector2(2 * _cx - _lastCx, 2 * _cy - _lastCy)
					: new Vector2(_cx, _cy);
				var c2 = new Vector2(Num() + (rel ? _cx : 0), Num() + (rel ? _cy : 0));
				var e = new Vector2(Num() + (rel ? _cx : 0), Num() + (rel ? _cy : 0));
				CubicTo(c1, c2, e);
				break;
			}
			case 'Q':
			{
				var c1 = new Vector2(Num() + (rel ? _cx : 0), Num() + (rel ? _cy : 0));
				var e = new Vector2(Num() + (rel ? _cx : 0), Num() + (rel ? _cy : 0));
				QuadTo(c1, e);
				break;
			}
			case 'T':
			{
				var c1 = char.ToUpperInvariant(_lastCommand) is 'Q' or 'T'
					? new Vector2(2 * _cx - _lastQx, 2 * _cy - _lastQy)
					: new Vector2(_cx, _cy);
				var e = new Vector2(Num() + (rel ? _cx : 0), Num() + (rel ? _cy : 0));
				QuadTo(c1, e);
				break;
			}
			case 'A':
			{
				var rx = Num();
				var ry = Num();
				var rot = Num();
				var large = Flag();
				var sweep = Flag();
				var x = Num() + (rel ? _cx : 0);
				var y = Num() + (rel ? _cy : 0);
				ArcTo(rx, ry, rot, large, sweep, x, y);
				break;
			}
			case 'Z':
				if (_open)
				{
					_builder.Close();
					_open = false;
				}
				_cx = _sx;
				_cy = _sy;
				break;
		}
	}

	private void MoveTo(float x, float y)
	{
		if (_open)
		{
			_builder.Close();
		}

		_builder.MoveTo(new Vector2(x, y));
		_open = true;
		_cx = _sx = x;
		_cy = _sy = y;
	}

	private void LineTo(float x, float y)
	{
		EnsureOpen();
		_builder.LineTo(new Vector2(x, y));
		_cx = x;
		_cy = y;
	}

	private void CubicTo(Vector2 c1, Vector2 c2, Vector2 e)
	{
		EnsureOpen();
		_builder.CubicTo(c1, c2, e);
		_lastCx = c2.X;
		_lastCy = c2.Y;
		_cx = e.X;
		_cy = e.Y;
	}

	private void QuadTo(Vector2 c1, Vector2 e)
	{
		EnsureOpen();
		_builder.QuadraticTo(c1, e);
		_lastQx = c1.X;
		_lastQy = c1.Y;
		_cx = e.X;
		_cy = e.Y;
	}

	private void ArcTo(float rx, float ry, float rotDeg, int large, int sweep, float x2, float y2)
	{
		EnsureOpen();
		var x1 = _cx;
		var y1 = _cy;
		if (rx == 0 || ry == 0)
		{
			LineTo(x2, y2);
			return;
		}

		rx = Math.Abs(rx);
		ry = Math.Abs(ry);
		var phi = rotDeg * MathF.PI / 180f;
		var cos = MathF.Cos(phi);
		var sin = MathF.Sin(phi);

		var dx = (x1 - x2) / 2f;
		var dy = (y1 - y2) / 2f;
		var x1p = cos * dx + sin * dy;
		var y1p = -sin * dx + cos * dy;

		var lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry);
		if (lambda > 1)
		{
			var s = MathF.Sqrt(lambda);
			rx *= s;
			ry *= s;
		}

		var sign = large != sweep ? 1f : -1f;
		var num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
		var den = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
		var coef = sign * MathF.Sqrt(MathF.Max(0f, num / den));
		var cxp = coef * (rx * y1p) / ry;
		var cyp = coef * -(ry * x1p) / rx;

		var cx = cos * cxp - sin * cyp + (x1 + x2) / 2f;
		var cy = sin * cxp + cos * cyp + (y1 + y2) / 2f;

		var theta1 = Angle(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
		var dtheta = Angle((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
		if (sweep == 0 && dtheta > 0)
		{
			dtheta -= 2 * MathF.PI;
		}
		else if (sweep == 1 && dtheta < 0)
		{
			dtheta += 2 * MathF.PI;
		}

		var segments = (int)MathF.Ceiling(MathF.Abs(dtheta) / (MathF.PI / 2f));
		var delta = dtheta / segments;
		var t = 4f / 3f * MathF.Tan(delta / 4f);

		var theta = theta1;
		for (var i = 0; i < segments; i++)
		{
			var cosA = MathF.Cos(theta);
			var sinA = MathF.Sin(theta);
			var cosB = MathF.Cos(theta + delta);
			var sinB = MathF.Sin(theta + delta);

			var p1 = OnArc(cx, cy, rx, ry, cos, sin, cosA, sinA);
			var p2 = OnArc(cx, cy, rx, ry, cos, sin, cosB, sinB);
			var c1 = new Vector2(
				p1.X - t * (cos * rx * sinA + sin * ry * cosA),
				p1.Y - t * (sin * rx * sinA - cos * ry * cosA));
			var c2 = new Vector2(
				p2.X + t * (cos * rx * sinB + sin * ry * cosB),
				p2.Y + t * (sin * rx * sinB - cos * ry * cosB));

			_builder.CubicTo(c1, c2, p2);
			theta += delta;
		}

		_cx = x2;
		_cy = y2;
	}

	private static Vector2 OnArc(float cx, float cy, float rx, float ry, float cosPhi, float sinPhi, float cosT, float sinT)
	{
		var x = rx * cosT;
		var y = ry * sinT;
		return new Vector2(cx + cosPhi * x - sinPhi * y, cy + sinPhi * x + cosPhi * y);
	}

	private static float Angle(float ux, float uy, float vx, float vy)
	{
		var dot = ux * vx + uy * vy;
		var len = MathF.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
		var a = MathF.Acos(Math.Clamp(dot / len, -1f, 1f));
		return (ux * vy - uy * vx) < 0 ? -a : a;
	}

	private void EnsureOpen()
	{
		if (!_open)
		{
			_builder.MoveTo(new Vector2(_cx, _cy));
			_open = true;
			_sx = _cx;
			_sy = _cy;
		}
	}

	private void SkipSep()
	{
		while (_pos < _d.Length && (_d[_pos] is ' ' or ',' or '\t' or '\n' or '\r'))
		{
			_pos++;
		}
	}

	private int Flag()
	{
		SkipSep();
		if (_pos < _d.Length)
		{
			var c = _d[_pos++];
			return c == '1' ? 1 : 0;
		}

		return 0;
	}

	private float Num()
	{
		SkipSep();
		var start = _pos;
		if (_pos < _d.Length && (_d[_pos] is '-' or '+'))
		{
			_pos++;
		}

		var dot = false;
		while (_pos < _d.Length)
		{
			var c = _d[_pos];
			if (char.IsDigit(c))
			{
				_pos++;
			}
			else if (c == '.' && !dot)
			{
				dot = true;
				_pos++;
			}
			else if ((c == 'e' || c == 'E') && _pos > start)
			{
				_pos++;
				if (_pos < _d.Length && (_d[_pos] is '-' or '+'))
				{
					_pos++;
				}
			}
			else
			{
				break;
			}
		}

		return float.TryParse(_d.AsSpan(start, _pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0f;
	}
}
