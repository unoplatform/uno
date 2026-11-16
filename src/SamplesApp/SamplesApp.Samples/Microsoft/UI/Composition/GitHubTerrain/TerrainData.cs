#nullable enable

using System;

namespace UITests.Shared.Windows_UI_Composition.GitHubTerrain;

/// <summary>
/// The contribution grid behind the terrain: 53 weeks across, nine years of weekdays down. Built
/// procedurally rather than from anybody's real history - a ridge, a summit cluster and a foreground
/// plateau, blurred and then quantised into the same five buckets GitHub exposes.
/// </summary>
internal static class TerrainData
{
	internal const int Cols = 53;
	internal const int Years = 9;
	internal const int RowsPerYear = 7;
	internal const int Rows = Years * RowsPerYear;

	/// <summary>GitHub publishes buckets, not counts; these are the per-bucket day estimates the
	/// reference uses so bar height tracks amount rather than level.</summary>
	private static readonly float[] EstimateByLevel = { 0f, 1f, 4f, 11f, 28f };

	/// <summary>
	/// Sparse activity, the way a real year of contributions looks: most days empty, work arriving
	/// in clusters. A smooth field sets how busy each week of each year is, then each day draws
	/// against it - which gives streaks and quiet months instead of uniform noise.
	/// </summary>
	internal static int[,] BuildSparseGrid(int seed = 20261006)
	{
		var rng = new Random(seed);
		var grid = new int[Cols, Rows];

		for (var y = 0; y < Years; y++)
		{
			// Some years are simply busier than others.
			var yearEnergy = 0.35f + 0.55f * Hash01(y * 7.13f + 0.5f);

			for (var c = 0; c < Cols; c++)
			{
				var w = c / (float)(Cols - 1);
				var busy = 0.5f + 0.5f * MathF.Sin(w * MathF.PI * 2.1f + y * 1.7f);
				busy = busy * 0.6f + 0.4f * Hash01(c * 0.37f + y * 11.9f);
				var weekEnergy = Math.Clamp(yearEnergy * (0.35f + 0.9f * busy), 0f, 1f);

				for (var d = 0; d < RowsPerYear; d++)
				{
					var r = y * RowsPerYear + d;

					// Weekends are quieter, as they are on a real profile.
					var dayFactor = d is 5 or 6 ? 0.45f : 1f;
					var p = weekEnergy * dayFactor * 0.62f;

					if (rng.NextDouble() > p)
					{
						grid[c, r] = 0;
						continue;
					}

					var t = (float)rng.NextDouble();
					grid[c, r] = t switch
					{
						< 0.42f => 1,
						< 0.72f => 2,
						< 0.91f => 3,
						_ => 4,
					};
				}
			}
		}

		return grid;
	}

