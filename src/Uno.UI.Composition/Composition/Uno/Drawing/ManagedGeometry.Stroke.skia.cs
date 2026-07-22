#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Stroke-to-fill for <see cref="ManagedGeometry"/>. Rather than build a single continuous outline (which
/// needs robust self-intersection handling), it stamps simple convex pieces — a quad per flattened segment,
/// a filler per join, a shape per cap — all wound the same way and filled NonZero, so overlaps merge into
/// the correct stroke region without any boolean union. Curves are flattened, so the result matches the
/// rendered stroke within flattening tolerance. Exotic WinUI quirks (clipped miter, triangle caps) are
/// approximated (bevel / no extra geometry) rather than reproduced bit-for-bit.
/// </summary>
internal sealed partial class ManagedGeometry
{
	public IGeometry GetStrokeFillGeometry(in StrokeStyle style)
	{
		var hw = style.Thickness / 2f;
		if (hw <= 0)
		{
			return new ManagedGeometry(Array.Empty<ManagedContour>(), GeometryFillRule.NonZero);
		}

		// Trim first (if requested), then dash, then stroke the resulting open runs.
		var source = (style.TrimStart != 0f || style.TrimEnd != 0f)
			? (ManagedGeometry)Trim(style.TrimStart, style.TrimEnd)
			: this;

		var pieces = new List<ManagedContour>();
		foreach (var run in source.EnumerateStrokeRuns(style))
		{
			if (run.ZeroLengthDash)
			{
				AddCap(pieces, run.Points[0], -run.Tangent, hw, run.StartCap); // backward
				AddCap(pieces, run.Points[0], run.Tangent, hw, run.EndCap); // forward
				continue;
			}

			StampRun(pieces, run.Points, run.Closed, run.StartCap, run.EndCap, hw, style);
		}

		return new ManagedGeometry(pieces, GeometryFillRule.NonZero);
	}

	private readonly struct StrokeRun
	{
		public StrokeRun(IReadOnlyList<Vector2> points, bool closed, StrokeCap startCap, StrokeCap endCap)
		{
			Points = points;
			Closed = closed;
			StartCap = startCap;
			EndCap = endCap;
		}

		public IReadOnlyList<Vector2> Points { get; }
		public bool Closed { get; }
		public StrokeCap StartCap { get; }
		public StrokeCap EndCap { get; }

		/// <summary>A zero-length dash at an endpoint that fell in a gap: a backward cap (<see cref="StartCap"/>)
		/// and a forward cap (<see cref="EndCap"/>) stamped at <see cref="Points"/>[0] along ±<see cref="Tangent"/>.</summary>
		public bool ZeroLengthDash { get; init; }
		public Vector2 Tangent { get; init; }
	}

	/// <summary>Flattens each contour and splits it into the runs to stroke (one per dash, or the whole contour).</summary>
	private IEnumerable<StrokeRun> EnumerateStrokeRuns(StrokeStyle style)
	{
		var hasDashes = style.DashArray is { Length: > 0 };
		var dashes = hasDashes ? ScaleDashes(style.DashArray!, style.Thickness) : null;
		var dashOffset = style.DashOffset * style.Thickness;

		foreach (var contour in Contours)
		{
			if (contour.Segments.Count == 0)
			{
				continue;
			}

			var points = new List<Vector2> { contour.Start };
			points.AddRange(Flatten(contour, includeImplicitClose: contour.Closed));

			if (!hasDashes)
			{
				yield return new StrokeRun(points, contour.Closed, style.StartCap, style.EndCap);
				continue;
			}

			// Dashed: walk the polyline by arc length, emitting each "on" interval as its own open run.
			foreach (var dashRun in SplitByDashes(points, dashes!, dashOffset, style, contour.Closed))
			{
				yield return dashRun;
			}
		}
	}

	private static float[] ScaleDashes(float[] dashArray, float thickness)
	{
		var scaled = new float[dashArray.Length % 2 == 0 ? dashArray.Length : dashArray.Length * 2];
		for (var i = 0; i < scaled.Length; i++)
		{
			scaled[i] = MathF.Max(0f, dashArray[i % dashArray.Length]) * thickness;
		}

		return scaled;
	}

