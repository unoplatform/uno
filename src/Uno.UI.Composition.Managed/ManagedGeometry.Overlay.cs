#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The general boolean of <see cref="Combine(IGeometry, GeometryCombineMode, float)"/>, on integer snap-grid
/// coordinates so every geometric decision is exact. The operands' edges are snap rounded (each operand against
/// itself too, since a stroke outline overlaps itself), which leaves edges that only meet at shared ends. An edge is
/// then on the result's boundary when the result fills one side of it and not the other.
/// </summary>
internal sealed partial class ManagedGeometry
{
	private readonly record struct GridPoint(long X, long Y)
	{
		public static GridPoint From(Vector2 p) => new((long)MathF.Round(p.X * SnapScale), (long)MathF.Round(p.Y * SnapScale));

		public Vector2 ToVector() => new(X / SnapScale, Y / SnapScale);
	}

	private readonly record struct GridEdge(GridPoint A, GridPoint B, bool InB);

	/// <summary>
	/// The boundary of `polysA` combined with `polysB`, as directed edges with the fill on their left.
	/// </summary>
	private static List<(Vector2 A, Vector2 B)> Overlay(List<Vector2[]> polysA, GeometryFillRule ruleA, List<Vector2[]> polysB, GeometryFillRule ruleB, GeometryCombineMode mode)
	{
		var edges = new List<GridEdge>();
		AddEdges(edges, polysA, inB: false);
		AddEdges(edges, polysB, inB: true);
		edges = SnapRound(edges);
		while (SplitAtVertices(ref edges))
		{
		}

		// Edges the operands share (or an operand repeats) are identical once rounded: classify each such group once.
		var groupByEnds = new Dictionary<(GridPoint, GridPoint), int>();
		var groupOf = new int[edges.Count];
		var groups = new List<List<int>>();
		for (var i = 0; i < edges.Count; i++)
		{
			var (a, b, _) = edges[i];
			var ends = Compare(a, b) <= 0 ? (a, b) : (b, a);
			if (!groupByEnds.TryGetValue(ends, out var group))
			{
				groupByEnds[ends] = group = groups.Count;
				groups.Add(new List<int>());
			}

			groupOf[i] = group;
			groups[group].Add(i);
		}

		EdgeBands? rowsByY = null, rowsByX = null;
		var boundary = new List<(Vector2 A, Vector2 B)>();
		for (var g = 0; g < groups.Count; g++)
		{
			var (p0, p1, _) = edges[groups[g][0]];
			// Cast across the edge, not along it: along +X for a steep edge, along +Y for a flat one.
			var alongX = Math.Abs(p1.Y - p0.Y) >= Math.Abs(p1.X - p0.X);
			var rows = alongX ? rowsByY ??= new EdgeBands(edges, byY: true, pad: 0) : rowsByX ??= new EdgeBands(edges, byY: false, pad: 0);
			if (FillsLeft(edges, rows, alongX, groupOf, g, groups[g], p0, p1, mode, ruleA, ruleB) is { } left)
			{
				boundary.Add(left ? (p0.ToVector(), p1.ToVector()) : (p1.ToVector(), p0.ToVector()));
			}
		}

		return boundary;
	}

	private static void AddEdges(List<GridEdge> edges, List<Vector2[]> polys, bool inB)
	{
		foreach (var poly in polys)
		{
			for (var i = 1; i < poly.Length; i++)
			{
				var (a, b) = (GridPoint.From(poly[i - 1]), GridPoint.From(poly[i]));
				if (a != b)
				{
					edges.Add(new GridEdge(a, b, inB));
				}
			}
		}
	}

