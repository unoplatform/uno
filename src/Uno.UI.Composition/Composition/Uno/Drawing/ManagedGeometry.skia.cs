#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A SkiaSharp-free <see cref="IGeometry"/>. The path is normalized to line and cubic-bézier segments
/// (quadratics, arcs, ellipses and rounded rects are converted on the way in), so every operation is
/// plain arithmetic. Backends that can't consume it natively read <see cref="Contours"/> and rebuild
/// their own representation (the Skia backend turns them into an SKPath for rasterization).
/// </summary>
internal sealed class ManagedGeometry : IGeometry
{
	private Rect? _bounds;

	public ManagedGeometry(IReadOnlyList<ManagedContour> contours, GeometryFillRule fillRule)
	{
		Contours = contours;
		FillRule = fillRule;
	}

	/// <summary>The sub-paths, each a start point plus line/cubic segments and a closed flag.</summary>
	public IReadOnlyList<ManagedContour> Contours { get; }

	public GeometryFillRule FillRule { get; }

	public Rect Bounds => _bounds ??= ComputeTightBounds();

	public bool IsEmpty
	{
		get
		{
			foreach (var contour in Contours)
			{
				if (contour.Segments.Count > 0)
				{
					return false;
				}
			}

			return true;
		}
	}

	public bool FillContains(Vector2 point)
	{
		// Ray-cast against the flattened outline. NonZero uses the winding number; EvenOdd uses parity.
		var winding = 0;
		var crossings = 0;

		foreach (var contour in Contours)
		{
			if (contour.Segments.Count == 0)
			{
				continue;
			}

			var prev = contour.Start;
			// The fill of a subpath always behaves as if closed (the implicit closing edge counts).
			foreach (var flat in Flatten(contour, includeImplicitClose: true))
			{
				CountRayCrossing(prev, flat, point, ref winding, ref crossings);
				prev = flat;
			}
		}

		return FillRule == GeometryFillRule.EvenOdd ? (crossings & 1) == 1 : winding != 0;
	}

	public IGeometry Transform(Matrix3x2 matrix)
	{
		var transformed = new ManagedContour[Contours.Count];
		for (var i = 0; i < Contours.Count; i++)
		{
			var contour = Contours[i];
			var segments = new ManagedPathSegment[contour.Segments.Count];
			for (var s = 0; s < segments.Length; s++)
			{
				var seg = contour.Segments[s];
				segments[s] = seg.Kind == ManagedSegmentKind.Line
					? ManagedPathSegment.Line(Vector2.Transform(seg.End, matrix))
					: ManagedPathSegment.Cubic(
						Vector2.Transform(seg.C1, matrix),
						Vector2.Transform(seg.C2, matrix),
						Vector2.Transform(seg.End, matrix));
			}

			transformed[i] = new ManagedContour(Vector2.Transform(contour.Start, matrix), segments, contour.Closed);
		}

		return new ManagedGeometry(transformed, FillRule);
	}

	// Implemented in later parts of the managed-geometry engine.
	public IGeometry Combine(IGeometry other, GeometryCombineMode mode) => throw new NotImplementedException("ManagedGeometry.Combine");

	public IGeometry GetFilledGeometry(float trimStart, float trimEnd) => throw new NotImplementedException("ManagedGeometry.GetFilledGeometry");

	public IGeometry GetStrokeFillGeometry(in StrokeStyle style) => throw new NotImplementedException("ManagedGeometry.GetStrokeFillGeometry");

	public void Dispose() { }

	private Rect ComputeTightBounds()
	{
		var hasPoint = false;
		float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

		void Include(Vector2 p)
		{
			hasPoint = true;
			if (p.X < minX) { minX = p.X; }
			if (p.Y < minY) { minY = p.Y; }
			if (p.X > maxX) { maxX = p.X; }
			if (p.Y > maxY) { maxY = p.Y; }
		}

		foreach (var contour in Contours)
		{
			if (contour.Segments.Count == 0)
			{
				continue;
			}

			var start = contour.Start;
			Include(start);
			var current = start;
			foreach (var seg in contour.Segments)
			{
				if (seg.Kind == ManagedSegmentKind.Line)
				{
					Include(seg.End);
				}
				else
				{
					// Tight bounds of a cubic: endpoints plus the axis extrema (roots of the derivative).
					Include(seg.End);
					IncludeCubicExtrema(current, seg.C1, seg.C2, seg.End, Include);
				}

				current = seg.End;
			}
		}

		return hasPoint ? new Rect(minX, minY, maxX - minX, maxY - minY) : default;
	}

