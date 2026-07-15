#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend-neutral builder that accumulates path segments and produces an <see cref="IGeometry"/>.
/// Mirrors the imperative path-construction surface the composition layer uses today.
/// </summary>
internal interface IPathBuilder
{
	void MoveTo(Vector2 point);
	void LineTo(Vector2 point);
	void CubicTo(Vector2 control1, Vector2 control2, Vector2 end);
	void QuadraticTo(Vector2 control, Vector2 end);
	/// <summary>Adds an elliptical arc to <paramref name="end"/> (SVG/D2D-style: radii, x-axis rotation in degrees, large-arc and clockwise flags).</summary>
	void ArcTo(Vector2 radius, float rotationAngle, bool isLargeArc, bool clockwise, Vector2 end);
	void AddRectangle(Rect rect);
	void AddRoundedRectangle(Rect rect, float radiusX, float radiusY);
	/// <summary>Adds a rounded rectangle with (possibly non-uniform) per-corner radii (x, y), ordered TL, TR, BR, BL.</summary>
	void AddRoundedRectangle(Rect rect, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft);
	void AddEllipse(Vector2 center, float radiusX, float radiusY);
	/// <summary>Appends the contours of an existing <paramref name="geometry"/> (used to flatten geometry groups).</summary>
	void AddGeometry(IGeometry geometry);
	void Close();

	/// <summary>The winding rule used to fill the built geometry. Defaults to <see cref="GeometryFillRule.NonZero"/>.</summary>
	GeometryFillRule FillRule { get; set; }

	/// <summary>Produces the geometry and resets the builder for reuse.</summary>
	IGeometry Build();
}
