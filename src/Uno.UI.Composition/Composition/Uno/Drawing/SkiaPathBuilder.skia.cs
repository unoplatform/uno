#nullable enable

using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IPathBuilder"/> that accumulates into an <see cref="SKPathBuilder"/>.</summary>
internal sealed class SkiaPathBuilder : IPathBuilder
{
	private SKPathBuilder _builder = new();

	public void MoveTo(Vector2 point) => _builder.MoveTo(new SKPoint(point.X, point.Y));

	public void LineTo(Vector2 point) => _builder.LineTo(new SKPoint(point.X, point.Y));

	public void CubicTo(Vector2 control1, Vector2 control2, Vector2 end)
		=> _builder.CubicTo(new SKPoint(control1.X, control1.Y), new SKPoint(control2.X, control2.Y), new SKPoint(end.X, end.Y));

	public void QuadraticTo(Vector2 control, Vector2 end)
		=> _builder.QuadTo(new SKPoint(control.X, control.Y), new SKPoint(end.X, end.Y));

	public void AddRectangle(Rect rect) => _builder.AddRect(rect.ToSKRect());

	public void AddRoundedRectangle(Rect rect, float radiusX, float radiusY)
	{
		var roundRect = new SKRoundRect();
		var radius = new SKPoint(radiusX, radiusY);
		roundRect.SetRectRadii(rect.ToSKRect(), new[] { radius, radius, radius, radius });
		_builder.AddRoundRect(roundRect);
	}

	public void AddEllipse(Vector2 center, float radiusX, float radiusY)
		=> _builder.AddOval(new SKRect(center.X - radiusX, center.Y - radiusY, center.X + radiusX, center.Y + radiusY));

	public void Close() => _builder.Close();

	public IGeometry Build()
	{
		var geometry = new SkiaGeometrySource2D(_builder.Detach());
		_builder = new SKPathBuilder();
		return geometry;
	}
}
