#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A SkiaSharp-free <see cref="IGeometry"/>. The path is normalized to line and cubic-bézier segments
/// (quadratics, arcs, ellipses and rounded rects are converted on the way in), so every operation is
/// plain arithmetic. Backends that can't consume it natively read <see cref="Contours"/> and rebuild
/// their own representation (the Skia backend turns them into an SKPath for rasterization).
/// </summary>
internal sealed partial class ManagedGeometry : IGeometry, IGeometrySource2D
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

	// Combine lives in ManagedGeometry.Combine.skia.cs.

	public IGeometry GetFilledGeometry(float trimStart, float trimEnd)
	{
		// The fill path of a fill (non-stroke) is the path itself; a (0,0) trim means "no trimming".
		if (trimStart == 0f && trimEnd == 0f)
		{
			return new ManagedGeometry(Contours, FillRule);
		}

		return Trim(trimStart, trimEnd);
	}

	// GetStrokeFillGeometry lives in ManagedGeometry.Stroke.skia.cs.

	public void StreamFlattened(IFlattenedPathSink sink)
	{
		foreach (var contour in Contours)
		{
			if (contour.Segments.Count == 0)
			{
				continue;
			}

			sink.BeginContour(contour.Start);
			foreach (var p in Flatten(contour, includeImplicitClose: false))
			{
				sink.LineTo(p);
			}

			sink.EndContour(contour.Closed);
		}
	}

	public void StreamSegments(IGeometrySink sink)
	{
		foreach (var contour in Contours)
		{
			if (contour.Segments.Count == 0)
			{
				continue;
			}

			sink.BeginFigure(contour.Start);
			foreach (var seg in contour.Segments)
			{
				if (seg.Kind == ManagedSegmentKind.Line)
				{
					sink.LineTo(seg.End);
				}
				else
				{
					sink.CubicTo(seg.C1, seg.C2, seg.End);
				}
			}

			sink.EndFigure(contour.Closed);
		}
	}

	/// <summary>
	/// Trims the outline to the arc-length fraction [<paramref name="trimStart"/>, <paramref name="trimEnd"/>]
	/// of the concatenated contour length (Skia's normal <c>CreateTrim</c>). Contours are flattened, so the
	/// result is a polyline — matching the rendered curve within flattening tolerance.
	/// </summary>
	private ManagedGeometry Trim(float trimStart, float trimEnd)
	{
		var polylines = new List<(Vector2[] Points, float StartLength)>();
		var total = 0f;
		foreach (var contour in Contours)
		{
			if (contour.Segments.Count == 0)
			{
				continue;
			}

			var pts = new List<Vector2> { contour.Start };
			foreach (var flat in Flatten(contour, includeImplicitClose: contour.Closed))
			{
				pts.Add(flat);
			}

			polylines.Add((pts.ToArray(), total));
			for (var i = 1; i < pts.Count; i++)
			{
				total += Vector2.Distance(pts[i - 1], pts[i]);
			}
		}

		var startLen = trimStart * total;
		var endLen = trimEnd * total;
		if (total <= 0 || startLen >= endLen)
		{
			return new ManagedGeometry(Array.Empty<ManagedContour>(), FillRule);
		}

		var result = new List<ManagedContour>();
		foreach (var (points, offset) in polylines)
		{
			var kept = new List<Vector2>();
			var pos = offset;
			for (var i = 1; i < points.Length; i++)
			{
				var a = points[i - 1];
				var b = points[i];
				var segLen = Vector2.Distance(a, b);
				if (segLen <= 0)
				{
					continue;
				}

				var segStart = pos;
				var segEnd = pos + segLen;
				// Intersect [segStart, segEnd] with [startLen, endLen].
				var lo = MathF.Max(segStart, startLen);
				var hi = MathF.Min(segEnd, endLen);
				if (lo < hi)
				{
					var p0 = Vector2.Lerp(a, b, (lo - segStart) / segLen);
					var p1 = Vector2.Lerp(a, b, (hi - segStart) / segLen);
					if (kept.Count == 0)
					{
						kept.Add(p0);
					}

					kept.Add(p1);
				}

				pos = segEnd;
			}

			if (kept.Count >= 2)
			{
				var segments = new ManagedPathSegment[kept.Count - 1];
				for (var i = 1; i < kept.Count; i++)
				{
					segments[i - 1] = ManagedPathSegment.Line(kept[i]);
				}

				result.Add(new ManagedContour(kept[0], segments, closed: false));
			}
		}

		return new ManagedGeometry(result, FillRule);
	}

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
				// Adaptive: ~2px chords, scaled by the control-polygon length, so large curves stay smooth.
				var controlLength = Vector2.Distance(current, seg.C1) + Vector2.Distance(seg.C1, seg.C2) + Vector2.Distance(seg.C2, seg.End);
				var steps = Math.Clamp((int)MathF.Ceiling(controlLength / 2f), 8, 256);
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