	private static IEnumerable<StrokeRun> SplitByDashes(List<Vector2> points, float[] dashes, float dashOffset, StrokeStyle style, bool closed)
	{
		var total = 0f;
		foreach (var d in dashes)
		{
			total += d;
		}

		if (total <= 0)
		{
			yield return new StrokeRun(points, closed, style.StartCap, style.EndCap);
			yield break;
		}

		var pathLength = 0f;
		for (var i = 1; i < points.Count; i++)
		{
			pathLength += Vector2.Distance(points[i - 1], points[i]);
		}

		// Start the pattern accounting for the offset (walked backward so the first dash aligns like Skia).
		var patternPos = -(dashOffset % total);
		if (patternPos > 0)
		{
			patternPos -= total;
		}

		var idx = 0;
		var pos = patternPos;
		while (pos < pathLength)
		{
			var segLen = dashes[idx % dashes.Length];
			if (segLen > 0)
			{
				var isOn = idx % 2 == 0;
				if (isOn)
				{
					var from = MathF.Max(pos, 0f);
					var to = MathF.Min(pos + segLen, pathLength);
					if (from < to)
					{
						var sub = ExtractSubPolyline(points, from, to);
						if (sub.Count >= 2)
						{
							// Path ends use the real start/end caps; internal dash boundaries use the dash cap.
							var startCap = from <= 0f && !closed ? style.StartCap : style.DashCap;
							var endCap = to >= pathLength && !closed ? style.EndCap : style.DashCap;
							yield return new StrokeRun(sub, closed: false, startCap, endCap);
						}
					}
				}

				pos += segLen;
			}

			idx++;
		}

		// WinUI renders a zero-length dash at an open path's endpoint ONLY when the endpoint coincides with a
		// gap boundary (not mid-gap): a backward dash cap plus a forward end cap.
		if (!closed && points.Count >= 2 && EndpointAtGapBoundary(pathLength, dashes, patternPos))
		{
			yield return new StrokeRun(new[] { points[^1] }, closed: false, style.DashCap, style.EndCap)
			{
				ZeroLengthDash = true,
				Tangent = Dir(points[^2], points[^1]),
			};
		}
	}

	/// <summary>True when the path end falls exactly at a gap→dash boundary (WinUI's zero-length-dash condition).</summary>
	private static bool EndpointAtGapBoundary(float pathLength, float[] dashes, float patternPos)
	{
		const float tolerance = 0.1f;
		var pos = patternPos;
		var idx = 0;
		while (pos < pathLength)
		{
			var segLen = dashes[idx % dashes.Length];
			if (segLen <= 0)
			{
				idx++;
				continue;
			}

			var segEnd = pos + segLen;
			var isDash = idx % 2 == 0;
			if (segEnd >= pathLength - tolerance)
			{
				// Endpoint inside a rendered dash (already capped) or mid-gap → no zero-length dash;
				// only a gap ending at the endpoint qualifies.
				return !isDash && MathF.Abs(segEnd - pathLength) < tolerance;
			}

			pos = segEnd;
			idx++;
		}

		return false;
	}

	private static List<Vector2> ExtractSubPolyline(List<Vector2> points, float from, float to)
	{
		var result = new List<Vector2>();
		var pos = 0f;
		for (var i = 1; i < points.Count; i++)
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
			var lo = MathF.Max(segStart, from);
			var hi = MathF.Min(segEnd, to);
			if (lo < hi)
			{
				var p0 = Vector2.Lerp(a, b, (lo - segStart) / segLen);
				var p1 = Vector2.Lerp(a, b, (hi - segStart) / segLen);
				if (result.Count == 0)
				{
					result.Add(p0);
				}

				result.Add(p1);
			}

			pos = segEnd;
		}

