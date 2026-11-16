#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using UITests.Shared.Windows_UI_Composition.GitHubTerrain;
using Windows.UI;

namespace UITests.Shared.Windows_UI_Composition.GitHubTerrainWinUI;

/// <summary>
/// The terrain of the Skia sample rebuilt out of Composition shapes. The projection is affine, so
/// every face of every box is a parallelogram - which is a unit rectangle under a Matrix3x2. Each
/// face is one sprite shape, and the collection is a list of draw slots that the depth-sorted faces
/// are written into, so painter's order costs no reordering.
/// </summary>
internal sealed class TerrainMesh
{
	private const float IsoAngleY = 0.8f;
	/// <summary>Toward -pi/2 is straight down, so a larger magnitude lifts the camera.</summary>
	private const float IsoAngleX = -0.66f;
	private const float FlatAngleY = 0f;
	private const float FlatAngleX = -MathF.PI / 2f;

	private const float MaxBlockHeight = 5.6f;
	private const float MinBlockHeight = 0.026f;
	private const float FlatBlockHeight = 0.06f;

	/// <summary>Years sit on separate cards with a gap between them, as in the reference.</summary>
	private const float YearGapFlat = 2.6f;
	/// <summary>Barely open when raised, so the nine cards read as one island.</summary>
	private const float YearGapIso = 0.28f;
	/// <summary>Just over 1 so neighbours overlap: meeting exactly, every shared edge is
	/// antialiased against the background and the seams draw a lattice across the terrain.</summary>
	private const float FootprintIso = 1.04f;
	private const float FootprintFlat = 0.86f;

	/// <summary>Settles 99% in 350ms, the reference's figure.</summary>
	private static readonly float SpringOmega = -MathF.Log(0.01f) / 0.35f;

	/// <summary>Finer across the ramp than the eye can separate, standing in for the Skia version's
	/// continuous blend; a brush per face would mean a brush per frame.</summary>
	private const int RampSteps = 64;

	private static readonly Color[] Ramp =
	{
		Color.FromArgb(255, 0xEB, 0xED, 0xF0),
		Color.FromArgb(255, 0x9B, 0xE9, 0xA8),
		Color.FromArgb(255, 0x40, 0xC4, 0x63),
		Color.FromArgb(255, 0x30, 0xA1, 0x4E),
		Color.FromArgb(255, 0x21, 0x6E, 0x39),
	};

	/// <summary>Face shades: the top, the side that steps right, and the side that steps forward.</summary>
	private static readonly float[] Shades = { 1f, 0.9f, 0.78f };

	private readonly Compositor _compositor;
	private readonly ShapeVisual _root;
	private readonly CompositionRectangleGeometry _unit;
	private readonly CompositionColorBrush[][] _palette = new CompositionColorBrush[Shades.Length][];
	private readonly CompositionColorBrush _card;

	private readonly List<CompositionSpriteShape> _slots = new();
	private readonly List<int> _slotBrush = new();

	private readonly int[] _order;
	private readonly float[] _depth;

	private int[,] _grid;
	private float[,] _heights;

	private float _raise;
	private float _raiseVelocity;
	private float _builtRaise = float.NaN;
	private float _builtWidth;
	private float _builtHeight;
	private bool _dataChanged = true;
	private int _used;

	internal TerrainMesh(UIElement host)
	{
		_compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
		_root = _compositor.CreateShapeVisual();
		_unit = _compositor.CreateRectangleGeometry();
		_unit.Size = Vector2.One;

		_card = _compositor.CreateColorBrush(Colors.White);
		for (var s = 0; s < Shades.Length; s++)
		{
			_palette[s] = new CompositionColorBrush[RampSteps + 1];
			_palette[s][0] = _compositor.CreateColorBrush(Shade(Ramp[0], Shades[s]));
			for (var i = 0; i < RampSteps; i++)
			{
				var t = 1f + 3f * (i / (float)(RampSteps - 1));
				_palette[s][i + 1] = _compositor.CreateColorBrush(Shade(SampleRamp(t), Shades[s]));
			}
		}

		_grid = TerrainData.BuildMountainGrid();
		_heights = TerrainData.BuildHeights(_grid);

		var cells = TerrainData.Cols * TerrainData.Rows;
		_order = new int[cells];
		_depth = new float[cells];

		ElementCompositionPreview.SetElementChildVisual(host, _root);
	}

