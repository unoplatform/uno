#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-free <see cref="IPathBuilder"/>/<see cref="IPrimitiveGeometryBuilder"/> that accumulates
/// <see cref="ManagedContour"/>s. Quadratics, arcs, ellipses and rounded rectangles are converted to
/// cubic béziers so the resulting <see cref="ManagedGeometry"/> only ever holds lines and cubics.
/// </summary>
internal sealed class ManagedPathBuilder : IPathBuilder, IPrimitiveGeometryBuilder
{
	private const float Kappa = 0.5522847498307936f;

	private readonly List<ManagedContour> _contours = new();
	private List<ManagedPathSegment>? _segments;
	private Vector2 _start;
	private Vector2 _current;
	private bool _closedPending;
	// Set when the accumulated content is exactly one AddRectangle/AddRoundedRectangle call, so the built
	// geometry can advertise its analytic shape (IGeometry.TryGetRoundRect); any other verb clears it.
	private RoundRectangle? _pureRoundRect;

	private bool IsEmpty => _contours.Count == 0 && _segments is null;

	public GeometryFillRule FillRule { get; set; } = GeometryFillRule.NonZero;

	public void MoveTo(Vector2 point)
	{
		_pureRoundRect = null;
		FlushContour(closed: false);
		_start = point;
		_current = point;
		_segments = new List<ManagedPathSegment>();
		_closedPending = false;
	}

	public void LineTo(Vector2 point)
	{
		_pureRoundRect = null;
		EnsureContour();
		_segments!.Add(ManagedPathSegment.Line(point));
		_current = point;
	}

	public void CubicTo(Vector2 control1, Vector2 control2, Vector2 end)
	{
		_pureRoundRect = null;
		EnsureContour();
		_segments!.Add(ManagedPathSegment.Cubic(control1, control2, end));
		_current = end;
	}

	public void QuadraticTo(Vector2 control, Vector2 end)
	{
		_pureRoundRect = null;
		EnsureContour();
		// Elevate the quadratic to an equivalent cubic.
		var c1 = _current + (2f / 3f) * (control - _current);
		var c2 = end + (2f / 3f) * (control - end);
		_segments!.Add(ManagedPathSegment.Cubic(c1, c2, end));
		_current = end;
	}

	public void ArcTo(Vector2 radius, float rotationAngle, bool isLargeArc, bool clockwise, Vector2 end)
	{
		_pureRoundRect = null;
		EnsureContour();
		AppendSvgArc(_current, end, radius.X, radius.Y, rotationAngle * MathF.PI / 180f, isLargeArc, clockwise);
		_current = end;
	}

	public void Close()
	{
		if (_segments is { Count: > 0 })
		{
			_closedPending = true;
			FlushContour(closed: true);
		}

		_current = _start;
	}

	public void AddRectangle(Rect rect)
	{
		var wasEmpty = IsEmpty;
		FlushContour(closed: false);
		var l = (float)rect.Left;
		var t = (float)rect.Top;
		var r = (float)rect.Right;
		var b = (float)rect.Bottom;
		_start = new Vector2(l, t);
		_segments = new List<ManagedPathSegment>
		{
			ManagedPathSegment.Line(new Vector2(r, t)),
			ManagedPathSegment.Line(new Vector2(r, b)),
			ManagedPathSegment.Line(new Vector2(l, b)),
		};
		FlushContour(closed: true);
		_pureRoundRect = wasEmpty ? new RoundRectangle { Rect = rect } : null;
	}

	public void AddRoundedRectangle(Rect rect, float radiusX, float radiusY)
		=> AddRoundedRectangle(rect, new Vector2(radiusX, radiusY), new Vector2(radiusX, radiusY), new Vector2(radiusX, radiusY), new Vector2(radiusX, radiusY));