	/// <summary>
	/// Snap rounding: every vertex and every crossing makes its grid cell "hot", and each edge is routed through the
	/// centre of every hot cell it passes through. Rounding crossings one at a time instead nudges edges into new
	/// crossings near dense clusters (a stroke outline's stamps) and never settles; after this pass, edges only meet at
	/// shared ends or coincide.
	/// </summary>
	private static List<GridEdge> SnapRound(List<GridEdge> edges)
	{
		var hot = new HashSet<GridPoint>();
		foreach (var (a, b, _) in edges)
		{
			hot.Add(a);
			hot.Add(b);
		}

		ForEachNearbyPair(edges, (i, j) =>
		{
			var (a, b, _) = edges[i];
			var (c, d, _) = edges[j];
			if (Math.Sign(Orient(a, b, c)) * Math.Sign(Orient(a, b, d)) < 0 && Math.Sign(Orient(c, d, a)) * Math.Sign(Orient(c, d, b)) < 0)
			{
				hot.Add(Crossing(a, b, c, d));
			}
		});

		var columns = new EdgeBands(edges, byY: false, pad: 1);
		var through = new List<GridPoint>?[edges.Count];
		foreach (var h in hot)
		{
			foreach (var i in columns.At(h.X))
			{
				var (a, b, _) = edges[i];
				if (h != a && h != b && PassesThrough(a, b, h))
				{
					(through[i] ??= new List<GridPoint>()).Add(h);
				}
			}
		}

		return SplitAt(edges, through);
	}

	// Whether segment (a, b) meets the closed grid cell centred on `h`, exactly: in doubled coordinates the cell's
	// corners are integers, and the segment misses it only when all four lie strictly on one side of its line.
	private static bool PassesThrough(GridPoint a, GridPoint b, GridPoint h)
	{
		var (a2, b2) = (new GridPoint(2 * a.X, 2 * a.Y), new GridPoint(2 * b.X, 2 * b.Y));
		var (left, right, top, bottom) = (2 * h.X - 1, 2 * h.X + 1, 2 * h.Y - 1, 2 * h.Y + 1);
		if (Math.Max(a2.X, b2.X) < left || Math.Min(a2.X, b2.X) > right || Math.Max(a2.Y, b2.Y) < top || Math.Min(a2.Y, b2.Y) > bottom)
		{
			return false;
		}

		var s0 = Math.Sign(Orient(a2, b2, new GridPoint(left, top)));
		var s1 = Math.Sign(Orient(a2, b2, new GridPoint(right, top)));
		var s2 = Math.Sign(Orient(a2, b2, new GridPoint(right, bottom)));
		var s3 = Math.Sign(Orient(a2, b2, new GridPoint(left, bottom)));
		return !((s0 > 0 && s1 > 0 && s2 > 0 && s3 > 0) || (s0 < 0 && s1 < 0 && s2 < 0 && s3 < 0));
	}

	/// <summary>
	/// Splits edges where an end of another lies inside them, which includes coincident edges overlapping only in
	/// part. Adds no new points, so repeating it settles. False when nothing needed splitting.
	/// </summary>
	private static bool SplitAtVertices(ref List<GridEdge> edges)
	{
		var list = edges;
		var at = new List<GridPoint>?[list.Count];
		var any = false;
		ForEachNearbyPair(list, (i, j) =>
		{
			var (a, b, _) = list[i];
			var (c, d, _) = list[j];
			any |= AddInside(at, i, a, b, c) | AddInside(at, i, a, b, d) | AddInside(at, j, c, d, a) | AddInside(at, j, c, d, b);
		});

		if (any)
		{
			edges = SplitAt(edges, at);
		}

		return any;
	}

	// Records `p` as a split point of edge (a, b) when it lies on the edge, strictly between its ends.
	private static bool AddInside(List<GridPoint>?[] at, int edge, GridPoint a, GridPoint b, GridPoint p)
	{
		if (p == a || p == b || Orient(a, b, p) != 0)
		{
			return false;
		}

		var (dx, dy) = (b.X - a.X, b.Y - a.Y);
		var along = (p.X - a.X) * dx + (p.Y - a.Y) * dy;
		if (along <= 0 || along >= dx * dx + dy * dy)
		{
			return false;
		}

		var list = at[edge] ??= new List<GridPoint>();
		if (list.Contains(p))
		{
			return false;
		}

		list.Add(p);
		return true;
	}