	/// <summary>0 is the flat heatmap, 1 the raised landscape.</summary>
	internal float RaiseTarget { get; set; } = 1f;

	internal bool IsRaised => RaiseTarget > 0.5f;

	internal int FaceCount => _used;

	/// <summary>Switches the source between a real-looking sparse year and the smooth field.</summary>
	internal void UseSparseData(bool sparse)
	{
		_grid = sparse ? TerrainData.BuildSparseGrid() : TerrainData.BuildMountainGrid();
		_heights = TerrainData.BuildHeights(_grid);
		_dataChanged = true;
	}

	internal void Update(float w, float h, float dt)
	{
		StepSpring(dt);

		if (w <= 0 || h <= 0)
		{
			return;
		}

		// Retained shapes, so there is nothing to do on a frame where neither the camera nor the
		// data moved - which is every frame outside the 350ms spring.
		if (!_dataChanged && _raise == _builtRaise && w == _builtWidth && h == _builtHeight)
		{
			return;
		}

		_dataChanged = false;
		_builtRaise = _raise;
		_builtWidth = w;
		_builtHeight = h;

		Rebuild(w, h);
	}

	private void Rebuild(float w, float h)
	{
		_root.Size = new Vector2(w, h);

		var t = _raise;
		var angleY = FlatAngleY + (IsoAngleY - FlatAngleY) * t;
		var angleX = FlatAngleX + (IsoAngleX - FlatAngleX) * t;
		var footprint = FootprintFlat + (FootprintIso - FootprintFlat) * t;

		var cosY = MathF.Cos(angleY);
		var sinY = MathF.Sin(angleY);
		var cosX = MathF.Cos(angleX);
		var sinX = MathF.Sin(angleX);

		var cx = (TerrainData.Cols - 1) * 0.5f;
		var cz = (TerrainData.Rows + (TerrainData.Years - 1) * (YearGapFlat + (YearGapIso - YearGapFlat) * t) - 1) * 0.5f;

		// Raised, the camera sits well back: at full width each cell is big enough that every seam
		// and step reads as an artefact, where the reference's cells are a few pixels and merge.
		var fill = 0.82f - 0.22f * t;
		var scale = FitScale(w, h, cosY, sinY, cosX, sinX, cx, cz, fill);
		var ox = w * 0.5f;
		var oy = h * 0.5f;

		var previous = _used;
		_used = 0;

		DrawYearCards(cosY, sinY, cosX, sinX, scale, ox, oy, cx, cz, t);

		var count = 0;
		for (var c = 0; c < TerrainData.Cols; c++)
		{
			for (var r = 0; r < TerrainData.Rows; r++)
			{
				_order[count] = c * TerrainData.Rows + r;

				// Depth of the cell centre after rotation; back to front once sorted.
				var x = c - cx;
				var z = RowDepth(r, t) - cz;
				_depth[count] = (-x * sinY + z * cosY) * cosX;
				count++;
			}
		}

		Array.Sort(_depth, _order, 0, count);

		for (var k = count - 1; k >= 0; k--)
		{
			var i = _order[k];
			var c = i / TerrainData.Rows;
			var r = i % TerrainData.Rows;

			var level = _grid[c, r];
			var height = BlockHeight(_heights[c, r], t);

			var half = footprint * 0.5f;
			var x0 = c - cx - half;
			var x1 = c - cx + half;
			var rz = RowDepth(r, t);
			var z0 = rz - cz - half;
			var z1 = rz - cz + half;

			var t00 = Project(x0, height, z0, cosY, sinY, cosX, sinX, scale, ox, oy);
			var t10 = Project(x1, height, z0, cosY, sinY, cosX, sinX, scale, ox, oy);
			var t11 = Project(x1, height, z1, cosY, sinY, cosX, sinX, scale, ox, oy);
			var t01 = Project(x0, height, z1, cosY, sinY, cosX, sinX, scale, ox, oy);

			// Walls only where the surface actually steps. Dropping every cell's sides to the base
			// drew two thin slivers per cell - a lattice of them across the whole terrain.
			var rightDrop = height - HeightAt(c + 1, r, t);
			var frontDrop = height - HeightAt(c, r + 1, t, r);

			var tint = PaletteIndex(_heights[c, r], level);
			Face(t00, t10, t01, 0, tint);

			if (rightDrop > 0.004f)
			{
				var b10 = Project(x1, height - rightDrop, z0, cosY, sinY, cosX, sinX, scale, ox, oy);
				Face(t10, b10, t11, 1, tint);
			}

			if (frontDrop > 0.004f)
			{
				var b01 = Project(x0, height - frontDrop, z1, cosY, sinY, cosX, sinX, scale, ox, oy);
				Face(t01, t11, b01, 2, tint);
			}
		}

		// Collapse whatever the previous frame left behind; a zero matrix gives the shape no area.
		for (var i = _used; i < previous; i++)
		{
			_slots[i].TransformMatrix = default;
		}
	}

