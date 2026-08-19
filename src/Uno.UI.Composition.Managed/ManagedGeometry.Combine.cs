#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Boolean combination for <see cref="ManagedGeometry"/> (union / intersect / difference / xor). Both
/// operands are flattened to closed polygons; every edge is split at its intersections with the other
/// operand, each resulting sub-edge is kept or dropped by testing its midpoint against the other region,
/// and the kept directed sub-edges are walked back into closed contours. Coordinates are snapped to a fine
/// grid so shared endpoints match exactly during reassembly. Curves become polylines, so the result matches
/// the rendered combination within flattening tolerance.
/// </summary>
internal sealed partial class ManagedGeometry
{
	private const float SnapScale = 256f;

	public IGeometry Combine(IGeometry other, GeometryCombineMode mode)
	{
		if (other is not ManagedGeometry b)
		{
			throw new NotSupportedException($"{nameof(ManagedGeometry)}.Combine requires a {nameof(ManagedGeometry)} operand.");
		}

		// Fast paths for axis-aligned rectangles — the overwhelmingly common clip case (layout/border clips
		// intersected down the visual tree). Avoids flattening + the O(edgesA*edgesB) SplitAll, which otherwise
		// dominates rendering when the managed geometry engine is used.
		var aIsRect = TryGetAxisAlignedRectangle(out var al, out var at, out var ar, out var ab);
		var bIsRect = b.TryGetAxisAlignedRectangle(out var bl, out var bt, out var br, out var bb);

		if (aIsRect && bIsRect)
		{
			switch (mode)
			{
				case GeometryCombineMode.Intersect:
				{
					var l = MathF.Max(al, bl);
					var t = MathF.Max(at, bt);
					var r = MathF.Min(ar, br);
					var bo = MathF.Min(ab, bb);
					return r <= l || bo <= t
						? new ManagedGeometry(Array.Empty<ManagedContour>(), GeometryFillRule.NonZero)
						: RectGeometry(l, t, r, bo);
				}
				case GeometryCombineMode.Union:
					// A union of two rectangles is itself a rectangle only when one contains the other; otherwise
					// fall through to the general path.
					if (al <= bl && at <= bt && ar >= br && ab >= bb)
					{
						return RectGeometry(al, at, ar, ab);
					}
					if (bl <= al && bt <= at && br >= ar && bb >= ab)
					{
						return RectGeometry(bl, bt, br, bb);
					}
					break;
					// Difference / Xor of rectangles is generally not a rectangle — use the general path.
			}
		}
		else if (mode == GeometryCombineMode.Intersect && (aIsRect || bIsRect))
		{
			// Intersecting an arbitrary geometry with a rectangle (e.g. a rounded-corner border clip, or content,
			// clipped by a layout rect) is O(n) Sutherland–Hodgman clipping — no all-pairs edge intersection.
			return aIsRect ? ClipToRect(b, al, at, ar, ab) : ClipToRect(this, bl, bt, br, bb);
		}

		var a = this;
		var polysA = a.FlattenClosedPolygons();
		var polysB = b.FlattenClosedPolygons();

		if (polysA.Count == 0)
		{
			// A is empty: Union/Xor with B is B; Intersect and Difference (A minus B, A empty) are both empty.
			return mode is GeometryCombineMode.Intersect or GeometryCombineMode.Difference
				? new ManagedGeometry(Array.Empty<ManagedContour>(), GeometryFillRule.NonZero)
				: b.AsNonZeroCopy(polysB);
		}

		if (polysB.Count == 0)
		{
			// B is empty: Intersect is empty; Union/Xor/Difference (A minus nothing) are all A.
			return mode is GeometryCombineMode.Intersect
				? new ManagedGeometry(Array.Empty<ManagedContour>(), GeometryFillRule.NonZero)
				: a.AsNonZeroCopy(polysA);
		}

		var edges = new List<(Vector2 A, Vector2 B)>();

		// A's edges, split at intersections with B, kept by A-vs-B classification.
		foreach (var (p0, p1) in SplitAll(polysA, polysB))
		{
			var mid = (p0 + p1) * 0.5f;
			var insideB = b.FillContains(mid);
			switch (mode)
			{
				case GeometryCombineMode.Union when !insideB:
				case GeometryCombineMode.Intersect when insideB:
				case GeometryCombineMode.Difference when !insideB:
					edges.Add((p0, p1));
					break;
				case GeometryCombineMode.Xor:
					// Union outer boundary forward; intersection boundary reversed → a hole under NonZero.
					edges.Add(insideB ? (p1, p0) : (p0, p1));
					break;
			}
		}

		// B's edges, split at intersections with A, kept by B-vs-A classification.
		foreach (var (p0, p1) in SplitAll(polysB, polysA))
		{
			var mid = (p0 + p1) * 0.5f;
			var insideA = a.FillContains(mid);
			switch (mode)
			{
				case GeometryCombineMode.Union when !insideA:
				case GeometryCombineMode.Intersect when insideA:
					edges.Add((p0, p1));
					break;
				case GeometryCombineMode.Difference when insideA:
					edges.Add((p1, p0)); // reversed: the hole cut out of A
					break;
				case GeometryCombineMode.Xor:
					edges.Add(insideA ? (p1, p0) : (p0, p1));
					break;
			}
		}

		return new ManagedGeometry(Reassemble(edges), GeometryFillRule.NonZero);
	}

