#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using UITests.Shared.Windows_UI_Composition.SculpturesInWords;

namespace UITests.Shared.Windows_UI_Composition.SculpturesInWordsWinUI;

/// <summary>
/// The sculpture of the Skia sample with a Composition sprite per letter. The Skia version samples
/// one glyph atlas and modulates a vertex colour per letter; no public brush tints an image, so the
/// atlas is baked once per ink step instead and a letter just picks the brush that already carries
/// its glyph and its grey. The sprites are a list of draw slots, written back to front each frame.
/// </summary>
internal sealed class GlyphSprites
{
	private const float Focal = 3.2f;

	/// <summary>Must match the atlas: 61 glyphs in an 8-wide grid of 32px cells, stacked once per
	/// ink step. See glyph-atlas.py beside this file.</summary>
	private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.,;:'\"!?-";
	private const float Cell = 32f;
	private const int Columns = 8;
	private const int InkSteps = 24;
	private const float TileHeight = 256f;

	private readonly CloudPoint[] _cloud;
	private readonly CompositionSpriteShape[] _slots;
	private readonly CompositionSurfaceBrush[] _brushes = new CompositionSurfaceBrush[InkSteps * 64];
	private readonly int[] _glyph;
	private readonly float[] _roll;
	private readonly int[] _slotBrush;

	private readonly int[] _order;
	private readonly float[] _depth;
	private readonly Vector3[] _rotated;

	private readonly ShapeVisual _root;
	private readonly float _maxAbsY;
	private readonly float _maxRadiusXz;

	private int _used;

	internal GlyphSprites(UIElement host, CloudPoint[] cloud, string letters)
	{
		_cloud = cloud;
		_glyph = new int[cloud.Length];
		_roll = new float[cloud.Length];
		_order = new int[cloud.Length];
		_depth = new float[cloud.Length];
		_rotated = new Vector3[cloud.Length];
		_slots = new CompositionSpriteShape[cloud.Length];
		_slotBrush = new int[cloud.Length];

		var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
		_root = compositor.CreateShapeVisual();

		// One cell-sized rectangle shared by every letter. The surface brush paints the whole atlas,
		// and the geometry is what bounds it to a single glyph.
		var cellGeometry = compositor.CreateRectangleGeometry();
		cellGeometry.Size = new Vector2(Cell, Cell);

		var surface = LoadedImageSurface.StartLoadFromUri(
			new Uri("ms-appx:///Assets/Sculptures/glyph-atlas.png"));

		for (var tint = 0; tint < InkSteps; tint++)
		{
			for (var g = 0; g < 64; g++)
			{
				var brush = compositor.CreateSurfaceBrush(surface);
				brush.Stretch = CompositionStretch.None;
				brush.HorizontalAlignmentRatio = 0;
				brush.VerticalAlignmentRatio = 0;

				// Slide the atlas so this glyph's cell lands on the sprite, which clips to one cell.
				brush.TransformMatrix = Matrix3x2.CreateTranslation(
					-(g % Columns) * Cell,
					-((g / Columns) * Cell + tint * TileHeight));
				_brushes[tint * 64 + g] = brush;
			}
		}

		var rng = new Random(7);
		for (var i = 0; i < cloud.Length; i++)
		{
			var c = letters.Length == 0 ? '.' : letters[i % letters.Length];
			var slot = Alphabet.IndexOf(c);
			_glyph[i] = slot >= 0 ? slot : Alphabet.IndexOf('.');
			_roll[i] = (float)(rng.NextDouble() - 0.5) * 0.5f;

			var shape = compositor.CreateSpriteShape(cellGeometry);
			shape.FillBrush = _brushes[(InkSteps - 1) * 64];
			_slotBrush[i] = (InkSteps - 1) * 64;
			_slots[i] = shape;
			_root.Shapes.Add(shape);

			_maxAbsY = MathF.Max(_maxAbsY, MathF.Abs(cloud[i].Position.Y));
			_maxRadiusXz = MathF.Max(
				_maxRadiusXz,
				MathF.Sqrt(cloud[i].Position.X * cloud[i].Position.X + cloud[i].Position.Z * cloud[i].Position.Z));
		}

		ElementCompositionPreview.SetElementChildVisual(host, _root);
	}

