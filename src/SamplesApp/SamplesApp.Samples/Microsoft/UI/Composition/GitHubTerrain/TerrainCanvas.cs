#nullable enable

using System;
using System.Diagnostics;
using System.Numerics;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;

namespace UITests.Shared.Windows_UI_Composition.GitHubTerrain;

/// <summary>
/// Nine years of contributions as a landscape. Each cell is a box drawn as three faces - top, and
/// the two sides that face the camera - sorted back to front, since DrawVertices has no depth test.
/// Tapping springs the camera between a flat heatmap and the isometric view.
/// </summary>
internal sealed partial class TerrainCanvas : SKCanvasElement
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
	/// <summary>Barely open when raised, so the nine cards read as one island; the gap only really
	/// appears as the terrain flattens into separate years.</summary>
	private const float YearGapIso = 0.28f;
	/// <summary>Just over 1 so neighbours overlap: meeting exactly, every shared edge is
	/// antialiased against the background and the seams draw a lattice across the terrain.</summary>
	private const float FootprintIso = 1.04f;
	private const float FootprintFlat = 0.86f;

	/// <summary>Settles 99% in 350ms, the reference's figure.</summary>
	private static readonly float SpringOmega = -MathF.Log(0.01f) / 0.35f;

	private static readonly SKColor[] Ramp =
	{
		new(0xEB, 0xED, 0xF0),
		new(0x9B, 0xE9, 0xA8),
		new(0x40, 0xC4, 0x63),
		new(0x30, 0xA1, 0x4E),
		new(0x21, 0x6E, 0x39),
	};

	private int[,] _grid;
	private float[,] _heights;
	private readonly int[] _order;
	private readonly float[] _depth;

	private readonly SKPoint[] _positions;
	private readonly SKColor[] _colors;

	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private double _lastTime;

	private float _raise;
	private float _raiseVelocity;

	internal TerrainCanvas()
	{
		_grid = TerrainData.BuildMountainGrid();
		_heights = TerrainData.BuildHeights(_grid);

		var cells = TerrainData.Cols * TerrainData.Rows;
		_order = new int[cells];
		_depth = new float[cells];

		// Three faces, two triangles each, three vertices per triangle.
		_positions = new SKPoint[cells * 18];
		_colors = new SKColor[cells * 18];
	}

	/// <summary>Switches the source between a real-looking sparse year and the smooth field.</summary>
	internal void UseSparseData(bool sparse)
	{
		_grid = sparse ? TerrainData.BuildSparseGrid() : TerrainData.BuildMountainGrid();
		_heights = TerrainData.BuildHeights(_grid);
	}

	/// <summary>0 is the flat heatmap, 1 the raised landscape.</summary>
	internal float RaiseTarget { get; set; }

	internal bool IsRaised => RaiseTarget > 0.5f;

	internal void Toggle() => RaiseTarget = IsRaised ? 0f : 1f;

	protected override void RenderOverride(SKCanvas canvas, Size area)
	{
		var now = _clock.Elapsed.TotalSeconds;
		var dt = (float)Math.Min(0.05, _lastTime == 0 ? 0 : now - _lastTime);
		_lastTime = now;

		StepSpring(dt);

		canvas.Clear(new SKColor(0xF9, 0xF6, 0xF1));

		var w = (float)area.Width;
		var h = (float)area.Height;
		if (w <= 0 || h <= 0)
		{
			return;
		}

		var t = _raise;
		var angleY = FlatAngleY + (IsoAngleY - FlatAngleY) * t;
		var angleX = FlatAngleX + (IsoAngleX - FlatAngleX) * t;
		var footprint = FootprintFlat + (FootprintIso - FootprintFlat) * t;

		var cosY = MathF.Cos(angleY);
		var sinY = MathF.Sin(angleY);
		var cosX = MathF.Cos(angleX);
		var sinX = MathF.Sin(angleX);

		var cx = (TerrainData.Cols - 1) * 0.5f;
		var cz = (TerrainData.Rows + (TerrainData.Years - 1) * (YearGapFlat + (YearGapIso - YearGapFlat) * _raise) - 1) * 0.5f;

		// Fit by projecting the corners of the whole volume rather than every block.
		// Raised, the camera sits well back: at full width each cell is big enough that every seam
		// and step reads as an artefact, where the reference's cells are a few pixels and merge.
		var fill = 0.82f - 0.22f * t;
		var scale = FitScale(w, h, cosY, sinY, cosX, sinX, cx, cz, fill);

		var ox = w * 0.5f;
		var oy = h * 0.5f;

		var verts0 = 0;
		DrawYearCards(ref verts0, cosY, sinY, cosX, sinX, scale, ox, oy, cx, cz);
		var cardVerts = verts0;

		var count = 0;
		for (var c = 0; c < TerrainData.Cols; c++)
		{
			for (var r = 0; r < TerrainData.Rows; r++)
			{
				var i = c * TerrainData.Rows + r;
				_order[count] = i;

				// Depth of the cell centre after rotation; back to front once sorted.
				var x = c - cx;
				var z = RowDepth(r) - cz;
				var zr = -x * sinY + z * cosY;
				_depth[count] = zr * cosX;
				count++;
			}
		}

		Array.Sort(_depth, _order, 0, count);

		var verts = cardVerts;
		for (var k = count - 1; k >= 0; k--)
		{
			var i = _order[k];
			var c = i / TerrainData.Rows;
			var r = i % TerrainData.Rows;

			var level = _grid[c, r];
			var height = MinBlockHeight + _heights[c, r] * MaxBlockHeight * t;
			if (t < 0.5f)
			{
				height = FlatBlockHeight + (height - FlatBlockHeight) * t;
			}

			var half = footprint * 0.5f;
			var x0 = c - cx - half;
			var x1 = c - cx + half;
			var rz = RowDepth(r);
			var z0 = rz - cz - half;
			var z1 = rz - cz + half;

			// Top face corners, then the two side faces that face the camera.
			var t00 = Project(x0, height, z0, cosY, sinY, cosX, sinX, scale, ox, oy);
			var t10 = Project(x1, height, z0, cosY, sinY, cosX, sinX, scale, ox, oy);
			var t11 = Project(x1, height, z1, cosY, sinY, cosX, sinX, scale, ox, oy);
			var t01 = Project(x0, height, z1, cosY, sinY, cosX, sinX, scale, ox, oy);
			// Walls only where the surface actually steps. Dropping every cell's sides to the base
			// drew two thin slivers per cell - a lattice of them across the whole terrain.
			var rightDrop = height - HeightAt(c + 1, r, t);
			var frontDrop = height - HeightAt(c, r + 1, t, r);
			var b10 = Project(x1, height - rightDrop, z0, cosY, sinY, cosX, sinX, scale, ox, oy);
			var b11r = Project(x1, height - rightDrop, z1, cosY, sinY, cosX, sinX, scale, ox, oy);
			var b11f = Project(x1, height - frontDrop, z1, cosY, sinY, cosX, sinX, scale, ox, oy);
			var b01 = Project(x0, height - frontDrop, z1, cosY, sinY, cosX, sinX, scale, ox, oy);

			var top = SampleRamp(_heights[c, r], level);
			Quad(ref verts, t00, t10, t11, t01, top);

			if (rightDrop > 0.004f)
			{
				Quad(ref verts, t10, b10, b11r, t11, Shade(top, 0.9f));
			}

			if (frontDrop > 0.004f)
			{
				Quad(ref verts, t01, t11, b11f, b01, Shade(top, 0.78f));
			}
		}

		if (verts == 0)
		{
			return;
		}

		var pos = new SKPoint[verts];
		var col = new SKColor[verts];
		Array.Copy(_positions, pos, verts);
		Array.Copy(_colors, col, verts);

		using var paint = new SKPaint { IsAntialias = true };
		using var vertices = SKVertices.CreateCopy(SKVertexMode.Triangles, pos, col);
		canvas.DrawVertices(vertices, SKBlendMode.Dst, paint);
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

		var h = MinBlockHeight + _heights[c, r] * MaxBlockHeight * t;
		return t < 0.5f ? FlatBlockHeight + (h - FlatBlockHeight) * t : h;
	}

	/// <summary>Row depth with the year gaps folded in, so each year reads as its own card.</summary>
	private float RowDepth(int row)
	{
		var gap = YearGapFlat + (YearGapIso - YearGapFlat) * _raise;
		var year = row / TerrainData.RowsPerYear;
		var day = row % TerrainData.RowsPerYear;
		return year * (TerrainData.RowsPerYear + gap) + day;
	}

	private void DrawYearCards(
		ref int verts,
		float cosY, float sinY, float cosX, float sinX,
		float scale, float ox, float oy, float cx, float cz)
	{
		var card = new SKColor(0xFF, 0xFF, 0xFF);
		var x0 = -cx - 1.2f;
		var x1 = TerrainData.Cols - 1 - cx + 1.2f;

		for (var y = 0; y < TerrainData.Years; y++)
		{
			var front = RowDepth(y * TerrainData.RowsPerYear) - cz - 0.9f;
			var back = RowDepth(y * TerrainData.RowsPerYear + TerrainData.RowsPerYear - 1) - cz + 0.9f;

			var a = Project(x0, -0.02f, front, cosY, sinY, cosX, sinX, scale, ox, oy);
			var b = Project(x1, -0.02f, front, cosY, sinY, cosX, sinX, scale, ox, oy);
			var c2 = Project(x1, -0.02f, back, cosY, sinY, cosX, sinX, scale, ox, oy);
			var d = Project(x0, -0.02f, back, cosY, sinY, cosX, sinX, scale, ox, oy);
			Quad(ref verts, a, b, c2, d, card);
		}
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

	/// <summary>
	/// Skips a face with no area on screen. Seen from straight above a box's sides collapse to a
	/// line, and those degenerate triangles still rasterise as hairlines - which is what drew a grid
	/// over the flat heatmap.
	/// </summary>
	private void Quad(ref int verts, SKPoint a, SKPoint b, SKPoint c, SKPoint d, SKColor color)
	{
		var area = MathF.Abs((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X))
			+ MathF.Abs((c.X - a.X) * (d.Y - a.Y) - (c.Y - a.Y) * (d.X - a.X));
		if (area < 1f)
		{
			return;
		}

		_positions[verts] = a; _colors[verts++] = color;
		_positions[verts] = b; _colors[verts++] = color;
		_positions[verts] = c; _colors[verts++] = color;
		_positions[verts] = a; _colors[verts++] = color;
		_positions[verts] = c; _colors[verts++] = color;
		_positions[verts] = d; _colors[verts++] = color;
	}

	private static SKPoint Project(
		float x, float y, float z,
		float cosY, float sinY, float cosX, float sinX,
		float scale, float ox, float oy)
	{
		var xr = x * cosY + z * sinY;
		var zr = -x * sinY + z * cosY;
		var yr = y * cosX - zr * sinX;
		return new SKPoint(ox + xr * scale, oy - yr * scale);
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
			z *= 1f;
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
	private static SKColor SampleRamp(float height, int level)
	{
		if (level <= 0)
		{
			return Ramp[0];
		}

		var t = Math.Clamp(MathF.Pow(height, 1f / 0.6f) * 4f, 1f, 4f);
		var i = (int)MathF.Floor(t);
		var f = t - i;
		if (i >= 4)
		{
			return Ramp[4];
		}

		var a = Ramp[i];
		var b = Ramp[i + 1];
		return new SKColor(
			(byte)(a.Red + (b.Red - a.Red) * f),
			(byte)(a.Green + (b.Green - a.Green) * f),
			(byte)(a.Blue + (b.Blue - a.Blue) * f));
	}

	private static SKColor Shade(SKColor c, float f) => new(
		(byte)(c.Red * f),
		(byte)(c.Green * f),
		(byte)(c.Blue * f));
}
