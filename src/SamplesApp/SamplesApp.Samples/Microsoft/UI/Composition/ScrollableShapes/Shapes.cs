#nullable enable

using System;
using System.Numerics;

namespace UITests.Shared.Windows_UI_Composition.ScrollableShapes;

/// <summary>
/// Four point clouds that share an index: point i of the sphere is point i of the cube, the torus
/// and the heart. That correspondence is the whole trick - morphing is then a straight lerp per
/// point, and the cloud reorganises rather than scrambling.
/// </summary>
internal static class Shapes
{
	internal const int PointCount = 3000;

	private const float TargetHeight = 200f;
	private static readonly float GoldenRatio = (1f + MathF.Sqrt(5f)) * 0.5f;

	internal static string[] Names { get; } = { "Sphere", "Cube", "Torus", "Heart" };

	internal static Vector3[][] Build()
	{
		var sphere = Normalize(Sphere(100f));
		var cube = Scale(Normalize(Cube(150f)), 0.75f);
		var torus = Scale(Normalize(Torus(50f, 25f)), 1.2f);
		var heart = Normalize(Heart(120f));
		return new[] { sphere, cube, torus, heart };
	}

	/// <summary>Evenly spread directions on a sphere; every shape is keyed off these angles.</summary>
	private static (float Theta, float Phi) Fibonacci(int i)
	{
		var t = i / (float)PointCount;
		return (2f * MathF.PI * i / GoldenRatio, MathF.Acos(1f - 2f * t));
	}

	private static Vector3[] Sphere(float radius)
	{
		var pts = new Vector3[PointCount];
		for (var i = 0; i < PointCount; i++)
		{
			var (theta, phi) = Fibonacci(i);
			pts[i] = new Vector3(
				radius * MathF.Sin(phi) * MathF.Cos(theta),
				radius * MathF.Sin(phi) * MathF.Sin(theta),
				radius * MathF.Cos(phi));
		}

		return pts;
	}

	private static Vector3[] Cube(float size)
	{
		var pts = new Vector3[PointCount];
		var s = size * 0.5f;
		for (var i = 0; i < PointCount; i++)
		{
			var (theta, phi) = Fibonacci(i);
			var sx = MathF.Sin(phi) * MathF.Cos(theta);
			var sy = MathF.Sin(phi) * MathF.Sin(theta);
			var sz = MathF.Cos(phi);

			// Push the sphere out onto the cube's faces along its own direction.
			var max = MathF.Max(MathF.Abs(sx), MathF.Max(MathF.Abs(sy), MathF.Abs(sz)));
			pts[i] = new Vector3(sx / max * s, sy / max * s, sz / max * s);
		}

		return pts;
	}

	private static Vector3[] Torus(float major, float minor)
	{
		var pts = new Vector3[PointCount];
		var ratio = major / minor;
		var minorSegments = (int)MathF.Round(MathF.Sqrt(PointCount / ratio));
		var majorSegments = (int)MathF.Round(PointCount / (float)minorSegments);

		var idx = 0;
		for (var i = 0; i < majorSegments && idx < PointCount; i++)
		{
			var u = i / (float)majorSegments * MathF.PI * 2f;
			for (var j = 0; j < minorSegments && idx < PointCount; j++)
			{
				var v = j / (float)minorSegments * MathF.PI * 2f;
				pts[idx++] = new Vector3(
					(major + minor * MathF.Cos(v)) * MathF.Cos(u),
					(major + minor * MathF.Cos(v)) * MathF.Sin(u),
					minor * MathF.Sin(v));
			}
		}

		while (idx < PointCount)
		{
			var t = idx / (float)PointCount;
			var u = t * MathF.PI * 2f * majorSegments;
			var v = t * MathF.PI * 2f * minorSegments;
			pts[idx++] = new Vector3(
				(major + minor * MathF.Cos(v)) * MathF.Cos(u),
				(major + minor * MathF.Cos(v)) * MathF.Sin(u),
				minor * MathF.Sin(v));
		}

		return pts;
	}

	private static Vector3[] Heart(float scale)
	{
		var pts = new Vector3[PointCount];
		for (var i = 0; i < PointCount; i++)
		{
			var (u, v) = Fibonacci(i);
			var sinV = MathF.Sin(v);
			var hx = sinV * (15f * MathF.Sin(u) - 4f * MathF.Sin(3f * u));
			var hz = 8f * MathF.Cos(v);
			var hy = sinV * (15f * MathF.Cos(u) - 5f * MathF.Cos(2f * u) - 2f * MathF.Cos(3f * u) - MathF.Cos(4f * u));
			pts[i] = new Vector3(hx * scale * 0.06f, -hy * scale * 0.06f, hz * scale * 0.06f);
		}

		return pts;
	}

	/// <summary>Centres each axis and scales so every shape stands the same height.</summary>
	private static Vector3[] Normalize(Vector3[] pts)
	{
		var min = new Vector3(float.MaxValue);
		var max = new Vector3(float.MinValue);
		foreach (var p in pts)
		{
			min = Vector3.Min(min, p);
			max = Vector3.Max(max, p);
		}

		var height = max.Y - min.Y;
		var scale = height > 1e-6f ? TargetHeight / height : 1f;
		var centre = (min + max) * 0.5f;

		var result = new Vector3[pts.Length];
		for (var i = 0; i < pts.Length; i++)
		{
			result[i] = (pts[i] - centre) * scale;
		}

		return result;
	}

	private static Vector3[] Scale(Vector3[] pts, float factor)
	{
		var result = new Vector3[pts.Length];
		for (var i = 0; i < pts.Length; i++)
		{
			result[i] = pts[i] * factor;
		}

		return result;
	}
}
