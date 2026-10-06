#nullable enable

using System;
using System.Numerics;

namespace UITests.Shared.Windows_UI_Composition.SculpturesInWords;

/// <summary>
/// One letter's place on the figure: where it sits, which way the surface faces there, and how
/// buried it is. Mirrors the 7-float record the reference demo bakes per point.
/// </summary>
internal readonly record struct CloudPoint(Vector3 Position, Vector3 Normal, float Ao);

internal static class PointCloud
{
	/// <summary>
	/// Reads a baked cloud: little-endian float32 x, y, z, nx, ny, nz, ao per point. Scans are
	/// sampled offline from a public-domain mesh, so the sample ships points rather than geometry.
	/// </summary>
	internal static CloudPoint[] FromBytes(byte[] bytes, int limit = int.MaxValue)
	{
		const int Stride = 7 * sizeof(float);
		var available = bytes.Length / Stride;
		var count = Math.Min(available, limit);
		var points = new CloudPoint[count];

		// Even stride across the whole cloud when fewer points are asked for, so thinning keeps the
		// figure's coverage instead of lopping off whichever end was baked last.
		var step = available / (double)Math.Max(1, count);

		for (var i = 0; i < count; i++)
		{
			var o = (int)(i * step) * Stride;
			points[i] = new CloudPoint(
				new Vector3(
					BitConverter.ToSingle(bytes, o),
					BitConverter.ToSingle(bytes, o + 4),
					BitConverter.ToSingle(bytes, o + 8)),
				new Vector3(
					BitConverter.ToSingle(bytes, o + 12),
					BitConverter.ToSingle(bytes, o + 16),
					BitConverter.ToSingle(bytes, o + 20)),
				BitConverter.ToSingle(bytes, o + 24));
		}

		return points;
	}

	/// <summary>
	/// Scatters points through the figure's box and pulls each one onto the surface along the field
	/// gradient, which lands them in proportion to area - dense where the form is, absent where it
	/// is not. Normals and occlusion come from the same field, so they cost almost nothing.
	/// </summary>
	internal static CloudPoint[] CreateFigure(int count, FigureKind kind, int seed = 20260401)
	{
		var rng = new Random(seed);
		var points = new CloudPoint[count];
		var found = 0;
		var attempts = 0;
		var maxAttempts = count * 40;

		// Keep only samples already within a thin shell of the surface before projecting. Points in
		// that band are distributed in proportion to area, so the figure fills in evenly instead of
		// crowding the silhouette, where the gradient is shallow and projection drifts.
		const float Band = 0.025f;

		while (found < count && attempts < maxAttempts)
		{
			attempts++;

			var p = new Vector3(
				(float)(rng.NextDouble() * 2.2 - 1.1),
				(float)(rng.NextDouble() * 2.4 - 1.2),
				(float)(rng.NextDouble() * 2.2 - 1.1));

			if (MathF.Abs(Sdf.Map(p, kind)) > Band)
			{
				continue;
			}

			if (!Project(ref p, kind))
			{
				continue;
			}

			var n = Sdf.Normal(p, kind);
			points[found++] = new CloudPoint(p, n, Sdf.Occlusion(p, n, kind));
		}

		if (found < count)
		{
			Array.Resize(ref points, Math.Max(1, found));
		}

		NormalizeHeight(points);
		return points;
	}

	/// <summary>Newton steps down the gradient onto the zero set; a few are enough for a smooth field.</summary>
	private static bool Project(ref Vector3 p, FigureKind kind)
	{
		for (var i = 0; i < 24; i++)
		{
			var d = Sdf.Map(p, kind);
			if (MathF.Abs(d) < 1e-4f)
			{
				return true;
			}

			if (MathF.Abs(d) > 3f)
			{
				return false;
			}

			p -= Sdf.Normal(p, kind) * d;
		}

		return MathF.Abs(Sdf.Map(p, kind)) < 2e-3f;
	}

	/// <summary>Rescales to y in [-1, 1], the convention the reference clouds are baked in.</summary>
	private static void NormalizeHeight(CloudPoint[] points)
	{
		if (points.Length == 0)
		{
			return;
		}

		var min = float.MaxValue;
		var max = float.MinValue;
		foreach (var p in points)
		{
			min = Math.Min(min, p.Position.Y);
			max = Math.Max(max, p.Position.Y);
		}

		var half = (max - min) * 0.5f;
		if (half <= 0f)
		{
			return;
		}

		var mid = (max + min) * 0.5f;
		for (var i = 0; i < points.Length; i++)
		{
			var p = points[i];
			points[i] = p with { Position = (p.Position - new Vector3(0, mid, 0)) / half };
		}
	}
}
