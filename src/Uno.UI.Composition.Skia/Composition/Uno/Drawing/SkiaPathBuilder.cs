#nullable enable

using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IPathBuilder"/> that accumulates into an <see cref="SKPathBuilder"/>.</summary>
internal sealed class SkiaPathBuilder : IPathBuilder, IPrimitiveGeometryBuilder
{
	private readonly SKPathBuilder _builder = new();

	// A contour of straight lines (a chart's polyline) is gathered and handed over in one native call rather than one
	// per point.
	private SKPoint[] _poly = [];
	private int _polyCount;

	/// <summary>Hands a polyline in progress to the native builder, so the next verb lands after it. Every verb that is
	/// not a straight line calls this first.</summary>
	private SKPathBuilder FlushPending()
	{
		if (_polyCount > 0)
		{
			FlushPoly(close: false);
		}
		return _builder;
	}

	private void FlushPoly(bool close)
	{
		var count = _polyCount;
		_polyCount = 0;
		_builder.AddPoly(new System.ReadOnlySpan<SKPoint>(_poly, 0, count), close);
	}

	private void AppendPoly(SKPoint point)
	{
		if (_polyCount == _poly.Length)
		{
			System.Array.Resize(ref _poly, System.Math.Max(16, _poly.Length * 2));
		}
		_poly[_polyCount++] = point;
	}

	public void MoveTo(Vector2 point)
	{
		FlushPending();
		AppendPoly(new SKPoint(point.X, point.Y));
	}

	public void LineTo(Vector2 point)
	{
		if (_polyCount > 0)
		{
			AppendPoly(new SKPoint(point.X, point.Y));
			return;
		}
		_builder.LineTo(new SKPoint(point.X, point.Y));
	}

	public void CubicTo(Vector2 control1, Vector2 control2, Vector2 end)
		=> FlushPending().CubicTo(new SKPoint(control1.X, control1.Y), new SKPoint(control2.X, control2.Y), new SKPoint(end.X, end.Y));

	public void QuadraticTo(Vector2 control, Vector2 end)
		=> FlushPending().QuadTo(new SKPoint(control.X, control.Y), new SKPoint(end.X, end.Y));

	public void ArcTo(Vector2 radius, float rotationAngle, bool isLargeArc, bool clockwise, Vector2 end)
		=> FlushPending().ArcTo(
			new SKPoint(radius.X, radius.Y),
			rotationAngle,
			isLargeArc ? SKPathArcSize.Large : SKPathArcSize.Small,
			clockwise ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise,
			new SKPoint(end.X, end.Y));

	public void AddRectangle(Rect rect) => FlushPending().AddRect(rect.ToSKRect());

	public void AddRoundedRectangle(Rect rect, float radiusX, float radiusY)
	{
		using var roundRect = new SKRoundRect();
		var radius = new SKPoint(radiusX, radiusY);
		roundRect.SetRectRadii(rect.ToSKRect(), new[] { radius, radius, radius, radius });
		FlushPending().AddRoundRect(roundRect);
	}

	public void AddRoundedRectangle(Rect rect, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft)
	{
		using var roundRect = new SKRoundRect();
		roundRect.SetRectRadii(rect.ToSKRect(), new[]
		{
			new SKPoint(topLeft.X, topLeft.Y),
			new SKPoint(topRight.X, topRight.Y),
			new SKPoint(bottomRight.X, bottomRight.Y),
			new SKPoint(bottomLeft.X, bottomLeft.Y),
		});
		FlushPending().AddRoundRect(roundRect);
	}

	public void AddEllipse(Vector2 center, float radiusX, float radiusY)
		=> FlushPending().AddOval(new SKRect(center.X - radiusX, center.Y - radiusY, center.X + radiusX, center.Y + radiusY));

	public void AddGeometry(IGeometry geometry)
	{
		using var lease = SkiaGeometryInterop.Lease(geometry);
		FlushPending().AddPath(lease.Path, SKPathAddMode.Append);
	}

	public void Close()
	{
		if (_polyCount > 0)
		{
			FlushPoly(close: true);
			return;
		}
		_builder.Close();
	}

	private GeometryFillRule _fillRule;

	public GeometryFillRule FillRule
	{
		get => _fillRule;
		set
		{
			_fillRule = value;
			_builder.FillType = value == GeometryFillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
		}
	}

	public IGeometry Build()
	{
		// The builder is reused rather than reallocated; Detach hands over an independent path.
		var geometry = new SkiaGeometrySource2D(FlushPending().Detach());
		_builder.Reset();
		// A reset builder fills by winding already, and setting it costs a native round trip per path.
		_fillRule = GeometryFillRule.NonZero;
		return geometry;
	}
}