	// Each edge split at its points, in order along it.
	private static List<GridEdge> SplitAt(List<GridEdge> edges, List<GridPoint>?[] at)
	{
		var result = new List<GridEdge>(edges.Count);
		for (var i = 0; i < edges.Count; i++)
		{
			var (a, b, inB) = edges[i];
			if (at[i] is not { } points)
			{
				result.Add(edges[i]);
				continue;
			}

			var (dx, dy) = (b.X - a.X, b.Y - a.Y);
			points.Sort((p, q) => ((p.X - a.X) * dx + (p.Y - a.Y) * dy).CompareTo((q.X - a.X) * dx + (q.Y - a.Y) * dy));
			var prev = a;
			foreach (var p in points)
			{
				if (p != prev)
				{
					result.Add(new GridEdge(prev, p, inB));
					prev = p;
				}
			}

			if (prev != b)
			{
				result.Add(new GridEdge(prev, b, inB));
			}
		}

		return result;
	}

	// Calls `visit` for each pair of edges whose bounding boxes overlap, sweeping in x.
	private static void ForEachNearbyPair(List<GridEdge> edges, Action<int, int> visit)
	{
		var order = new int[edges.Count];
		var startX = new long[edges.Count];
		for (var i = 0; i < order.Length; i++)
		{
			order[i] = i;
			startX[i] = Math.Min(edges[i].A.X, edges[i].B.X);
		}
		Array.Sort(startX, order);

		for (var oi = 0; oi < order.Length; oi++)
		{
			var i = order[oi];
			var (a, b, _) = edges[i];
			var maxX = Math.Max(a.X, b.X);
			var minY = Math.Min(a.Y, b.Y);
			var maxY = Math.Max(a.Y, b.Y);
			for (var oj = oi + 1; oj < order.Length && startX[oj] <= maxX; oj++)
			{
				var j = order[oj];
				var (c, d, _) = edges[j];
				if ((c.Y >= minY || d.Y >= minY) && (c.Y <= maxY || d.Y <= maxY))
				{
					visit(i, j);
				}
			}
		}
	}