	private static void IncludeCubicExtrema(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Action<Vector2> include)
	{
		for (var axis = 0; axis < 2; axis++)
		{
			float c0 = axis == 0 ? p0.X : p0.Y;
			float c1 = axis == 0 ? p1.X : p1.Y;
			float c2 = axis == 0 ? p2.X : p2.Y;
			float c3 = axis == 0 ? p3.X : p3.Y;

			// B'(t) = 3[(a)t² + (b)t + c]; solve a t² + b t + c = 0 on (0,1).
			float a = -c0 + 3 * c1 - 3 * c2 + c3;
			float b = 2 * (c0 - 2 * c1 + c2);
			float c = c1 - c0;

			foreach (var t in SolveQuadratic(a, b, c))
			{
				if (t > 0 && t < 1)
				{
					include(EvaluateCubic(p0, p1, p2, p3, t));
				}
			}
		}
	}

	private static IEnumerable<float> SolveQuadratic(float a, float b, float c)
	{
		if (MathF.Abs(a) < 1e-7f)
		{
			if (MathF.Abs(b) > 1e-7f)
			{
				yield return -c / b;
			}

			yield break;
		}

		var disc = b * b - 4 * a * c;
		if (disc < 0)
		{
			yield break;
		}

		var sqrt = MathF.Sqrt(disc);
		yield return (-b + sqrt) / (2 * a);
		yield return (-b - sqrt) / (2 * a);
	}

	internal static Vector2 EvaluateCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
	{
		var u = 1 - t;
		return (u * u * u) * p0 + (3 * u * u * t) * p1 + (3 * u * t * t) * p2 + (t * t * t) * p3;
	}

	/// <summary>Flattens a contour's segments into a polyline of end points (curves subdivided).</summary>
	internal static IEnumerable<Vector2> Flatten(ManagedContour contour, bool includeImplicitClose)
	{
		var current = contour.Start;
		foreach (var seg in contour.Segments)
		{
			if (seg.Kind == ManagedSegmentKind.Line)
			{
				yield return seg.End;
			}
			else
			{
				const int steps = 24;
				for (var i = 1; i <= steps; i++)
				{
					yield return EvaluateCubic(current, seg.C1, seg.C2, seg.End, i / (float)steps);
				}
			}

			current = seg.End;
		}

		if (includeImplicitClose && current != contour.Start)
		{
			yield return contour.Start;
		}
	}

	private static void CountRayCrossing(Vector2 a, Vector2 b, Vector2 point, ref int winding, ref int crossings)
	{
		// Horizontal ray to +X from `point`; count edges crossing the ray, tracking direction for winding.
		if ((a.Y <= point.Y && b.Y > point.Y) || (b.Y <= point.Y && a.Y > point.Y))
		{
			var t = (point.Y - a.Y) / (b.Y - a.Y);
			var xCross = a.X + t * (b.X - a.X);
			if (xCross > point.X)
			{
				crossings++;
				winding += a.Y <= point.Y ? 1 : -1;
			}
		}
	}
}

internal enum ManagedSegmentKind
{
	Line,
	Cubic,
}

/// <summary>A single path segment, normalized to either a line or a cubic bézier.</summary>
internal readonly struct ManagedPathSegment
{
	public ManagedSegmentKind Kind { get; private init; }
	public Vector2 C1 { get; private init; }
	public Vector2 C2 { get; private init; }
	public Vector2 End { get; private init; }

	public static ManagedPathSegment Line(Vector2 end) => new() { Kind = ManagedSegmentKind.Line, End = end };

	public static ManagedPathSegment Cubic(Vector2 c1, Vector2 c2, Vector2 end)
		=> new() { Kind = ManagedSegmentKind.Cubic, C1 = c1, C2 = c2, End = end };
}

/// <summary>A sub-path: a start point, its line/cubic segments, and whether it is closed.</summary>
internal sealed class ManagedContour
{
	public ManagedContour(Vector2 start, IReadOnlyList<ManagedPathSegment> segments, bool closed)
	{
		Start = start;
		Segments = segments;
		Closed = closed;
	}

	public Vector2 Start { get; }
	public IReadOnlyList<ManagedPathSegment> Segments { get; }
	public bool Closed { get; }
}
