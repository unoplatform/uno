#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;

namespace UITests.Shared.Windows_UI_Composition.SculpturesInWords;

/// <summary>
/// The scan as it was scanned: a solid shaded surface, for comparison with the same figure written
/// out in letters. Triangles are lit from their own normals, back faces dropped, and the rest sorted
/// far to near - there is no depth buffer behind DrawVertices.
/// </summary>
internal sealed partial class MeshCanvas : SKCanvasElement
{
	private const float Focal = 3.2f;

	private readonly Vector3[] _position;
	private readonly Vector3[] _normal;
	private readonly int[] _index;

	private readonly SKPoint[] _screen;
	private readonly SKColor[] _colors;
	private readonly int[] _order;
	private readonly float[] _depth;
	private readonly ushort[] _drawIndex;

	private readonly float _maxAbsY;
	private readonly float _maxRadiusXz;

	internal MeshCanvas(MeshData mesh)
	{
		_position = mesh.Positions;
		_normal = mesh.Normals;
		_index = mesh.Indices;

		_screen = new SKPoint[_position.Length];
		_colors = new SKColor[_position.Length];
		_order = new int[_index.Length / 3];
		_depth = new float[_index.Length / 3];
		_drawIndex = new ushort[_index.Length];

		foreach (var p in _position)
		{
			_maxAbsY = MathF.Max(_maxAbsY, MathF.Abs(p.Y));
			_maxRadiusXz = MathF.Max(_maxRadiusXz, MathF.Sqrt(p.X * p.X + p.Z * p.Z));
		}
	}

	internal float Angle { get; set; }

	protected override void RenderOverride(SKCanvas canvas, Size area)
	{
		canvas.Clear(new SKColor(0xF5, 0xF2, 0xEB));

		var w = (float)area.Width;
		var h = (float)area.Height;
		if (w <= 0 || h <= 0 || _position.Length == 0)
		{
			return;
		}

		var cos = MathF.Cos(Angle);
		var sin = MathF.Sin(Angle);

		var perspMax = Focal / MathF.Max(0.2f, Focal - _maxRadiusXz);
		var scale = MathF.Min(
			w * 0.46f / MathF.Max(1e-3f, _maxRadiusXz * perspMax),
			h * 0.46f / MathF.Max(1e-3f, _maxAbsY * perspMax));

		var cx = w * 0.5f;
		var cy = h * 0.5f;
		var light = Vector3.Normalize(new Vector3(-0.4f, 0.6f, -0.7f));

		for (var i = 0; i < _position.Length; i++)
		{
			var p = _position[i];
			var rx = p.X * cos + p.Z * sin;
			var rz = -p.X * sin + p.Z * cos;
			var persp = Focal / MathF.Max(0.2f, Focal + rz);
			_screen[i] = new SKPoint(cx + rx * scale * persp, cy - p.Y * scale * persp);

			var n = _normal[i];
			var nx = n.X * cos + n.Z * sin;
			var nz = -n.X * sin + n.Z * cos;
			var lambert = Math.Clamp(Vector3.Dot(new Vector3(nx, n.Y, nz), light) * 0.5f + 0.5f, 0f, 1f);

			// Plaster, not paper: mid grey into near white, with a cool cast in the shadows so the
			// form reads without the letters doing the work.
			var v = 0.18f + 0.78f * lambert * lambert;
			var r = (byte)Math.Clamp(v * 238, 0, 255);
			var g = (byte)Math.Clamp(v * 234, 0, 255);
			var b = (byte)Math.Clamp(v * 226 + 12, 0, 255);
			_colors[i] = new SKColor(r, g, b);
		}

		var live = 0;
		for (var t = 0; t < _order.Length; t++)
		{
			var a = _index[t * 3];
			var b = _index[t * 3 + 1];
			var c = _index[t * 3 + 2];

			// Screen-space winding: a back face has the opposite sign once projected, and dropping it
			// halves the overdraw and stops interior surfaces showing through the silhouette.
			var pa = _screen[a];
			var pb = _screen[b];
			var pc = _screen[c];
			if ((pb.X - pa.X) * (pc.Y - pa.Y) - (pb.Y - pa.Y) * (pc.X - pa.X) <= 0f)
			{
				continue;
			}

			_order[live] = t;
			_depth[live] = RotatedZ(a, cos, sin) + RotatedZ(b, cos, sin) + RotatedZ(c, cos, sin);
			live++;
		}

		Array.Sort(_depth, _order, 0, live);

		var n2 = 0;
		for (var k = live - 1; k >= 0; k--)
		{
			var t = _order[k];
			_drawIndex[n2++] = (ushort)_index[t * 3];
			_drawIndex[n2++] = (ushort)_index[t * 3 + 1];
			_drawIndex[n2++] = (ushort)_index[t * 3 + 2];
		}

		if (n2 == 0)
		{
			return;
		}

		var indices = new ushort[n2];
		Array.Copy(_drawIndex, indices, n2);

		using var paint = new SKPaint { IsAntialias = false };
		using var vertices = SKVertices.CreateCopy(SKVertexMode.Triangles, _screen, null, _colors, indices);
		canvas.DrawVertices(vertices, SKBlendMode.Dst, paint);
	}

	private float RotatedZ(int i, float cos, float sin)
	{
		var p = _position[i];
		return -p.X * sin + p.Z * cos;
	}
}