	private void DrawYearCards(
		float cosY, float sinY, float cosX, float sinX,
		float scale, float ox, float oy, float cx, float cz, float t)
	{
		var x0 = -cx - 1.2f;
		var x1 = TerrainData.Cols - 1 - cx + 1.2f;

		for (var y = 0; y < TerrainData.Years; y++)
		{
			var front = RowDepth(y * TerrainData.RowsPerYear, t) - cz - 0.9f;
			var back = RowDepth(y * TerrainData.RowsPerYear + TerrainData.RowsPerYear - 1, t) - cz + 0.9f;

			var a = Project(x0, -0.02f, front, cosY, sinY, cosX, sinX, scale, ox, oy);
			var b = Project(x1, -0.02f, front, cosY, sinY, cosX, sinX, scale, ox, oy);
			var d = Project(x0, -0.02f, back, cosY, sinY, cosX, sinX, scale, ox, oy);
			Face(a, b, d, -1, 0);
		}
	}

	/// <summary>
	/// Writes one parallelogram into the next draw slot, from the corner <paramref name="a"/> and its
	/// two edges. The fourth corner follows, which is what lets a unit rectangle under a Matrix3x2
	/// stand in for the face.
	/// </summary>
	private void Face(Vector2 a, Vector2 b, Vector2 d, int shade, int tint)
	{
		var u = b - a;
		var v = d - a;

		// Skips a face with no area on screen. Seen from straight above a box's sides collapse to a
		// line, and those degenerate quads still rasterise as hairlines.
		if (MathF.Abs(u.X * v.Y - u.Y * v.X) * 2f < 1f)
		{
			return;
		}

		var brush = shade < 0 ? _card : _palette[shade][tint];
		var key = shade < 0 ? -1 : shade * (RampSteps + 1) + tint;

		if (_used == _slots.Count)
		{
			var created = _compositor.CreateSpriteShape(_unit);
			created.FillBrush = brush;
			_root.Shapes.Add(created);
			_slots.Add(created);
			_slotBrush.Add(key);
		}

		var shape = _slots[_used];
		shape.TransformMatrix = new Matrix3x2(u.X, u.Y, v.X, v.Y, a.X, a.Y);
		if (_slotBrush[_used] != key)
		{
			_slotBrush[_used] = key;
			shape.FillBrush = brush;
		}

		_used++;
	}

	private static float BlockHeight(float normalized, float t)
	{
		var height = MinBlockHeight + normalized * MaxBlockHeight * t;
		return t < 0.5f ? FlatBlockHeight + (height - FlatBlockHeight) * t : height;
	}

