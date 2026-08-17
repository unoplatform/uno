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
