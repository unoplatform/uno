// A path's rasterisation inputs, resolved at draw time when the full transform is known: the outline flattened at
// device density as edges, its content hash, and the triangles that tile the fill with an AA ring when the shape
// admits one. Cached per geometry for each (recorded matrix, density) class, so a replay at the same density reuses
// it and a DPI or zoom change re-flattens at the right density instead of drawing stale triangles.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.WebGpu;

internal sealed class WebGpuShapeCache
{
	internal sealed class Shape
	{
		// All in the recording's space with the recorded translation left out (the command adds its Offset), so one
		// glyph drawn at a hundred positions is one entry.
		public float[] Edges;      // x0,y0,x1,y1 per edge; null when the outline could not be captured
		public long Hash;          // the outline relative to its bbox corner: identical shapes share atlas entries
		public Vector2 BbMin, BbMax;
		public float[] Tris;       // triangles tiling the fill (x,y per vertex); null when only a mask can draw it
		public float[] Cov;        // per-vertex coverage of Tris' AA ring; null = hard edges
		public static readonly Shape Empty = new();
	}

	// The quantisation policy, the one place deciding which transforms share a shape: density in sixteenths (exact
	// for every common DPI, a zoom re-tessellates sixteen times per octave) and the recorded linear part in 1/64.
	public static float DensityClass(float scale) => MathF.Max(1f / 16f, MathF.Round(scale * 16f) / 16f);
	private static long Q(float v) => (long)MathF.Round(v * 64f);

	private readonly record struct Key(long M11, long M12, long M21, long M22, float Density, bool EvenOdd, float Stroke);
	private readonly ConditionalWeakTable<IGeometry, Dictionary<Key, Shape>> _byGeometry = new();

	// Why fills took the mask route, and why tessellation refused (UNO_WEBGPU_STATS).
	internal static int StatFanRefused, StatTessPoints, StatTessTri, StatTessArea, StatTessFold;

	/// <summary>The fill of <paramref name="g"/> under the linear part of <paramref name="m"/>, at <paramref name="scale"/> device pixels per unit.</summary>
	public Shape Get(IGeometry g, in Matrix3x2 m, float scale, bool evenOdd) => Resolve(g, m, scale, evenOdd, 0f);

	/// <summary>The stroke of <paramref name="g"/>, <paramref name="width"/> wide in its own units, as a tiling strip.</summary>
	public Shape GetStroke(IGeometry g, in Matrix3x2 m, float width, float scale) => Resolve(g, m, scale, false, width);

	private Shape Resolve(IGeometry g, in Matrix3x2 m, float scale, bool evenOdd, float stroke)
	{
		var density = DensityClass(scale);
		var key = new Key(Q(m.M11), Q(m.M12), Q(m.M21), Q(m.M22), density, evenOdd, stroke);
		var shapes = _byGeometry.GetValue(g, static _ => new Dictionary<Key, Shape>());
		lock (shapes)
		{
			if (!shapes.TryGetValue(key, out var s))
			{
				if (shapes.Count >= 8) { shapes.Clear(); }   // a zoom's worth of classes; the geometry bounds the lifetime
				var linear = new Matrix3x2(m.M11, m.M12, m.M21, m.M22, 0f, 0f);
				s = stroke > 0f ? BuildStrip(g, linear, stroke, density) : BuildFill(g, linear, density, evenOdd);
				shapes[key] = s;
			}
			return s;
		}
	}

	// ------------------------------------------------------------------------------------------------ flattening

	// Streams the geometry's segments, mapped through the linear part, and flattens curves so they stay within a
	// fifth of a device pixel of the true curve at this density.
	private sealed class Flattener : IGeometrySink
	{
		private readonly Matrix3x2 _m;
		private readonly float _tolerance;
		private readonly bool _fill;   // a fill closes every contour and needs three points; a stroke keeps a two-point line
		public readonly List<List<Vector2>> Contours = new();
		private List<Vector2> _cur;
		private Vector2 _last;

		public Flattener(in Matrix3x2 m, float tolerance, bool fill = true) { _m = m; _tolerance = tolerance; _fill = fill; }

		private Vector2 Map(Vector2 p) => Vector2.Transform(p, _m);