		return result;
	}

	private static void StampRun(List<ManagedContour> pieces, IReadOnlyList<Vector2> pts, bool closed, StrokeCap startCap, StrokeCap endCap, float hw, StrokeStyle style)
	{
		// Drop consecutive duplicates so directions are well-defined.
		var p = new List<Vector2>(pts.Count);
		foreach (var pt in pts)
		{
			if (p.Count == 0 || Vector2.DistanceSquared(p[^1], pt) > 1e-8f)
			{
				p.Add(pt);
			}
		}

		if (closed && p.Count > 1 && Vector2.DistanceSquared(p[0], p[^1]) > 1e-8f)
		{
			p.Add(p[0]);
		}

		if (p.Count < 2)
		{
			// A degenerate run (single point) still renders a round/square dot when capped.
			if (p.Count == 1 && startCap == StrokeCap.Round)
			{
				AddDisc(pieces, p[0], hw);
			}

			return;
		}

		var count = p.Count;
		// Segment quads.
		for (var i = 1; i < count; i++)
		{
			AddSegmentQuad(pieces, p[i - 1], p[i], hw);
		}

		// Joins at interior vertices (and the wrap vertex for closed runs).
		var lastVertex = closed ? count - 1 : count - 1;
		for (var i = 1; i < lastVertex; i++)
		{
			AddJoin(pieces, p[i - 1], p[i], p[i + 1], hw, style);
		}

		if (closed)
		{
			// Join across the closing vertex (p[0]==p[^1]).
			AddJoin(pieces, p[count - 2], p[0], p[1], hw, style);
		}
		else
		{
			AddCap(pieces, p[0], Dir(p[1], p[0]), hw, startCap);
			AddCap(pieces, p[count - 1], Dir(p[count - 2], p[count - 1]), hw, endCap);
		}
	}

	private static Vector2 Dir(Vector2 from, Vector2 to)
	{
		var d = to - from;
		var len = d.Length();
		return len < 1e-6f ? new Vector2(1, 0) : d / len;
	}

	private static Vector2 Normal(Vector2 dir) => new(-dir.Y, dir.X);

	private static void AddSegmentQuad(List<ManagedContour> pieces, Vector2 a, Vector2 b, float hw)
	{
		var n = Normal(Dir(a, b)) * hw;
		AddPolygon(pieces, a + n, b + n, b - n, a - n);
	}

	private static void AddJoin(List<ManagedContour> pieces, Vector2 prev, Vector2 v, Vector2 next, float hw, StrokeStyle style)
	{
		var dIn = Dir(prev, v);
		var dOut = Dir(v, next);
		var cross = dIn.X * dOut.Y - dIn.Y * dOut.X;
		if (MathF.Abs(cross) < 1e-6f && Vector2.Dot(dIn, dOut) > 0)
		{
			return; // Collinear — the segment quads already meet flush.
		}

		if (style.LineJoin == StrokeJoin.Round)
		{
			AddDisc(pieces, v, hw);
			return;
		}

		var nIn = Normal(dIn) * hw;
		var nOut = Normal(dOut) * hw;

		// Bevel filler on both sides (the inner one is harmlessly inside the union).
		AddTriangle(pieces, v + nIn, v, v + nOut);
		AddTriangle(pieces, v - nIn, v, v - nOut);

		if (style.LineJoin is StrokeJoin.Miter or StrokeJoin.MiterOrBevel)
		{
			AddMiterJoin(pieces, v, dIn, dOut, hw, style.MiterLimit <= 0 ? 10f : style.MiterLimit);
		}
	}

	/// <summary>
	/// Adds the outer-side miter geometry: a full pointed tip within the limit, otherwise the WinUI
	/// miter-clip trapezoid truncated at the limit (matching SkiaGeometrySource2D's DoLimitedMiter).
	/// </summary>
	private static void AddMiterJoin(List<ManagedContour> pieces, Vector2 v, Vector2 dIn, Vector2 dOut, float hw, float miterLimit)
	{
		var dot = dIn.X * dOut.X + dIn.Y * dOut.Y;
		var sinHalfSq = (1 + dot) / 2;
		if (sinHalfSq <= 0)
		{
			return;
		}

		var sinHalf = MathF.Sqrt(sinHalfSq);
		var cross = dIn.X * dOut.Y - dIn.Y * dOut.X;
		if (MathF.Abs(cross) < 1e-6f)
		{
			return;
		}

		// Outward normals toward the miter (outer) side.
		Vector2 nIn, nOut;
		if (cross > 0)
		{
			nIn = new Vector2(dIn.Y, -dIn.X);
			nOut = new Vector2(dOut.Y, -dOut.X);
		}
		else
		{
			nIn = new Vector2(-dIn.Y, dIn.X);
			nOut = new Vector2(-dOut.Y, dOut.X);
		}

		var bevelIn = v + nIn * hw;
		var bevelOut = v + nOut * hw;

		if (sinHalf >= 1f / miterLimit)
		{
			// Within the limit: full pointed miter tip (intersection of the two outer offset edges).
			if (LineIntersect(bevelIn, dIn, bevelOut, dOut, out var tip))
			{
				AddTriangle(pieces, bevelIn, tip, bevelOut);
			}

			return;
		}

		// Over the limit: truncate the miter at the limit distance (clipped trapezoid).
		var cosHalfSq = (1 - dot) / 2;
		if (cosHalfSq <= 1e-12f)
		{
			return;
		}

		var rRatio = (miterLimit - sinHalf) / MathF.Sqrt(cosHalfSq);
		if (rRatio <= 0)
		{
			return;
		}

		var ext = rRatio * hw;
		AddPolygon(pieces, bevelIn, bevelIn + dIn * ext, bevelOut - dOut * ext, bevelOut);
	}

	private static void AddCap(List<ManagedContour> pieces, Vector2 end, Vector2 outwardDir, float hw, StrokeCap cap)
	{
		var n = Normal(outwardDir) * hw;
		switch (cap)
		{
			case StrokeCap.Round:
				AddSemicircle(pieces, end, outwardDir, hw);
				break;
			case StrokeCap.Square:
				var ext = outwardDir * hw;
				AddPolygon(pieces, end + n, end + n + ext, end - n + ext, end - n);
				break;
			case StrokeCap.Triangle:
				AddTriangle(pieces, end + n, end + outwardDir * hw, end - n);
				break;
			case StrokeCap.Butt:
			default:
				break;
		}
	}

	/// <summary>Half-disc at <paramref name="center"/> bulging toward <paramref name="outwardDir"/> (a round cap).</summary>
	private static void AddSemicircle(List<ManagedContour> pieces, Vector2 center, Vector2 outwardDir, float radius)
	{
		var startAngle = MathF.Atan2(-outwardDir.X, outwardDir.Y); // angle of the +normal (-dir.Y, dir.X)
		const int steps = 16;
		var pts = new Vector2[steps + 1];
		for (var i = 0; i <= steps; i++)
		{
			var a = startAngle + i / (float)steps * MathF.PI; // sweep 180° through the outward side
			pts[i] = center + new Vector2(radius * MathF.Cos(a), radius * MathF.Sin(a));
		}

		AddLoop(pieces, pts);
	}

	private static void AddDisc(List<ManagedContour> pieces, Vector2 center, float radius)
	{
		var steps = Math.Clamp((int)MathF.Ceiling(radius * 2f), 24, 96);
		var pts = new Vector2[steps];
		for (var i = 0; i < steps; i++)
		{
			var a = i / (float)steps * MathF.PI * 2f;
			pts[i] = center + new Vector2(radius * MathF.Cos(a), radius * MathF.Sin(a));
		}

		AddLoop(pieces, pts);
	}

	private static void AddTriangle(List<ManagedContour> pieces, Vector2 a, Vector2 b, Vector2 c)
		=> AddLoop(pieces, new[] { a, b, c });

	private static void AddPolygon(List<ManagedContour> pieces, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
		=> AddLoop(pieces, new[] { a, b, c, d });

	/// <summary>
	/// Adds a closed piece with a consistent (positive-area) winding. Every stamped piece must wind the same
	/// way so that under NonZero fill their overlaps union (winding accumulates) instead of cancelling to a hole.
	/// </summary>
	private static void AddLoop(List<ManagedContour> pieces, Vector2[] pts)
	{
		if (pts.Length < 3)
		{
			return;
		}

		double area = 0;
		for (var i = 0; i < pts.Length; i++)
		{
			var a = pts[i];
			var b = pts[(i + 1) % pts.Length];
			area += (double)a.X * b.Y - (double)b.X * a.Y;
		}

		if (area < 0)
		{
			Array.Reverse(pts);
		}

		var segments = new ManagedPathSegment[pts.Length - 1];
		for (var i = 1; i < pts.Length; i++)
		{
			segments[i - 1] = ManagedPathSegment.Line(pts[i]);
		}

		pieces.Add(new ManagedContour(pts[0], segments, closed: true));
	}

	private static bool LineIntersect(Vector2 p0, Vector2 d0, Vector2 p1, Vector2 d1, out Vector2 point)
	{
		var denom = d0.X * d1.Y - d0.Y * d1.X;
		if (MathF.Abs(denom) < 1e-6f)
		{
			point = default;
			return false;
		}

		var t = ((p1.X - p0.X) * d1.Y - (p1.Y - p0.Y) * d1.X) / denom;
		point = p0 + d0 * t;
		return true;
	}
}