	// The cross product of (b - a) and (c - a): positive when c is on one side of the line ab, negative on the other.
	private static long Orient(GridPoint a, GridPoint b, GridPoint c)
		=> (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

	private static GridPoint Crossing(GridPoint a, GridPoint b, GridPoint c, GridPoint d)
	{
		double rx = b.X - a.X, ry = b.Y - a.Y, sx = d.X - c.X, sy = d.Y - c.Y;
		var t = ((c.X - a.X) * sy - (c.Y - a.Y) * sx) / (rx * sy - ry * sx);
		return new GridPoint((long)Math.Round(a.X + t * rx), (long)Math.Round(a.Y + t * ry));
	}

	private static int Compare(GridPoint p, GridPoint q) => p.X != q.X ? p.X.CompareTo(q.X) : p.Y.CompareTo(q.Y);

	/// <summary>
	/// Whether the result is filled on the left of the directed edge (p0, p1), or null when it is filled on both
	/// sides or neither (the edge isn't on the result's boundary). One ray cast from the edge's midpoint, skipping the
	/// edge's own group, gives each operand's winding on one side; the group's edges give the step to the other.
	/// </summary>
	private static bool? FillsLeft(List<GridEdge> edges, EdgeBands rows, bool alongX, int[] groupOf, int group, List<int> members, GridPoint p0, GridPoint p1, GeometryCombineMode mode, GeometryFillRule ruleA, GeometryFillRule ruleB)
	{
		// Doubled coordinates keep the midpoint on integers.
		var mid = new GridPoint(p0.X + p1.X, p0.Y + p1.Y);

		int windA = 0, windB = 0, crossA = 0, crossB = 0;
		foreach (var i in rows.At(alongX ? (p0.Y + p1.Y) / 2 : (p0.X + p1.X) / 2))
		{
			if (groupOf[i] == group)
			{
				continue;
			}

			var (a, b, inB) = edges[i];
			var step = RayStep(new GridPoint(2 * a.X, 2 * a.Y), new GridPoint(2 * b.X, 2 * b.Y), mid, alongX);
			if (step != 0)
			{
				if (inB)
				{
					windB += step;
					crossB++;
				}
				else
				{
					windA += step;
					crossA++;
				}
			}
		}

		// Those windings hold on the side the ray leaves through; on the other side, the group's own edges add theirs.
		int stepA = 0, stepB = 0, countA = 0, countB = 0;
		foreach (var i in members)
		{
			var (a, b, inB) = edges[i];
			var step = (alongX ? a.Y < b.Y : a.X < b.X) ? 1 : -1;
			if (inB)
			{
				stepB += step;
				countB++;
			}
			else
			{
				stepA += step;
				countA++;
			}
		}

		var raySide = InResult(mode, Fills(ruleA, windA, crossA), Fills(ruleB, windB, crossB));
		var otherSide = InResult(mode, Fills(ruleA, windA + stepA, crossA + countA), Fills(ruleB, windB + stepB, crossB + countB));
		if (raySide == otherSide)
		{
			return null;
		}

		// The ray side is +X (or +Y); it is the edge's left when the left normal (-dy, dx) points that way.
		var raySideIsLeft = alongX ? p1.Y < p0.Y : p1.X > p0.X;
		return raySide == raySideIsLeft;
	}

	// The winding step of edge (a, b) for a ray from `p` along +X (or +Y), half-open at the edge's ends.
	private static int RayStep(GridPoint a, GridPoint b, GridPoint p, bool alongX)
	{
		var (aAcross, bAcross, pAcross) = alongX ? (a.Y, b.Y, p.Y) : (a.X, b.X, p.X);
		if (!((aAcross <= pAcross && bAcross > pAcross) || (bAcross <= pAcross && aAcross > pAcross)))
		{
			return 0;
		}

		// The edge meets the ray's line beyond p when (hit - p) has the sign of the edge's run across the ray.
		var (aAlong, bAlong, pAlong) = alongX ? (a.X, b.X, p.X) : (a.Y, b.Y, p.Y);
		var beyond = (aAlong - pAlong) * (bAcross - aAcross) - (aAcross - pAcross) * (bAlong - aAlong);
		return (bAcross > aAcross ? beyond > 0 : beyond < 0) ? (aAcross <= pAcross ? 1 : -1) : 0;
	}

	private static bool Fills(GeometryFillRule rule, int winding, int crossings)
		=> rule == GeometryFillRule.EvenOdd ? (crossings & 1) == 1 : winding != 0;

	private static bool InResult(GeometryCombineMode mode, bool inA, bool inB) => mode switch
	{
		GeometryCombineMode.Union => inA || inB,
		GeometryCombineMode.Intersect => inA && inB,
		GeometryCombineMode.Difference => inA && !inB,
		_ => inA != inB,
	};

	/// <summary>
	/// Edges bucketed by the range they span across one axis (widened by `pad`), so a query at one coordinate only
	/// visits the edges that can reach it.
	/// </summary>
	private sealed class EdgeBands
	{
		private static readonly List<int> _none = new();

		private readonly List<int>?[] _bands;
		private readonly long _min;
		private readonly double _scale;

		public EdgeBands(List<GridEdge> edges, bool byY, long pad)
		{
			var min = long.MaxValue;
			var max = long.MinValue;
			foreach (var (a, b, _) in edges)
			{
				min = Math.Min(min, Math.Min(Across(a, byY), Across(b, byY)) - pad);
				max = Math.Max(max, Math.Max(Across(a, byY), Across(b, byY)) + pad);
			}

			_bands = new List<int>?[Math.Clamp((int)Math.Sqrt(edges.Count) * 2, 1, 1024)];
			_min = min;
			_scale = max > min ? _bands.Length / (double)(max - min) : 0d;
			for (var i = 0; i < edges.Count; i++)
			{
				var (a, b, _) = edges[i];
				var last = Band(Math.Max(Across(a, byY), Across(b, byY)) + pad);
				for (var band = Band(Math.Min(Across(a, byY), Across(b, byY)) - pad); band <= last; band++)
				{
					(_bands[band] ??= new List<int>()).Add(i);
				}
			}
		}

		public List<int> At(long across) => _bands[Band(across)] ?? _none;

		private int Band(long across) => Math.Clamp((int)((across - _min) * _scale), 0, _bands.Length - 1);

		private static long Across(GridPoint p, bool byY) => byY ? p.Y : p.X;
	}
}
