#nullable enable

using System;
using System.Diagnostics;
using System.Numerics;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;

namespace UITests.Shared.Windows_UI_Composition.SculpturesInWords;

/// <summary>
/// Draws the figure: one textured quad per letter, turned on a turntable and sorted back to front.
/// Quads are built corner by corner in 3D rather than as screen-space sprites, so a letter can tip
/// edge-on - which is what reads as depth rather than as sliding.
/// </summary>
internal sealed partial class SculptureCanvas : SKCanvasElement
{
	private const int BatchQuads = 8192;
	private const float Focal = 3.2f;

	private readonly CloudPoint[] _cloud;
	private readonly GlyphAtlas _atlas;
	private readonly int[] _slot;
	private readonly float[] _roll;

	private readonly int[] _order;
	private readonly float[] _depth;
	private readonly Vector3[] _rotated;

	private readonly SKPoint[] _positions = new SKPoint[BatchQuads * 6];
	private readonly SKPoint[] _texs = new SKPoint[BatchQuads * 6];
	private readonly SKColor[] _colors = new SKColor[BatchQuads * 6];

	private readonly float _maxAbsY;
	private readonly float _maxRadiusXz;

	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private double _lastFrameMs;
	private double _emaFrameMs;
	private long _lastTicks;

	internal SculptureCanvas(CloudPoint[] cloud, GlyphAtlas atlas, string letters)
	{
		_cloud = cloud;
		_atlas = atlas;

		_slot = new int[cloud.Length];
		_roll = new float[cloud.Length];
		var rng = new Random(7);
		for (var i = 0; i < cloud.Length; i++)
		{
			var c = letters.Length == 0 ? '.' : letters[i % letters.Length];
			var slot = atlas.SlotFor(c);
			_slot[i] = slot >= 0 ? slot : atlas.SlotFor('.');
			_roll[i] = (float)(rng.NextDouble() - 0.5) * 0.5f;
		}

		_order = new int[cloud.Length];
		_depth = new float[cloud.Length];
		_rotated = new Vector3[cloud.Length];

		foreach (var p in cloud)
		{
			_maxAbsY = MathF.Max(_maxAbsY, MathF.Abs(p.Position.Y));
			_maxRadiusXz = MathF.Max(_maxRadiusXz, MathF.Sqrt(p.Position.X * p.Position.X + p.Position.Z * p.Position.Z));
		}
	}

	/// <summary>How much a letter overruns its share of the silhouette. Above 1 they overlap.</summary>
	internal float GlyphDensity { get; set; } = 2.2f;

	internal double FrameMilliseconds => _emaFrameMs;

	internal int LetterCount => _cloud.Length;

	/// <summary>Turntable angle, driven from outside so two views can share one rotation.</summary>
	internal float Angle { get; set; }

	/// <summary>Off draws the same cloud as plain shaded marks - the figure without the words.</summary>
	internal bool UseGlyphs { get; set; } = true;

	protected override void RenderOverride(SKCanvas canvas, Size area)
	{
		var now = _clock.ElapsedTicks;
		if (_lastTicks != 0)
		{
			_lastFrameMs = (now - _lastTicks) * 1000.0 / Stopwatch.Frequency;
			_emaFrameMs = _emaFrameMs == 0 ? _lastFrameMs : _emaFrameMs * 0.9 + _lastFrameMs * 0.1;
		}
		_lastTicks = now;

		canvas.Clear(new SKColor(0xF5, 0xF2, 0xEB));

		var w = (float)area.Width;
		var h = (float)area.Height;
		if (w <= 0 || h <= 0)
		{
			return;
		}

		var angle = Angle;
		var cos = MathF.Cos(angle);
		var sin = MathF.Sin(angle);

		// A turntable sweeps every point out to its xz radius, so that - not the static x extent -
		// is what has to fit. Nearest-point perspective is the worst case for both axes.
		var perspMax = Focal / MathF.Max(0.2f, Focal - _maxRadiusXz);
		var scale = MathF.Min(
			w * 0.46f / MathF.Max(1e-3f, _maxRadiusXz * perspMax),
			h * 0.46f / MathF.Max(1e-3f, _maxAbsY * perspMax));

		// Spread the letters over the silhouette rather than sizing them absolutely: halve the count
		// and each letter grows to cover the gap, so the figure stays readable at any density.
		var silhouette = 2f * _maxRadiusXz * scale * 2f * _maxAbsY * scale * 0.55f;
		// Only the near half of a shell is ever on screen, so the letters that have to cover the
		// silhouette are half the cloud. Sizing against the full count made every letter a speck.
		var visible = MathF.Max(1, _cloud.Length * 0.5f);
		// Marks have no glyph shape to read, so they can be smaller and still cover: at letter size
		// they smear into a blob.
		var baseGlyph = MathF.Sqrt(silhouette / visible) * (UseGlyphs ? GlyphDensity : 1.15f);
		var cx = w * 0.5f;
		var cy = h * 0.5f;

		var light = Vector3.Normalize(new Vector3(-0.4f, 0.6f, -0.7f));

		for (var i = 0; i < _cloud.Length; i++)
		{
			var p = _cloud[i].Position;
			var rx = p.X * cos + p.Z * sin;
			var rz = -p.X * sin + p.Z * cos;
			_rotated[i] = new Vector3(rx, p.Y, rz);
			_depth[i] = rz;
			_order[i] = i;
		}

		// Painter's algorithm: no depth buffer behind DrawVertices, so far letters go down first.
		Array.Sort(_depth, _order);
		Array.Reverse(_order);

		using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White };
		using var shader = UseGlyphs
			? SKShader.CreateImage(_atlas.Image, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp)
			: null;
		paint.Shader = shader;

