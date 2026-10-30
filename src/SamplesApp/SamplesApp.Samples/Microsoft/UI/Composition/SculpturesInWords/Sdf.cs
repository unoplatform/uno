#nullable enable

using System;
using System.Numerics;

namespace UITests.Shared.Windows_UI_Composition.SculpturesInWords;

internal enum FigureKind
{
	Bust,
	Thinker,
	Winged,
}

/// <summary>
/// The figures are signed distance fields rather than meshes: a handful of blended primitives gives
/// a carved silhouette, exact normals from the gradient, and occlusion for free - with no scanned
/// geometry to ship.
/// </summary>
internal static class Sdf
{
	internal static float Map(Vector3 p, FigureKind kind) => kind switch
	{
		FigureKind.Thinker => Thinker(p),
		FigureKind.Winged => Winged(p),
		_ => Bust(p),
	};

	/// <summary>A head, neck and shoulders cut off square, like a museum bust.</summary>
	private static float Bust(Vector3 p)
	{
		var d = Ellipsoid(p - new Vector3(0f, 0.74f, 0.02f), new Vector3(0.25f, 0.31f, 0.27f));
		d = Smin(d, Ellipsoid(p - new Vector3(0f, 0.82f, -0.07f), new Vector3(0.28f, 0.27f, 0.29f)), 0.07f);
		d = Smin(d, Ellipsoid(p - new Vector3(0f, 0.62f, 0.11f), new Vector3(0.17f, 0.13f, 0.17f)), 0.09f);
		d = Smin(d, Ellipsoid(p - new Vector3(0f, 0.71f, 0.26f), new Vector3(0.05f, 0.10f, 0.07f)), 0.035f);
		d = Smin(d, Capsule(p, new Vector3(0f, 0.56f, 0f), new Vector3(0f, 0.28f, 0f), 0.12f), 0.08f);
		d = Smin(d, Ellipsoid(p - new Vector3(0f, 0.02f, 0f), new Vector3(0.44f, 0.33f, 0.23f)), 0.11f);
		d = Smin(d, Capsule(p, new Vector3(-0.32f, 0.18f, 0f), new Vector3(0.32f, 0.18f, 0f), 0.16f), 0.10f);
		d = Smin(d, Capsule(p, new Vector3(-0.40f, 0.14f, 0f), new Vector3(-0.47f, -0.18f, 0.02f), 0.12f), 0.07f);
		d = Smin(d, Capsule(p, new Vector3(0.40f, 0.14f, 0f), new Vector3(0.47f, -0.18f, 0.02f), 0.12f), 0.07f);

		// Square the bottom off: the plinth cut is what makes it read as a bust and not a doll.
		return MathF.Max(d, -(p.Y + 0.42f));
	}

	/// <summary>A seated figure, elbow on knee, chin on hand.</summary>
	private static float Thinker(Vector3 p)
	{
		var d = Ellipsoid(p - new Vector3(0.02f, 0.70f, 0.10f), new Vector3(0.19f, 0.22f, 0.20f));
		d = Smin(d, Capsule(p, new Vector3(0f, 0.56f, 0.06f), new Vector3(-0.02f, 0.42f, -0.02f), 0.10f), 0.07f);
		d = Smin(d, Ellipsoid(p - new Vector3(-0.02f, 0.18f, -0.04f), new Vector3(0.28f, 0.30f, 0.22f)), 0.12f);

		// Thigh forward, shin down: the seated L that carries the pose.
		d = Smin(d, Capsule(p, new Vector3(-0.14f, -0.08f, 0.02f), new Vector3(-0.10f, -0.14f, 0.40f), 0.15f), 0.10f);
		d = Smin(d, Capsule(p, new Vector3(0.16f, -0.08f, 0.02f), new Vector3(0.14f, -0.12f, 0.36f), 0.14f), 0.10f);
		d = Smin(d, Capsule(p, new Vector3(-0.10f, -0.14f, 0.40f), new Vector3(-0.10f, -0.62f, 0.30f), 0.11f), 0.09f);
		d = Smin(d, Capsule(p, new Vector3(0.14f, -0.12f, 0.36f), new Vector3(0.16f, -0.62f, 0.22f), 0.10f), 0.09f);

		// Supporting arm: shoulder to elbow resting on the knee, then forearm up to the chin.
		d = Smin(d, Capsule(p, new Vector3(0.22f, 0.34f, -0.02f), new Vector3(0.26f, 0.00f, 0.22f), 0.09f), 0.08f);
		d = Smin(d, Capsule(p, new Vector3(0.26f, 0.00f, 0.22f), new Vector3(0.10f, 0.52f, 0.16f), 0.08f), 0.08f);
		d = Smin(d, Capsule(p, new Vector3(-0.24f, 0.34f, -0.02f), new Vector3(-0.26f, -0.04f, 0.14f), 0.09f), 0.08f);

		return MathF.Max(d, -(p.Y + 0.78f));
	}