	/// <summary>
	/// True when the geometry is a single closed, axis-aligned rectangle (all edges horizontal/vertical), returning
	/// its extents. Used to short-circuit <see cref="Combine"/> for the common rectangular-clip case.
	/// </summary>
	private bool TryGetAxisAlignedRectangle(out float left, out float top, out float right, out float bottom)
	{
		left = top = right = bottom = 0;

		if (Contours.Count != 1)
		{
			return false;
		}

		var contour = Contours[0];
		Span<Vector2> pts = stackalloc Vector2[5];
		var count = 0;
		pts[count++] = contour.Start;
		foreach (var seg in contour.Segments)
		{
			if (seg.Kind != ManagedSegmentKind.Line || count >= 5)
			{
				return false;
			}
			pts[count++] = seg.End;
		}

		// Drop the explicit closing point if it repeats the start.
		if (count >= 2 && pts[count - 1] == pts[0])
		{
			count--;
		}

		if (count != 4)
		{
			return false;
		}

		float minX = pts[0].X, maxX = pts[0].X, minY = pts[0].Y, maxY = pts[0].Y;
		for (var i = 1; i < 4; i++)
		{
			minX = MathF.Min(minX, pts[i].X);
			maxX = MathF.Max(maxX, pts[i].X);
			minY = MathF.Min(minY, pts[i].Y);
			maxY = MathF.Max(maxY, pts[i].Y);
		}

		if (maxX <= minX || maxY <= minY)
		{
			return false; // degenerate
		}

		for (var i = 0; i < 4; i++)
		{
			var p = pts[i];
			var q = pts[(i + 1) % 4];
			// Each edge must be axis-aligned, and each corner must sit on the bounding box.
			if ((p.X != q.X && p.Y != q.Y) ||
				(p.X != minX && p.X != maxX) ||
				(p.Y != minY && p.Y != maxY))
			{
				return false;
			}
		}

		left = minX;
		top = minY;
		right = maxX;
		bottom = maxY;
		return true;
	}

	private static ManagedGeometry RectGeometry(float left, float top, float right, float bottom)
	{
		var topLeft = new Vector2(left, top);
		var segments = new[]
		{
			ManagedPathSegment.Line(new Vector2(right, top)),
			ManagedPathSegment.Line(new Vector2(right, bottom)),
			ManagedPathSegment.Line(new Vector2(left, bottom)),
			ManagedPathSegment.Line(topLeft),
		};
		return new ManagedGeometry(new[] { new ManagedContour(topLeft, segments, closed: true) }, GeometryFillRule.NonZero);
	}