		public void BeginFigure(Vector2 start) { _last = Map(start); _cur = new List<Vector2> { _last }; }
		public void LineTo(Vector2 point) { _cur?.Add(_last = Map(point)); }
		public void QuadTo(Vector2 control, Vector2 point)
		{
			var p0 = _last; var c = Map(control); var p1 = Map(point);
			Cubic(p0 + (c - p0) * (2f / 3f), p1 + (c - p1) * (2f / 3f), p1);
		}
		public void CubicTo(Vector2 control1, Vector2 control2, Vector2 point) => Cubic(Map(control1), Map(control2), Map(point));
		private void Cubic(Vector2 c1, Vector2 c2, Vector2 p1)
		{
			if (_cur is null) { return; }
			var p0 = _last;
			var chord = p1 - p0;
			var deviation = MathF.Max(Deviation(p0, chord, c1), Deviation(p0, chord, c2));
			var steps = Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(deviation / _tolerance)), 1, 256);
			for (var i = 1; i <= steps; i++)
			{
				float t = i / (float)steps, u = 1f - t;
				var q = u * u * u * p0 + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * p1;
				_cur.Add(q);
			}
			_last = p1;
		}
		public void EndFigure(bool closed)
		{
			if (_cur is { Count: >= 2 } pts)
			{
				// A fill closes every contour; a repeated closing point would make a zero-length edge.
				if (_fill && (pts[^1] - pts[0]).LengthSquared() < 1e-12f) { pts.RemoveAt(pts.Count - 1); }
				if (pts.Count >= (_fill ? 3 : 2)) { Contours.Add(pts); }
			}
			_cur = null;
		}

		private static float Deviation(Vector2 start, Vector2 chord, Vector2 p)
		{
			var len = chord.Length();
			var v = p - start;
			return len < 1e-6f ? v.Length() : MathF.Abs(v.X * chord.Y - v.Y * chord.X) / len;
		}
	}

	private const float FlattenTolerancePx = 0.2f;

	// ------------------------------------------------------------------------------------------------ fills

	private Shape BuildFill(IGeometry g, in Matrix3x2 linear, float density, bool evenOdd)
	{
		var flat = new Flattener(linear, FlattenTolerancePx / density);
		g.StreamSegments(flat);
		var contours = flat.Contours;
		if (contours.Count == 0) { return Shape.Empty; }
		PathTessellator.Simplify(contours, 0.03f / density);

		var s = new Shape();
		var total = 0;
		var bbMin = new Vector2(float.MaxValue); var bbMax = new Vector2(float.MinValue);
		foreach (var c in contours) { total += c.Count; foreach (var p in c) { bbMin = Vector2.Min(bbMin, p); bbMax = Vector2.Max(bbMax, p); } }
		s.BbMin = bbMin; s.BbMax = bbMax;
		s.Edges = Edges(contours, total);
		s.Hash = EdgeHash(s.Edges, bbMin);

		// Non-overlapping triangles plus the ring: the single-pass fill. The tessellator finds holes by even-odd depth
		// and the area check rejects outlines on which the two fill rules disagree, so a success serves either rule.
		if (Tessellate(contours, total, density, out s.Tris, out s.Cov)) { return s; }
		// A single non-zero contour that is star-shaped about its centroid tiles as a plain fan: hard edges, one pass.
		if (!evenOdd && contours.Count == 1 && CentroidFan(contours[0]) is { } fan) { s.Tris = fan; return s; }
		StatFanRefused++;
		return s;
	}

	private static float[] Edges(List<List<Vector2>> contours, int total)
	{
		var edges = new float[total * 4];
		var w = 0;
		foreach (var pts in contours)
		{
			for (var k = 0; k < pts.Count; k++)
			{
				var a = pts[k]; var b = pts[(k + 1) % pts.Count];
				edges[w++] = a.X; edges[w++] = a.Y; edges[w++] = b.X; edges[w++] = b.Y;
			}
		}
		return edges;
	}

	// The outline's identity for the atlas: its edges relative to its own bbox corner, bit for bit, so two geometry
	// objects with the same outline share one entry and nothing that differs by even a rounding step does (a shared
	// mask must render exactly as a fresh bake would). Never zero, so zero can mean "no outline".
	private static long EdgeHash(float[] edges, Vector2 origin)
	{
		ulong h = 14695981039346656037UL;
		for (var i = 0; i < edges.Length; i++)
		{
			var v = (ulong)(uint)BitConverter.SingleToInt32Bits(edges[i] - (i % 2 == 0 ? origin.X : origin.Y));
			h = (h ^ v) * 1099511628211UL;
		}
		h = (h ^ (ulong)edges.Length) * 1099511628211UL;
		return h == 0 ? 1 : (long)h;
	}

	private bool Tessellate(List<List<Vector2>> contours, int total, float density, out float[] tris, out float[] cov)
	{
		tris = cov = null;
		if (total > PathTessellator.MaxPoints) { StatTessPoints++; return false; }
		var idx = PathTessellator.TryTriangulate(contours);
		if (idx is null) { StatTessTri++; return false; }

		// The triangulation must cover the area the winding rule fills; if not, the rules disagree on this outline
		// (self-intersection, same-wound overlap) and only a mask draws it right. Both are twice the true area.
		var flat = new Vector2[total];
		var w = 0;
		foreach (var c in contours) { foreach (var p in c) { flat[w++] = p; } }
		double triArea = 0;
		for (var t = 0; t + 2 < idx.Length; t += 3)
		{
			var a = flat[idx[t]]; var b = flat[idx[t + 1]]; var c = flat[idx[t + 2]];
			triArea += Math.Abs((double)(b.X - a.X) * (c.Y - a.Y) - (double)(c.X - a.X) * (b.Y - a.Y));
		}
		double windArea = 0;
		foreach (var c in contours) { windArea += PathTessellator.SignedArea2(c); }
		if (Math.Abs(triArea - Math.Abs(windArea)) > 1e-2 * Math.Max(triArea, 1)) { StatTessArea++; return false; }

		// Half the antialiasing ring, in the recording's units: half a device pixel at this density.
		var verts = new List<float>(); var coverage = new List<float>();
		if (!PathTessellator.BuildGeometry(contours, idx, 0.5f / density, verts, coverage)) { StatTessFold++; return false; }
		tris = verts.ToArray();
		cov = coverage.ToArray();
		return true;
	}

	// A fan from the centroid tiles the contour iff every triangle winds the same way: sum|area| == |sum area|.
	private static float[] CentroidFan(List<Vector2> pts)
	{
		var n = pts.Count;
		var c = Vector2.Zero;
		foreach (var p in pts) { c += p; }
		c /= n;
		var fan = new float[n * 6];
		double abs = 0, signed = 0;
		for (var i = 0; i < n; i++)
		{
			var a = pts[i]; var b = pts[(i + 1) % n];
			fan[i * 6] = c.X; fan[i * 6 + 1] = c.Y; fan[i * 6 + 2] = a.X; fan[i * 6 + 3] = a.Y; fan[i * 6 + 4] = b.X; fan[i * 6 + 5] = b.Y;
			double ar = ((double)a.X - c.X) * ((double)b.Y - c.Y) - ((double)b.X - c.X) * ((double)a.Y - c.Y);
			abs += Math.Abs(ar); signed += ar;
		}
		return abs > 0 && Math.Abs(abs - Math.Abs(signed)) <= 1e-4 * abs ? fan : null;
	}

	// ------------------------------------------------------------------------------------------------ strokes

	// A stroke as a miter-joined triangle strip, which tiles, so it fills in one pass instead of baking the stroke's
	// OUTLINE into a bbox-sized mask whose cost is the bbox rather than the ink. Offsets are computed in the
	// geometry's own space before the linear map, so a non-uniform scale strokes correctly. Consecutive quads share
	// their join edge exactly, so a translucent stroke does not double-blend, except where the polyline crosses itself.
	private static Shape BuildStrip(IGeometry g, in Matrix3x2 linear, float width, float density)
	{
		// Flattened in local space at the density the linear map will produce.
		var scale = MathF.Max(new Vector2(linear.M11, linear.M12).Length(), new Vector2(linear.M21, linear.M22).Length());
		var flat = new OpenFlattener(FlattenTolerancePx / MathF.Max(density * scale, 1e-3f));
		g.StreamSegments(flat);
		var tris = new List<float>();
		var bbMin = new Vector2(float.MaxValue); var bbMax = new Vector2(float.MinValue);
		var h = width * 0.5f;
		foreach (var (pts, closed) in flat.Contours)
		{
			Strip(pts, closed, h, linear, tris, ref bbMin, ref bbMax);
		}
		if (tris.Count == 0) { return Shape.Empty; }
		return new Shape { Tris = tris.ToArray(), BbMin = bbMin, BbMax = bbMax };
	}

	// Flattens in the geometry's own space and keeps open contours open.
	private sealed class OpenFlattener : IGeometrySink
	{
		private readonly Flattener _inner;
		public readonly List<(List<Vector2> Pts, bool Closed)> Contours = new();
		public OpenFlattener(float tolerance) { _inner = new Flattener(Matrix3x2.Identity, tolerance, fill: false); }
		public void BeginFigure(Vector2 start) => _inner.BeginFigure(start);
		public void LineTo(Vector2 point) => _inner.LineTo(point);
		public void QuadTo(Vector2 control, Vector2 point) => _inner.QuadTo(control, point);
		public void CubicTo(Vector2 control1, Vector2 control2, Vector2 point) => _inner.CubicTo(control1, control2, point);
		public void EndFigure(bool closed)
		{
			var before = _inner.Contours.Count;
			_inner.EndFigure(closed);
			if (_inner.Contours.Count > before) { Contours.Add((_inner.Contours[^1], closed)); }
		}
	}

	private static void Strip(List<Vector2> pts, bool closed, float h, in Matrix3x2 m, List<float> tris, ref Vector2 bbMin, ref Vector2 bbMax)
	{
		for (int i = pts.Count - 1; i > 0; i--)
		{
			if ((pts[i] - pts[i - 1]).LengthSquared() < 1e-12f) { pts.RemoveAt(i); }
		}
		// A closed contour arrives with its closing point still on it (only the fill flattener drops that), and the
		// wrap segment from it to the start is then zero-length with no direction. Both ends of the contour would
		// take the no-miter fallback below and spike ten half-widths out of the first vertex.
		if (closed && pts.Count > 2 && (pts[^1] - pts[0]).LengthSquared() < 1e-12f) { pts.RemoveAt(pts.Count - 1); }
		var n = pts.Count;
		if (n < 2) { return; }

		var off = new Vector2[n];
		for (int i = 0; i < n; i++)
		{
			var hasPrev = i > 0 || closed;
			var hasNext = i < n - 1 || closed;
			var n1 = hasPrev ? Norm(pts[i] - pts[(i - 1 + n) % n]) : Vector2.Zero;
			var n2 = hasNext ? Norm(pts[(i + 1) % n] - pts[i]) : Vector2.Zero;
			if (!hasPrev || n1 == Vector2.Zero) { off[i] = Perp(n2) * h; continue; }
			if (!hasNext || n2 == Vector2.Zero) { off[i] = Perp(n1) * h; continue; }
			var mid = Perp(n1) + Perp(n2);
			var ml = mid.Length();
			if (ml < 1e-5f) { off[i] = Perp(n1) * h; continue; }   // 180 degree reversal: no finite miter
			mid /= ml;
			// miterLength = h / cos(theta/2); clamped so a near-degenerate corner cannot shoot off to infinity.
			// miterLength = h / cos(theta/2), bevelled rather than extended once the corner is sharp enough that the
			// miter would run away (a spike is never the right answer; it is what a degenerate corner used to draw).
			var cos = Vector2.Dot(mid, Perp(n1));
			off[i] = MathF.Abs(cos) < 0.25f ? Perp(n1) * h : mid * (h / cos);
		}

		var segs = closed ? n : n - 1;
		for (int i = 0; i < segs; i++)
		{
			var j = (i + 1) % n;
			var a0 = Vector2.Transform(pts[i] + off[i], m); var a1 = Vector2.Transform(pts[i] - off[i], m);
			var b0 = Vector2.Transform(pts[j] + off[j], m); var b1 = Vector2.Transform(pts[j] - off[j], m);
			foreach (var p in new[] { a0, b0, b1, a0, b1, a1 })
			{
				tris.Add(p.X); tris.Add(p.Y);
				bbMin = Vector2.Min(bbMin, p); bbMax = Vector2.Max(bbMax, p);
			}
		}

		static Vector2 Norm(Vector2 v) { var l = v.Length(); return l < 1e-6f ? Vector2.Zero : v / l; }
		static Vector2 Perp(Vector2 v) => new(-v.Y, v.X);
	}
}