	public void AddRoundedRectangle(Rect rect, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft)
	{
		var wasEmpty = IsEmpty;
		FlushContour(closed: false);

		var l = (float)rect.Left;
		var t = (float)rect.Top;
		var r = (float)rect.Right;
		var b = (float)rect.Bottom;

		// Clamp radii so opposing corners never overlap (matches the usual round-rect normalization).
		var w = r - l;
		var h = b - t;
		Clamp(ref topLeft, ref topRight, w, isHorizontalPair: true);
		Clamp(ref bottomLeft, ref bottomRight, w, isHorizontalPair: true);
		Clamp(ref topLeft, ref bottomLeft, h, isHorizontalPair: false);
		Clamp(ref topRight, ref bottomRight, h, isHorizontalPair: false);

		_start = new Vector2(l + topLeft.X, t);
		_segments = new List<ManagedPathSegment>();

		// Top edge → TR corner
		_segments.Add(ManagedPathSegment.Line(new Vector2(r - topRight.X, t)));
		AppendCornerArc(new Vector2(r - topRight.X, t), new Vector2(r, t + topRight.Y), corner: new Vector2(r, t));
		// Right edge → BR corner
		_segments.Add(ManagedPathSegment.Line(new Vector2(r, b - bottomRight.Y)));
		AppendCornerArc(new Vector2(r, b - bottomRight.Y), new Vector2(r - bottomRight.X, b), corner: new Vector2(r, b));
		// Bottom edge → BL corner
		_segments.Add(ManagedPathSegment.Line(new Vector2(l + bottomLeft.X, b)));
		AppendCornerArc(new Vector2(l + bottomLeft.X, b), new Vector2(l, b - bottomLeft.Y), corner: new Vector2(l, b));
		// Left edge → TL corner
		_segments.Add(ManagedPathSegment.Line(new Vector2(l, t + topLeft.Y)));
		AppendCornerArc(new Vector2(l, t + topLeft.Y), new Vector2(l + topLeft.X, t), corner: new Vector2(l, t));

		FlushContour(closed: true);
		// Tag with the CLAMPED radii so the analytic shape matches the tessellated contour exactly.
		_pureRoundRect = wasEmpty
			? new RoundRectangle { Rect = rect, TopLeft = topLeft, TopRight = topRight, BottomRight = bottomRight, BottomLeft = bottomLeft }
			: null;

		static void Clamp(ref Vector2 a, ref Vector2 b, float extent, bool isHorizontalPair)
		{
			var sum = isHorizontalPair ? a.X + b.X : a.Y + b.Y;
			if (sum > extent && sum > 0)
			{
				var scale = extent / sum;
				if (isHorizontalPair) { a.X *= scale; b.X *= scale; }
				else { a.Y *= scale; b.Y *= scale; }
			}
		}
	}

	public void AddEllipse(Vector2 center, float radiusX, float radiusY)
	{
		_pureRoundRect = null;
		FlushContour(closed: false);
		_start = new Vector2(center.X + radiusX, center.Y);
		_segments = new List<ManagedPathSegment>();

		// Four cubic quarter-arcs, clockwise, via the kappa approximation.
		var kx = radiusX * Kappa;
		var ky = radiusY * Kappa;
		var right = new Vector2(center.X + radiusX, center.Y);
		var bottom = new Vector2(center.X, center.Y + radiusY);
		var left = new Vector2(center.X - radiusX, center.Y);
		var top = new Vector2(center.X, center.Y - radiusY);

		_segments.Add(ManagedPathSegment.Cubic(new Vector2(right.X, center.Y + ky), new Vector2(center.X + kx, bottom.Y), bottom));
		_segments.Add(ManagedPathSegment.Cubic(new Vector2(center.X - kx, bottom.Y), new Vector2(left.X, center.Y + ky), left));
		_segments.Add(ManagedPathSegment.Cubic(new Vector2(left.X, center.Y - ky), new Vector2(center.X - kx, top.Y), top));
		_segments.Add(ManagedPathSegment.Cubic(new Vector2(center.X + kx, top.Y), new Vector2(right.X, center.Y - ky), right));

		FlushContour(closed: true);
	}

	public void AddGeometry(IGeometry geometry)
	{
		if (geometry is not ManagedGeometry managed)
		{
			throw new NotSupportedException($"ManagedPathBuilder can only append a {nameof(ManagedGeometry)}.");
		}

		var wasEmpty = IsEmpty;
		FlushContour(closed: false);
		foreach (var contour in managed.Contours)
		{
			_contours.Add(contour);
		}
		_pureRoundRect = wasEmpty ? managed.SourceRoundRect : null;
	}

	public IGeometry Build()
	{
		FlushContour(closed: false);
		var geometry = new ManagedGeometry(_contours.ToArray(), FillRule, _pureRoundRect);
		_contours.Clear();
		FillRule = GeometryFillRule.NonZero;
		_pureRoundRect = null;
		return geometry;
	}

	private void EnsureContour()
	{
		if (_segments is null || _closedPending)
		{
			// A drawing verb after Close (or with no prior MoveTo) starts a fresh contour at the current point.
			_start = _current;
			_segments = new List<ManagedPathSegment>();
			_closedPending = false;
		}
	}

	private void FlushContour(bool closed)
	{
		if (_segments is { Count: > 0 })
		{
			_contours.Add(new ManagedContour(_start, _segments.ToArray(), closed));
		}

		_segments = null;
	}

