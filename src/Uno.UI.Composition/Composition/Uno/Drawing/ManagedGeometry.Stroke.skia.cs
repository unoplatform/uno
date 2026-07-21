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

		var miter = style.LineJoin is StrokeJoin.Miter or StrokeJoin.MiterOrBevel;
		if (miter)
		{
			AddMiterTip(pieces, v, dIn, dOut, nIn, nOut, style.MiterLimit <= 0 ? 10f : style.MiterLimit, hw, side: 1);
			AddMiterTip(pieces, v, dIn, dOut, nIn, nOut, style.MiterLimit <= 0 ? 10f : style.MiterLimit, hw, side: -1);
		}
	}

	private static void AddMiterTip(List<ManagedContour> pieces, Vector2 v, Vector2 dIn, Vector2 dOut, Vector2 nIn, Vector2 nOut, float miterLimit, float hw, int side)
	{
		var a = v + side * nIn;
		var b = v + side * nOut;
		// Intersect line(a, dIn) with line(b, dOut).
		if (!LineIntersect(a, dIn, b, dOut, out var tip))
		{
			return;
		}

		var dist = Vector2.Distance(tip, v);
		var bisector = side * (nIn + nOut);
		if (dist > miterLimit * hw || Vector2.Dot(tip - v, bisector) <= 0)
		{
			return; // Over the miter limit or on the inner side → keep the bevel.
		}

		AddTriangle(pieces, a, tip, b);
	}

	private static void AddCap(List<ManagedContour> pieces, Vector2 end, Vector2 outwardDir, float hw, StrokeCap cap)
	{
		var n = Normal(outwardDir) * hw;
		switch (cap)
		{
			case StrokeCap.Round:
				AddDisc(pieces, end, hw);
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

	private static void AddDisc(List<ManagedContour> pieces, Vector2 center, float radius)
	{
		const int steps = 24;
		var segments = new ManagedPathSegment[steps];
		var start = center + new Vector2(radius, 0);
		for (var i = 1; i <= steps; i++)
		{
			var a = i / (float)steps * MathF.PI * 2f;
			segments[i - 1] = ManagedPathSegment.Line(center + new Vector2(radius * MathF.Cos(a), radius * MathF.Sin(a)));
		}

		pieces.Add(new ManagedContour(start, segments, closed: true));
	}

	private static void AddTriangle(List<ManagedContour> pieces, Vector2 a, Vector2 b, Vector2 c)
		=> pieces.Add(new ManagedContour(a, new[] { ManagedPathSegment.Line(b), ManagedPathSegment.Line(c) }, closed: true));

	private static void AddPolygon(List<ManagedContour> pieces, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
		=> pieces.Add(new ManagedContour(a, new[] { ManagedPathSegment.Line(b), ManagedPathSegment.Line(c), ManagedPathSegment.Line(d) }, closed: true));

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