	/// <summary>
	/// Intersects a geometry with an axis-aligned rectangle via Sutherland–Hodgman clipping (O(n) per contour),
	/// avoiding the general boolean's O(edgesA*edgesB) edge splitting. Each contour is clipped independently.
	/// </summary>
	private static ManagedGeometry ClipToRect(ManagedGeometry g, float left, float top, float right, float bottom)
	{
		var polys = g.FlattenClosedPolygons();
		var contours = new List<ManagedContour>(polys.Count);
		foreach (var poly in polys)
		{
			// FlattenClosedPolygons may repeat the start as the last point; clip on the distinct ring.
			var length = poly.Length;
			if (length >= 2 && poly[length - 1] == poly[0])
			{
				length--;
			}

			var clipped = ClipRingToRect(poly, length, left, top, right, bottom);
			if (clipped.Count >= 3)
			{
				var segments = new ManagedPathSegment[clipped.Count];
				for (var i = 1; i < clipped.Count; i++)
				{
					segments[i - 1] = ManagedPathSegment.Line(clipped[i]);
				}
				segments[clipped.Count - 1] = ManagedPathSegment.Line(clipped[0]);
				contours.Add(new ManagedContour(clipped[0], segments, closed: true));
			}
		}

		return new ManagedGeometry(contours, GeometryFillRule.NonZero);
	}

	private enum RectEdge
	{
		Left,
		Right,
		Top,
		Bottom,
	}

	private static bool Inside(Vector2 p, RectEdge edge, float v) => edge switch
	{
		RectEdge.Left => p.X >= v,
		RectEdge.Right => p.X <= v,
		RectEdge.Top => p.Y >= v,
		_ => p.Y <= v,
	};

	private static Vector2 IntersectRectEdge(Vector2 a, Vector2 b, RectEdge edge, float v)
	{
		// The caller only intersects a segment that straddles the edge, so the divisor is never zero.
		if (edge is RectEdge.Left or RectEdge.Right)
		{
			var t = (v - a.X) / (b.X - a.X);
			return new Vector2(v, a.Y + t * (b.Y - a.Y));
		}

		var ty = (v - a.Y) / (b.Y - a.Y);
		return new Vector2(a.X + ty * (b.X - a.X), v);
	}

	private static List<Vector2> ClipRingToRect(Vector2[] ring, int length, float left, float top, float right, float bottom)
	{
		var input = new List<Vector2>(length);
		for (var i = 0; i < length; i++)
		{
			input.Add(ring[i]);
		}

		input = ClipHalfPlane(input, RectEdge.Left, left);
		input = ClipHalfPlane(input, RectEdge.Right, right);
		input = ClipHalfPlane(input, RectEdge.Top, top);
		input = ClipHalfPlane(input, RectEdge.Bottom, bottom);
		return input;
	}

	private static List<Vector2> ClipHalfPlane(List<Vector2> input, RectEdge edge, float v)
	{
		var output = new List<Vector2>(input.Count + 4);
		if (input.Count == 0)
		{
			return output;
		}

		var prev = input[input.Count - 1];
		var prevIn = Inside(prev, edge, v);
		foreach (var cur in input)
		{
			var curIn = Inside(cur, edge, v);
			if (curIn)
			{
				if (!prevIn)
				{
					output.Add(IntersectRectEdge(prev, cur, edge, v));
				}
				output.Add(cur);
			}
			else if (prevIn)
			{
				output.Add(IntersectRectEdge(prev, cur, edge, v));
			}

			prev = cur;
			prevIn = curIn;
		}

		return output;
	}

	private ManagedGeometry AsNonZeroCopy(List<Vector2[]> polygons)
	{
		var contours = new List<ManagedContour>(polygons.Count);
		foreach (var poly in polygons)
		{
			if (poly.Length < 2)
			{
				continue;
			}

			var segments = new ManagedPathSegment[poly.Length - 1];
			for (var i = 1; i < poly.Length; i++)
			{
				segments[i - 1] = ManagedPathSegment.Line(poly[i]);
			}

			contours.Add(new ManagedContour(poly[0], segments, closed: true));
		}

		return new ManagedGeometry(contours, GeometryFillRule.NonZero);
	}

	private List<Vector2[]> FlattenClosedPolygons()
	{
		var result = new List<Vector2[]>();
		foreach (var contour in Contours)
		{
			if (contour.Segments.Count == 0)
			{
				continue;
			}

			var pts = new List<Vector2> { Snap(contour.Start) };
			foreach (var flat in Flatten(contour, includeImplicitClose: true))
			{
				var s = Snap(flat);
				if (pts.Count == 0 || pts[^1] != s)
				{
					pts.Add(s);
				}
			}

			if (pts.Count >= 3)
			{
				result.Add(pts.ToArray());
			}
		}

		return result;
	}

