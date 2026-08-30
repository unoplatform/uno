#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// Turns a flattened path into a NON-OVERLAPPING triangulation plus an analytic anti-aliasing ring, so a fill
/// can be drawn in one pass at ONE sample per pixel instead of relying on MSAA.
/// </summary>
/// <remarks>
/// The renderer's other option is stencil-then-cover: rasterize a triangle fan into the stencil, then blend a
/// full bounding-box quad through it. That is ~2x the bounding box of a shape whose ink may be a third of it,
/// and its edges are anti-aliased only by the multisampled attachment — which costs 4x the fill everywhere,
/// including the interior where nothing needs it. Skia instead computes coverage per fragment and never asks
/// for MSAA (Uno hands Ganesh sampleCount 0), which is what lets it antialias every shape differently inside
/// one single-sampled pass. This produces the same thing geometrically: the interior is inset by half a pixel
/// and filled at coverage 1, and a one-pixel ring straddling the true edge ramps coverage 1 -> 0. Coverage
/// travels in the existing per-vertex alpha, so no vertex layout or shader change is needed.
///
/// Split of work: <see cref="TryTriangulate"/> produces TOPOLOGY (triangle indices over the concatenated
/// contour points), which is AFFINE-INVARIANT and therefore cacheable across frames even while a spin or
/// scroll changes every device coordinate. <see cref="BuildGeometry"/> then runs per frame and is O(n): it
/// offsets the current device-space points along their bisectors and emits the triangles.
/// </remarks>
internal static class PathTessellator
{
	/// <summary>Total points across all contours we are willing to tessellate (ear clipping is O(n^2)).</summary>
	public const int MaxPoints = 512;

	/// <summary>
	/// Triangulates the contours, treating a contour nested an odd number of deep as a hole. Returns indices
	/// into the concatenated point list, or null when the input is not something we can tessellate safely.
	/// </summary>
	public static int[]? TryTriangulate(IReadOnlyList<List<Vector2>> contours)
	{
		var total = 0;
		for (var i = 0; i < contours.Count; i++)
		{
			if (contours[i].Count < 3) { return null; }
			total += contours[i].Count;
		}
		if (total < 3 || total > MaxPoints) { return null; }

		// Contour i is a hole iff it sits inside an odd number of the others. This is the even-odd rule; for
		// glyph outlines (non-zero with opposite-wound holes) it gives the same answer, and BuildGeometry's
		// area check rejects the cases where the two rules would disagree.
		var depth = new int[contours.Count];
		for (var i = 0; i < contours.Count; i++)
		{
			var probe = contours[i][0];
			for (var j = 0; j < contours.Count; j++)
			{
				if (i != j && Contains(contours[j], probe)) { depth[i]++; }
			}
		}

		// Group each hole with the immediate parent that encloses it (the deepest enclosing outer contour).
		var offsets = new int[contours.Count];
		var run = 0;
		for (var i = 0; i < contours.Count; i++) { offsets[i] = run; run += contours[i].Count; }

		var tris = new List<int>(total * 3);
		for (var o = 0; o < contours.Count; o++)
		{
			if ((depth[o] & 1) != 0) { continue; }   // a hole, handled by its parent

			var holes = new List<int>();
			for (var h = 0; h < contours.Count; h++)
			{
				if (h == o || (depth[h] & 1) == 0 || depth[h] != depth[o] + 1) { continue; }
				if (Contains(contours[o], contours[h][0])) { holes.Add(h); }
			}

			var poly = BridgeHoles(contours, o, holes, offsets);
			if (poly is null) { return null; }
			if (!EarClip(contours, offsets, poly, tris)) { return null; }
		}

		return tris.Count >= 3 ? tris.ToArray() : null;
	}