	/// <summary>Height of a neighbour, or the base outside the grid so the rim keeps its wall.</summary>
	private float HeightAt(int c, int r, float t, int fromRow = -1)
	{
		if (c < 0 || c >= TerrainData.Cols || r < 0 || r >= TerrainData.Rows)
		{
			return 0f;
		}

		// A neighbour on the next year's card is across a gap, not adjacent: treating it as adjacent
		// drops the wall that covers the card edge and tears a hole through to the background.
		if (fromRow >= 0 && r / TerrainData.RowsPerYear != fromRow / TerrainData.RowsPerYear)
		{
			return 0f;
		}

		return BlockHeight(_heights[c, r], t);
	}

	/// <summary>Row depth with the year gaps folded in, so each year reads as its own card.</summary>
	private static float RowDepth(int row, float t)
	{
		var gap = YearGapFlat + (YearGapIso - YearGapFlat) * t;
		return (row / TerrainData.RowsPerYear) * (TerrainData.RowsPerYear + gap) + (row % TerrainData.RowsPerYear);
	}

	private void StepSpring(float dt)
	{
		if (dt <= 0f)
		{
			return;
		}

		// Closed-form critically damped step, so a long frame lands where it should instead of
		// overshooting the way a naive Euler step would.
		var displacement = _raise - RaiseTarget;
		var decay = MathF.Exp(-SpringOmega * dt);
		var newDisplacement = (displacement + (_raiseVelocity + SpringOmega * displacement) * dt) * decay;
		_raiseVelocity = (_raiseVelocity - SpringOmega * (_raiseVelocity + SpringOmega * displacement) * dt) * decay;
		_raise = RaiseTarget + newDisplacement;

		if (MathF.Abs(_raise - RaiseTarget) < 0.0005f && MathF.Abs(_raiseVelocity) < 0.01f)
		{
			_raise = RaiseTarget;
			_raiseVelocity = 0f;
		}
	}

	private static Vector2 Project(
		float x, float y, float z,
		float cosY, float sinY, float cosX, float sinX,
		float scale, float ox, float oy)
	{
		var xr = x * cosY + z * sinY;
		var zr = -x * sinY + z * cosY;
		var yr = y * cosX - zr * sinX;
		return new Vector2(ox + xr * scale, oy - yr * scale);
	}

	private static float FitScale(
		float w, float h,
		float cosY, float sinY, float cosX, float sinX,
		float cx, float cz, float fill)
	{
		var maxX = 0.001f;
		var maxY = 0.001f;
		for (var i = 0; i < 8; i++)
		{
			var x = ((i & 1) == 0 ? -cx : cx) - 0.5f;
			var z = ((i & 2) == 0 ? -cz : cz) - 0.5f;
			var y = (i & 4) == 0 ? 0f : MaxBlockHeight;

			var xr = x * cosY + z * sinY;
			var zr = -x * sinY + z * cosY;
			var yr = y * cosX - zr * sinX;
			maxX = MathF.Max(maxX, MathF.Abs(xr));
			maxY = MathF.Max(maxY, MathF.Abs(yr));
		}

		return MathF.Min(w * 0.5f * fill / maxX, h * 0.5f * fill / maxY);
	}

	/// <summary>
	/// Blends between the five GitHub stops using the smoothed height. Indexing the ramp by bucket
	/// alone gave five flat plateaus of colour, which is most of what made this look 8-bit.
	/// </summary>
	private static int PaletteIndex(float height, int level)
	{
		if (level <= 0)
		{
			return 0;
		}

		var t = Math.Clamp(MathF.Pow(height, 1f / 0.6f) * 4f, 1f, 4f);
		return 1 + (int)MathF.Round((t - 1f) / 3f * (RampSteps - 1));
	}

	private static Color SampleRamp(float t)
	{
		var i = (int)MathF.Floor(t);
		if (i >= 4)
		{
			return Ramp[4];
		}

		var f = t - i;
		var a = Ramp[i];
		var b = Ramp[i + 1];
		return Color.FromArgb(
			255,
			(byte)(a.R + (b.R - a.R) * f),
			(byte)(a.G + (b.G - a.G) * f),
			(byte)(a.B + (b.B - a.B) * f));
	}

	private static Color Shade(Color c, float f) =>
		Color.FromArgb(255, (byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f));
}