	private static Vector2 Snap(Vector2 p)
		=> new(MathF.Round(p.X * SnapScale) / SnapScale, MathF.Round(p.Y * SnapScale) / SnapScale);

	/// <summary>Enumerates every edge of <paramref name="polys"/>, split at all crossings with <paramref name="others"/>.</summary>
	private static IEnumerable<(Vector2, Vector2)> SplitAll(List<Vector2[]> polys, List<Vector2[]> others)
	{
		foreach (var poly in polys)
		{
			for (var i = 1; i < poly.Length; i++)
			{
				var a = poly[i - 1];
				var b = poly[i];
				if (a == b)
				{
					continue;
				}

				var cuts = new List<float>();
				foreach (var other in others)
				{
					for (var j = 1; j < other.Length; j++)
					{
						if (SegmentIntersection(a, b, other[j - 1], other[j], out var t))
						{
							if (t > 1e-4f && t < 1 - 1e-4f)
							{
								cuts.Add(t);
							}
						}
					}
				}

				if (cuts.Count == 0)
				{
					yield return (a, b);
					continue;
				}

				cuts.Sort();
				var prev = a;
				var prevT = 0f;
				foreach (var t in cuts)
				{
					if (t - prevT < 1e-5f)
					{
						continue;
					}

					var p = Snap(Vector2.Lerp(a, b, t));
					if (p != prev)
					{
						yield return (prev, p);
					}

					prev = p;
					prevT = t;
				}

				if (prev != b)
				{
					yield return (prev, b);
				}
			}
		}
	}

	private static bool SegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out float t)
	{
		t = 0f;
		var d1 = p2 - p1;
		var d2 = p4 - p3;
		var denom = d1.X * d2.Y - d1.Y * d2.X;
		if (MathF.Abs(denom) < 1e-9f)
		{
			return false;
		}

		var dp = p3 - p1;
		t = (dp.X * d2.Y - dp.Y * d2.X) / denom;
		var u = (dp.X * d1.Y - dp.Y * d1.X) / denom;
		return t >= -1e-4f && t <= 1 + 1e-4f && u >= -1e-4f && u <= 1 + 1e-4f;
	}

	/// <summary>Walks kept directed edges back into closed contours, matching endpoints by snapped key.</summary>
	private static List<ManagedContour> Reassemble(List<(Vector2 A, Vector2 B)> edges)
	{
		var bySource = new Dictionary<(int, int), List<int>>();
		var used = new bool[edges.Count];
		for (var i = 0; i < edges.Count; i++)
		{
			if (edges[i].A == edges[i].B)
			{
				used[i] = true;
				continue;
			}

			var key = Key(edges[i].A);
			if (!bySource.TryGetValue(key, out var list))
			{
				bySource[key] = list = new List<int>();
			}

			list.Add(i);
		}

		var contours = new List<ManagedContour>();
		for (var start = 0; start < edges.Count; start++)
		{
			if (used[start])
			{
				continue;
			}

			var loop = new List<Vector2> { edges[start].A };
			var current = start;
			var guard = 0;
			while (current >= 0 && !used[current] && guard++ < edges.Count + 2)
			{
				used[current] = true;
				var end = edges[current].B;
				loop.Add(end);
				current = TakeNext(bySource, used, Key(end));
			}

			if (loop.Count >= 3)
			{
				var segments = new ManagedPathSegment[loop.Count - 1];
				for (var i = 1; i < loop.Count; i++)
				{
					segments[i - 1] = ManagedPathSegment.Line(loop[i]);
				}

				contours.Add(new ManagedContour(loop[0], segments, closed: true));
			}
		}

		return contours;
	}

	private static int TakeNext(Dictionary<(int, int), List<int>> bySource, bool[] used, (int, int) key)
	{
		if (bySource.TryGetValue(key, out var list))
		{
			foreach (var idx in list)
			{
				if (!used[idx])
				{
					return idx;
				}
			}
		}

		return -1;
	}

	private static (int, int) Key(Vector2 p) => ((int)MathF.Round(p.X * SnapScale), (int)MathF.Round(p.Y * SnapScale));
}