	/// <summary>
	/// Emits device-space triangles and their per-vertex coverage for the given (already triangulated) contours.
	/// Interior triangles are inset by <paramref name="aaHalf"/> and fully covered; a ring straddling each true
	/// edge ramps coverage to zero. Returns false when the shape is too thin to inset without turning inside out.
	/// </summary>
	public static bool BuildGeometry(
		IReadOnlyList<List<Vector2>> contours,
		int[] indices,
		float aaHalf,
		List<float> verts,
		List<float> coverage)
	{
		verts.Clear();
		coverage.Clear();

		var total = 0;
		for (var i = 0; i < contours.Count; i++) { total += contours[i].Count; }

		// Inward bisector offset per point. "Inward" means toward the filled region, which for a hole is out of
		// the hole -- the winding of each contour already encodes that, so the same formula serves both.
		var inset = new Vector2[total];
		var at = 0;
		if (aaHalf <= 0)
		{
			// No ring: the interior must sit on the true edge, not inset, or the shape loses half a pixel.
			for (var t = 0; t + 2 < indices.Length; t += 3)
			{
				for (var k = 0; k < 3; k++)
				{
					var p2 = PointAt(contours, indices[t + k]);
					verts.Add(p2.X); verts.Add(p2.Y);
					coverage.Add(1f);
				}
			}
			return verts.Count > 0;
		}
		for (var c = 0; c < contours.Count; c++)
		{
			var pts = contours[c];
			var n = pts.Count;
			var area2 = SignedArea2(pts);
			if (Math.Abs(area2) < 1e-9) { return false; }
			var sign = area2 > 0 ? 1f : -1f;

			for (var i = 0; i < n; i++)
			{
				var prev = pts[(i + n - 1) % n];
				var cur = pts[i];
				var next = pts[(i + 1) % n];

				var d0 = Norm(cur - prev);
				var d1 = Norm(next - cur);
				// Interior lies to the left of travel for a positively wound contour (and the device Y flip is
				// already baked into the points, so the sign is taken from the measured area, not assumed).
				var n0 = new Vector2(d0.Y, -d0.X) * sign;
				var n1 = new Vector2(d1.Y, -d1.X) * sign;

				var bis = n0 + n1;
				var len = bis.Length();
				if (len < 1e-4f)
				{
					// A 180-degree reversal (a spike): no meaningful bisector, so offset along the edge normal.
					inset[at + i] = n1 * aaHalf;
				}
				else
				{
					bis /= len;
					// Miter length grows as 1/cos(theta/2); clamp it so a sharp corner cannot shoot off.
					var scale = Math.Clamp(1f / Math.Max(Vector2.Dot(bis, n1), 0.25f), 1f, 4f);
					inset[at + i] = bis * (aaHalf * scale);
				}
			}
			at += n;
		}

		// Reject shapes thinner than the ramp: insetting would fold them inside out, which reads as missing ink.
		at = 0;
		for (var c = 0; c < contours.Count; c++)
		{
			var pts = contours[c];
			var n = pts.Count;
			for (var i = 0; i < n; i++)
			{
				var a = pts[i]; var b = pts[(i + 1) % n];
				var ia = a + inset[at + i]; var ib = b + inset[at + (i + 1) % n];
				var e = b - a; var ie = ib - ia;
				if (Vector2.Dot(e, ie) < 0) { return false; }
			}
			at += n;
		}

		// 1) Interior at full coverage, using the cached topology over the INSET positions.
		for (var t = 0; t + 2 < indices.Length; t += 3)
		{
			for (var k = 0; k < 3; k++)
			{
				var gi = indices[t + k];
				var p = PointAt(contours, gi) + inset[gi];
				verts.Add(p.X); verts.Add(p.Y);
				coverage.Add(1f);
			}
		}

		// 2) One-pixel ring per true edge: inset side fully covered, outset side empty. Skipped entirely when the
		// attachment is multisampled — the ring would be zero-width there, i.e. pure waste.
		if (aaHalf <= 0) { return verts.Count > 0; }
		at = 0;
		for (var c = 0; c < contours.Count; c++)
		{
			var pts = contours[c];
			var n = pts.Count;
			for (var i = 0; i < n; i++)
			{
				var i0 = at + i;
				var i1 = at + (i + 1) % n;
				var a = pts[i]; var b = pts[(i + 1) % n];
				var ia = a + inset[i0]; var ib = b + inset[i1];
				var oa = a - inset[i0]; var ob = b - inset[i1];

				Tri(verts, coverage, ia, 1f, ib, 1f, ob, 0f);
				Tri(verts, coverage, ia, 1f, ob, 0f, oa, 0f);
			}
			at += n;
		}

		return verts.Count > 0;
	}