		var written = 0;

		for (var k = 0; k < _order.Length; k++)
		{
			var i = _order[k];
			var centre = _rotated[i];
			var denom = Focal + centre.Z;
			if (denom <= 0.1f)
			{
				continue;
			}

			var persp = Focal / denom;
			var sx = cx + centre.X * scale * persp;
			var sy = cy - centre.Y * scale * persp;
			var size = baseGlyph * persp;

			var n = _cloud[i].Normal;
			var nx = n.X * cos + n.Z * sin;
			var nz = -n.X * sin + n.Z * cos;

			// Sorting alone cannot hide the far side: a depth buffer would, but thousands of separate
			// marks leave gaps, and the back of the figure is lit from behind so it reads darker than
			// the front it shows through. Fade a mark out as its surface turns away, which is what the
			// depth test would have done for us.
			var facing = Math.Clamp((-nz + 0.10f) / 0.45f, 0f, 1f);
			if (facing <= 0.02f)
			{
				continue;
			}
			// Raw cosine, not the half-lambert wrap: with the far side culled every mark that remains
			// faces the viewer, and wrapping compressed them all into the pale end of the range.
			var lambert = Math.Clamp(Vector3.Dot(new Vector3(nx, n.Y, nz), light), 0f, 1f);

			// Ink on paper, so light has to lighten: a lit face writes in pale grey and recedes,
			// a shadowed one writes in near-black. Occlusion deepens the creases on top of that.
			// Ink on paper, so light has to lighten: a lit face writes in pale grey and recedes,
			// a shadowed one writes in near-black. Occlusion deepens the creases on top of that.
			var shade = (0.18f + 0.82f * lambert) * (0.28f + 0.72f * _cloud[i].Ao);

			// Letters further away lose contrast, separating the far side of the turn from the near.
			var haze = Math.Clamp((centre.Z + 1f) * 0.5f, 0f, 1f) * 0.35f;
			var lit = 22 + shade * 200 + haze * 45;

			// Toward the paper rather than toward transparency: these are overlapping opaque marks,
			// so fading the colour keeps the silhouette clean where alpha would stack up and bruise.
			var ink = (byte)Math.Clamp(245 + (lit - 245) * facing, 0, 255);
			var color = new SKColor(ink, ink, (byte)Math.Min(255, ink + 10));

			var roll = _roll[i];
			var cr = MathF.Cos(roll) * size * 0.5f;
			var sr = MathF.Sin(roll) * size * 0.5f;

			var p0 = new SKPoint(sx - cr + sr, sy - sr - cr);
			var p1 = new SKPoint(sx + cr + sr, sy + sr - cr);
			var p2 = new SKPoint(sx + cr - sr, sy + sr + cr);
			var p3 = new SKPoint(sx - cr - sr, sy - sr + cr);

			var b = _atlas.Bounds[_slot[i]];
			var t0 = new SKPoint(b.Left, b.Top);
			var t1 = new SKPoint(b.Right, b.Top);
			var t2 = new SKPoint(b.Right, b.Bottom);
			var t3 = new SKPoint(b.Left, b.Bottom);

			var o = written * 6;
			_positions[o + 0] = p0; _texs[o + 0] = t0; _colors[o + 0] = color;
			_positions[o + 1] = p1; _texs[o + 1] = t1; _colors[o + 1] = color;
			_positions[o + 2] = p2; _texs[o + 2] = t2; _colors[o + 2] = color;
			_positions[o + 3] = p0; _texs[o + 3] = t0; _colors[o + 3] = color;
			_positions[o + 4] = p2; _texs[o + 4] = t2; _colors[o + 4] = color;
			_positions[o + 5] = p3; _texs[o + 5] = t3; _colors[o + 5] = color;
			written++;

			if (written == BatchQuads)
			{
				Flush(canvas, paint, written);
				written = 0;
			}
		}

		if (written > 0)
		{
			Flush(canvas, paint, written);
		}
	}

	private void Flush(SKCanvas canvas, SKPaint paint, int quads)
	{
		var count = quads * 6;
		var pos = new SKPoint[count];
		var tex = new SKPoint[count];
		var col = new SKColor[count];
		Array.Copy(_positions, pos, count);
		Array.Copy(_texs, tex, count);
		Array.Copy(_colors, col, count);

		using var vertices = SKVertices.CreateCopy(SKVertexMode.Triangles, pos, tex, col);
		canvas.DrawVertices(vertices, SKBlendMode.Modulate, paint);
	}
}