	/// <summary>How much a letter overruns its share of the silhouette. Above 1 they overlap.</summary>
	internal float GlyphDensity { get; set; } = 2.2f;

	internal int LetterCount => _cloud.Length;

	/// <summary>Turntable angle, driven from outside.</summary>
	internal float Angle { get; set; }

	internal void Update(float w, float h)
	{
		if (w <= 0 || h <= 0)
		{
			return;
		}

		_root.Size = new Vector2(w, h);

		var cos = MathF.Cos(Angle);
		var sin = MathF.Sin(Angle);

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
		// silhouette are half the cloud.
		var visible = MathF.Max(1, _cloud.Length * 0.5f);
		var baseGlyph = MathF.Sqrt(silhouette / visible) * GlyphDensity;

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

		// Painter's algorithm: child order is the draw order, so far letters take the low slots.
		Array.Sort(_depth, _order);
		Array.Reverse(_order);

		var previous = _used;
		_used = 0;

		for (var k = 0; k < _order.Length; k++)
		{
			var i = _order[k];
			var centre = _rotated[i];
			var denom = Focal + centre.Z;
			if (denom <= 0.1f)
			{
				continue;
			}

			var n = _cloud[i].Normal;
			var nx = n.X * cos + n.Z * sin;
			var nz = -n.X * sin + n.Z * cos;

			// Sorting alone cannot hide the far side: a depth buffer would, but thousands of separate
			// marks leave gaps, and the back of the figure is lit from behind so it reads darker than
			// the front it shows through. Fade a mark out as its surface turns away.
			var facing = Math.Clamp((-nz + 0.10f) / 0.45f, 0f, 1f);
			if (facing <= 0.02f)
			{
				continue;
			}

			var persp = Focal / denom;
			var sx = cx + centre.X * scale * persp;
			var sy = cy - centre.Y * scale * persp;
			var size = baseGlyph * persp;

			// Raw cosine, not the half-lambert wrap: with the far side culled every mark that remains
			// faces the viewer, and wrapping compressed them all into the pale end of the range.
			var lambert = Math.Clamp(Vector3.Dot(new Vector3(nx, n.Y, nz), light), 0f, 1f);

			// Ink on paper, so light has to lighten: a lit face writes in pale grey and recedes,
			// a shadowed one writes in near-black. Occlusion deepens the creases on top of that.
			var shade = (0.18f + 0.82f * lambert) * (0.28f + 0.72f * _cloud[i].Ao);
			// Letters further away lose contrast, separating the far side of the turn from the near.
			var haze = Math.Clamp((centre.Z + 1f) * 0.5f, 0f, 1f) * 0.35f;
			var lit = 22 + shade * 200 + haze * 45;

			// Toward the paper rather than toward transparency: these are overlapping opaque marks,
			// so fading the colour keeps the silhouette clean where alpha would stack up and bruise.
			var ink = Math.Clamp(245 + (lit - 245) * facing, 22f, 245f);
			var tint = Math.Clamp((int)MathF.Round((ink - 22f) / (245f - 22f) * (InkSteps - 1)), 0, InkSteps - 1);

			var shape = _slots[_used];

			// One matrix per letter: scale and roll about the cell centre, then place it on its point.
			var s = size / Cell;
			var roll = _roll[i];
			var m11 = s * MathF.Cos(roll);
			var m12 = s * MathF.Sin(roll);
			var half = Cell * 0.5f;
			shape.TransformMatrix = new Matrix3x2(
				m11, m12,
				-m12, m11,
				sx - (half * m11 - half * m12), sy - (half * m12 + half * m11));

			var brush = tint * 64 + _glyph[i];
			if (_slotBrush[_used] != brush)
			{
				_slotBrush[_used] = brush;
				shape.FillBrush = _brushes[brush];
			}

			_used++;
		}

		// Collapse whatever the previous frame left behind.
		for (var j = _used; j < previous; j++)
		{
			_slots[j].TransformMatrix = default;
		}
	}
}