	/// <summary>Appends one quarter-ellipse corner (kappa cubic) from <paramref name="from"/> to
	/// <paramref name="to"/>, curving toward the rectangle's <paramref name="corner"/>.</summary>
	private void AppendCornerArc(Vector2 from, Vector2 to, Vector2 corner)
	{
		if ((to - from).LengthSquared() < 1e-6f)
		{
			// Zero-radius corner: sharp vertex at the rectangle corner.
			_segments!.Add(ManagedPathSegment.Line(corner));
			return;
		}

		// Pull each endpoint toward the corner by kappa (endpoint tangents are axis-aligned here).
		var c1 = from + (corner - from) * Kappa;
		var c2 = to + (corner - to) * Kappa;
		_segments!.Add(ManagedPathSegment.Cubic(c1, c2, to));
	}

	/// <summary>Converts an SVG endpoint-parameterized arc into cubic segments and appends them.</summary>
	private void AppendSvgArc(Vector2 p0, Vector2 p1, float rx, float ry, float phi, bool largeArc, bool sweep)
	{
		rx = MathF.Abs(rx);
		ry = MathF.Abs(ry);
		if (rx < 1e-6f || ry < 1e-6f || p0 == p1)
		{
			_segments!.Add(ManagedPathSegment.Line(p1));
			return;
		}

		var cosPhi = MathF.Cos(phi);
		var sinPhi = MathF.Sin(phi);

		// Step 1: transform to the ellipse's coordinate frame.
		var dx = (p0.X - p1.X) / 2f;
		var dy = (p0.Y - p1.Y) / 2f;
		var x1p = cosPhi * dx + sinPhi * dy;
		var y1p = -sinPhi * dx + cosPhi * dy;

		// Step 2: correct out-of-range radii.
		var lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry);
		if (lambda > 1)
		{
			var s = MathF.Sqrt(lambda);
			rx *= s;
			ry *= s;
		}

		// Step 3: compute the transformed centre.
		var num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
		num = MathF.Max(0, num);
		var denom = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
		var coef = (largeArc != sweep ? 1f : -1f) * MathF.Sqrt(denom <= 0 ? 0 : num / denom);
		var cxp = coef * (rx * y1p / ry);
		var cyp = coef * -(ry * x1p / rx);

		// Step 4: back to user space and compute start/sweep angles.
		var cx = cosPhi * cxp - sinPhi * cyp + (p0.X + p1.X) / 2f;
		var cy = sinPhi * cxp + cosPhi * cyp + (p0.Y + p1.Y) / 2f;

		var theta1 = Angle(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
		var dTheta = Angle((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
		if (!sweep && dTheta > 0)
		{
			dTheta -= 2 * MathF.PI;
		}
		else if (sweep && dTheta < 0)
		{
			dTheta += 2 * MathF.PI;
		}

		var segments = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(dTheta) / (MathF.PI / 2f)));
		var delta = dTheta / segments;
		var alpha = (4f / 3f) * MathF.Tan(delta / 4f);
		var theta = theta1;

		for (var i = 0; i < segments; i++)
		{
			var thetaNext = theta + delta;
			var e1 = EllipsePoint(cx, cy, rx, ry, cosPhi, sinPhi, theta);
			var e2 = EllipsePoint(cx, cy, rx, ry, cosPhi, sinPhi, thetaNext);
			var d1 = EllipseDerivative(rx, ry, cosPhi, sinPhi, theta);
			var d2 = EllipseDerivative(rx, ry, cosPhi, sinPhi, thetaNext);
			_segments!.Add(ManagedPathSegment.Cubic(e1 + alpha * d1, e2 - alpha * d2, e2));
			theta = thetaNext;
		}
	}

	private static Vector2 EllipsePoint(float cx, float cy, float rx, float ry, float cosPhi, float sinPhi, float t)
	{
		var x = rx * MathF.Cos(t);
		var y = ry * MathF.Sin(t);
		return new Vector2(cx + cosPhi * x - sinPhi * y, cy + sinPhi * x + cosPhi * y);
	}

	private static Vector2 EllipseDerivative(float rx, float ry, float cosPhi, float sinPhi, float t)
	{
		var dx = -rx * MathF.Sin(t);
		var dy = ry * MathF.Cos(t);
		return new Vector2(cosPhi * dx - sinPhi * dy, sinPhi * dx + cosPhi * dy);
	}

	private static float Angle(float ux, float uy, float vx, float vy)
	{
		var dot = ux * vx + uy * vy;
		var len = MathF.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
		var ang = MathF.Acos(Math.Clamp(len <= 0 ? 1 : dot / len, -1f, 1f));
		return (ux * vy - uy * vx) < 0 ? -ang : ang;
	}
}