	/// <summary>A winged torso: no head, drapery, two swept wings.</summary>
	private static float Winged(Vector3 p)
	{
		var d = Ellipsoid(p - new Vector3(0f, 0.34f, 0f), new Vector3(0.24f, 0.30f, 0.18f));
		d = Smin(d, Ellipsoid(p - new Vector3(0.02f, -0.10f, 0.02f), new Vector3(0.26f, 0.30f, 0.20f)), 0.12f);
		d = Smin(d, Capsule(p, new Vector3(-0.04f, -0.34f, 0.04f), new Vector3(-0.06f, -0.82f, 0.10f), 0.17f), 0.12f);
		d = Smin(d, Capsule(p, new Vector3(-0.20f, 0.50f, -0.02f), new Vector3(-0.30f, 0.22f, 0.06f), 0.08f), 0.07f);

		// Wings: thin flattened ellipsoids swept back and up from the shoulders.
		d = Smin(d, Wing(p, +1f), 0.09f);
		d = Smin(d, Wing(p, -1f), 0.09f);

		return MathF.Max(d, -(p.Y + 0.92f));
	}

	private static float Wing(Vector3 p, float side)
	{
		var q = p - new Vector3(0.14f * side, 0.46f, -0.10f);
		var c = MathF.Cos(0.5f * side);
		var s = MathF.Sin(0.5f * side);
		q = new Vector3(q.X * c - q.Z * s, q.Y, q.X * s + q.Z * c);
		q.X *= side;
		return Ellipsoid(q - new Vector3(0.22f, 0.10f, -0.22f), new Vector3(0.30f, 0.46f, 0.07f));
	}

	internal static Vector3 Normal(Vector3 p, FigureKind kind)
	{
		const float H = 0.0015f;
		var dx = Map(p + new Vector3(H, 0, 0), kind) - Map(p - new Vector3(H, 0, 0), kind);
		var dy = Map(p + new Vector3(0, H, 0), kind) - Map(p - new Vector3(0, H, 0), kind);
		var dz = Map(p + new Vector3(0, 0, H), kind) - Map(p - new Vector3(0, 0, H), kind);
		var g = new Vector3(dx, dy, dz);
		var len = g.Length();
		return len > 1e-6f ? g / len : Vector3.UnitY;
	}

	/// <summary>Walks away from the surface along the normal; ground it does not clear is occlusion.</summary>
	internal static float Occlusion(Vector3 p, Vector3 n, FigureKind kind)
	{
		var occ = 0f;
		var weight = 1f;
		for (var i = 1; i <= 5; i++)
		{
			var step = 0.02f * i * i;
			var d = Map(p + n * step, kind);
			occ += MathF.Max(0f, step - d) * weight;
			weight *= 0.72f;
		}

		return Math.Clamp(1f - occ * 3.2f, 0.08f, 1f);
	}

	private static float Smin(float a, float b, float k)
	{
		var h = Math.Clamp(0.5f + 0.5f * (b - a) / k, 0f, 1f);
		return b + (a - b) * h - k * h * (1f - h);
	}

	private static float Ellipsoid(Vector3 p, Vector3 r)
	{
		var k0 = new Vector3(p.X / r.X, p.Y / r.Y, p.Z / r.Z).Length();
		var k1 = new Vector3(p.X / (r.X * r.X), p.Y / (r.Y * r.Y), p.Z / (r.Z * r.Z)).Length();
		return k1 > 1e-6f ? k0 * (k0 - 1f) / k1 : -MathF.Min(r.X, MathF.Min(r.Y, r.Z));
	}

	private static float Capsule(Vector3 p, Vector3 a, Vector3 b, float r)
	{
		var pa = p - a;
		var ba = b - a;
		var h = Math.Clamp(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba), 0f, 1f);
		return (pa - ba * h).Length() - r;
	}
}