	/// <summary>
	/// The reference's other data source: a ridge, a summit cluster and a foreground plateau, blurred
	/// and then quantised into the same five buckets. Dense and smooth, where real contributions are
	/// sparse and scattered.
	/// </summary>
	internal static int[,] BuildMountainGrid()
	{
		var raw = new float[Cols, Rows];
		for (var c = 0; c < Cols; c++)
		{
			for (var r = 0; r < Rows; r++)
			{
				var nx = c / (float)Math.Max(Cols - 1, 1);
				var ny = r / (float)Math.Max(Rows - 1, 1);

				var ridgeLine = ny - (0.17f + 0.52f * nx);
				var ridge = MathF.Exp(-(ridgeLine * ridgeLine) / 0.028f) * 1.05f;

				var along = nx * 0.72f + ny * 0.68f;
				var undulate =
					0.10f * MathF.Sin(along * MathF.PI * 4.2f + 0.35f) +
					0.05f * MathF.Sin(along * MathF.PI * 8.1f + 1.1f);

				var backMassif =
					MathF.Exp(-((Sq(nx - 0.84f) + Sq(ny - 0.14f)) / 0.026f)) * 0.95f +
					MathF.Exp(-((Sq(nx - 0.72f) + Sq(ny - 0.22f)) / 0.04f)) * 0.45f;

				var foreBase = 0.14f * (0.55f + 0.45f * Smoothstep(0.38f, 0.96f, ny));
				var foreDamp = 0.62f + 0.38f * (1f - Smoothstep(0.48f, 0.98f, ny));

				var dx = (nx - 0.5f) / 0.48f;
				var dz = (ny - 0.5f) / 0.46f;
				var edge = 1f - Smoothstep(0.62f, 1.08f, dx * dx + dz * dz);

				raw[c, r] = MathF.Max(0f, (ridge + undulate + backMassif + foreBase) * foreDamp * (0.78f + 0.22f * edge));
			}
		}

		var smoothed = BoxBlur(raw, 5);
		var min = float.MaxValue;
		var max = float.MinValue;
		foreach (var v in smoothed)
		{
			min = MathF.Min(min, v);
			max = MathF.Max(max, v);
		}

		var span = max - min;
		if (span <= 0f)
		{
			span = 1f;
		}

		var grid = new int[Cols, Rows];
		for (var c = 0; c < Cols; c++)
		{
			for (var r = 0; r < Rows; r++)
			{
				var t = (smoothed[c, r] - min) / span;
				grid[c, r] = t switch
				{
					< 0.20f => 0,
					< 0.36f => 1,
					< 0.54f => 2,
					< 0.72f => 3,
					_ => 4,
				};
			}
		}

		return grid;
	}

	private static float Sq(float v) => v * v;

	private static float Smoothstep(float a, float b, float x)
	{
		var t = Math.Clamp((x - a) / (b - a), 0f, 1f);
		return t * t * (3f - 2f * t);
	}

	private static float[,] BoxBlur(float[,] src, int passes)
	{
		var cur = (float[,])src.Clone();
		for (var p = 0; p < passes; p++)
		{
			var next = new float[Cols, Rows];
			for (var c = 0; c < Cols; c++)
			{
				for (var r = 0; r < Rows; r++)
				{
					var sum = 0f;
					var count = 0;
					for (var dc = -1; dc <= 1; dc++)
					{
						for (var dr = -1; dr <= 1; dr++)
						{
							var cc = c + dc;
							var rr = r + dr;
							if (cc >= 0 && cc < Cols && rr >= 0 && rr < Rows)
							{
								sum += cur[cc, rr];
								count++;
							}
						}
					}

					next[c, r] = sum / count;
				}
			}

			cur = next;
		}

		return cur;
	}

	private static float Hash01(float x)
	{
		var h = MathF.Sin(x * 127.1f) * 43758.5453f;
		return MathF.Abs(h - MathF.Floor(h));
	}

	internal static float[,] BuildHeights(int[,] grid)
	{
		var heights = new float[Cols, Rows];
		var max = 0f;
		for (var c = 0; c < Cols; c++)
		{
			for (var r = 0; r < Rows; r++)
			{
				var v = EstimateByLevel[Math.Clamp(grid[c, r], 0, 4)];
				heights[c, r] = v;
				max = MathF.Max(max, v);
			}
		}

		if (max <= 0f)
		{
			return heights;
		}

		for (var c = 0; c < Cols; c++)
		{
			for (var r = 0; r < Rows; r++)
			{
				// Raw estimates are 1, 4, 11, 28 - normalised that leaves the top bucket towering and
				// the other three flat against the floor, which reads as scattered towers rather than
				// as ground. The curve pulls the middle up so the levels step evenly.
				heights[c, r] = MathF.Pow(heights[c, r] / max, 0.6f);
			}
		}

		// Colour stays bucketed, height does not: five discrete heights terrace the surface into
		// plateaus, which is what makes it read as blocky. Blurring the height field lets the same
		// five colour bands sit on a rolling surface, the way the reference's foothills do.
		// One pass, not three: enough to break the five buckets into foothills, while the cells still
		// step visibly. Three passes sanded the terrain flat.
		return BoxBlur(heights, 1);
	}
}