	private static void Tri(List<float> v, List<float> cov, Vector2 a, float ca, Vector2 b, float cb, Vector2 c, float cc)
	{
		v.Add(a.X); v.Add(a.Y); cov.Add(ca);
		v.Add(b.X); v.Add(b.Y); cov.Add(cb);
		v.Add(c.X); v.Add(c.Y); cov.Add(cc);
	}

	private static Vector2 PointAt(IReadOnlyList<List<Vector2>> contours, int global)
	{
		for (var c = 0; c < contours.Count; c++)
		{
			if (global < contours[c].Count) { return contours[c][global]; }
			global -= contours[c].Count;
		}
		return default;
	}

	private static Vector2 Norm(Vector2 v)
	{
		var l = v.Length();
		return l < 1e-6f ? new Vector2(1, 0) : v / l;
	}

	public static double SignedArea2(List<Vector2> pts)
	{
		double a = 0;
		for (int i = 0, n = pts.Count; i < n; i++)
		{
			var p = pts[i]; var q = pts[(i + 1) % n];
			a += (double)p.X * q.Y - (double)q.X * p.Y;
		}
		return a;
	}

	private static bool Contains(List<Vector2> poly, Vector2 p)
	{
		var inside = false;
		for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
		{
			var a = poly[i]; var b = poly[j];
			if (a.Y > p.Y != b.Y > p.Y
				&& p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y + float.Epsilon) + a.X)
			{
				inside = !inside;
			}
		}
		return inside;
	}

	/// <summary>
	/// Splices each hole into the outer contour with a pair of coincident bridge edges, producing one simple
	/// polygon (the classic earcut approach). Returns global point indices in traversal order.
	/// </summary>
	private static List<int>? BridgeHoles(IReadOnlyList<List<Vector2>> contours, int outer, List<int> holes, int[] offsets)
	{
		var poly = new List<int>(contours[outer].Count + 8);
		for (var i = 0; i < contours[outer].Count; i++) { poly.Add(offsets[outer] + i); }
		if (holes.Count == 0) { return poly; }

		// Bridge the rightmost hole first: with the holes processed right-to-left, a bridge added for one hole
		// cannot separate a later hole from the outer boundary.
		holes.Sort((x, y) => Rightmost(contours[y]).X.CompareTo(Rightmost(contours[x]).X));

		foreach (var h in holes)
		{
			var hp = contours[h];
			var hi = RightmostIndex(hp);
			var hpt = hp[hi];

			// Connect to the visible outer vertex closest to the hole's rightmost point. A full visibility test
			// is overkill here: the candidate set is small, and BuildGeometry's area check catches a bad splice.
			var best = -1;
			var bestD = double.MaxValue;
			for (var k = 0; k < poly.Count; k++)
			{
				var p = PointAt(contours, poly[k]);
				if (p.X < hpt.X) { continue; }
				var d = (double)(p.X - hpt.X) * (p.X - hpt.X) + (double)(p.Y - hpt.Y) * (p.Y - hpt.Y);
				if (d < bestD) { bestD = d; best = k; }
			}
			if (best < 0)
			{
				for (var k = 0; k < poly.Count; k++)
				{
					var p = PointAt(contours, poly[k]);
					var d = (double)(p.X - hpt.X) * (p.X - hpt.X) + (double)(p.Y - hpt.Y) * (p.Y - hpt.Y);
					if (d < bestD) { bestD = d; best = k; }
				}
			}
			if (best < 0) { return null; }

			// Walk the hole in the opposite orientation to the outer contour, so the merged polygon stays simple.
			var outerCcw = SignedArea2(contours[outer]) > 0;
			var holeCcw = SignedArea2(hp) > 0;
			var seq = new List<int>(hp.Count + 2);
			for (var s = 0; s <= hp.Count; s++)
			{
				var idx = outerCcw == holeCcw
					? (hi - s + hp.Count * 2) % hp.Count
					: (hi + s) % hp.Count;
				seq.Add(offsets[h] + idx);
			}
			seq.Add(poly[best]);   // bridge back to the outer contour
			poly.InsertRange(best + 1, seq);
		}

		return poly;
	}

	private static Vector2 Rightmost(List<Vector2> pts) => pts[RightmostIndex(pts)];

	private static int RightmostIndex(List<Vector2> pts)
	{
		var bi = 0;
		for (var i = 1; i < pts.Count; i++) { if (pts[i].X > pts[bi].X) { bi = i; } }
		return bi;
	}

	private static bool EarClip(IReadOnlyList<List<Vector2>> contours, int[] offsets, List<int> poly, List<int> outTris)
	{
		var n = poly.Count;
		if (n < 3) { return false; }

		double area2 = 0;
		for (var i = 0; i < n; i++)
		{
			var p = PointAt(contours, poly[i]);
			var q = PointAt(contours, poly[(i + 1) % n]);
			area2 += (double)p.X * q.Y - (double)q.X * p.Y;
		}
		if (Math.Abs(area2) < 1e-9) { return false; }

		var live = new List<int>(n);
		if (area2 > 0) { for (var i = 0; i < n; i++) { live.Add(poly[i]); } }
		else { for (var i = n - 1; i >= 0; i--) { live.Add(poly[i]); } }

		var guard = n * n + 16;
		while (live.Count > 3)
		{
			var clipped = false;
			for (var i = 0; i < live.Count; i++)
			{
				int g0 = live[(i + live.Count - 1) % live.Count], g1 = live[i], g2 = live[(i + 1) % live.Count];
				var a = PointAt(contours, g0); var b = PointAt(contours, g1); var c = PointAt(contours, g2);
				if ((double)(b.X - a.X) * (c.Y - a.Y) - (double)(c.X - a.X) * (b.Y - a.Y) <= 0) { continue; }

				var empty = true;
				for (var j = 0; j < live.Count && empty; j++)
				{
					var g = live[j];
					if (g == g0 || g == g1 || g == g2) { continue; }
					empty = !PointInTri(PointAt(contours, g), a, b, c);
				}
				if (!empty) { continue; }

				outTris.Add(g0); outTris.Add(g1); outTris.Add(g2);
				live.RemoveAt(i);
				clipped = true;
				break;
			}
			if (!clipped || --guard < 0) { return false; }
		}

		outTris.Add(live[0]); outTris.Add(live[1]); outTris.Add(live[2]);
		return true;
	}

	private static bool PointInTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
	{
		var d1 = (double)(p.X - a.X) * (b.Y - a.Y) - (double)(b.X - a.X) * (p.Y - a.Y);
		var d2 = (double)(p.X - b.X) * (c.Y - b.Y) - (double)(c.X - b.X) * (p.Y - b.Y);
		var d3 = (double)(p.X - c.X) * (a.Y - c.Y) - (double)(a.X - c.X) * (p.Y - c.Y);
		var neg = d1 < 0 || d2 < 0 || d3 < 0;
		var pos = d1 > 0 || d2 > 0 || d3 > 0;
		return !(neg && pos);
	}
}
