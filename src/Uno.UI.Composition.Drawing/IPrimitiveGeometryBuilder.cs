#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Whole-primitive builder: each <c>Add*</c> appends an independent, self-closed sub-contour, terminated by
/// <see cref="IGeometryBuilder.Build"/>. Sub-contours share a single <see cref="IGeometryBuilder.FillRule"/>
/// (so overlaps resolve as even-odd/non-zero across the whole set). For point-by-point construction use
/// <see cref="IPathBuilder"/> instead.
/// </summary>
public interface IPrimitiveGeometryBuilder : IGeometryBuilder
{
	void AddRectangle(Rect rect);
	void AddRoundedRectangle(Rect rect, float radiusX, float radiusY);
	/// <summary>Adds a rounded rectangle with (possibly non-uniform) per-corner radii (x, y), ordered TL, TR, BR, BL.</summary>
	void AddRoundedRectangle(Rect rect, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft);
	void AddEllipse(Vector2 center, float radiusX, float radiusY);
	/// <summary>Appends the contours of an existing <paramref name="geometry"/> (used to flatten geometry groups).</summary>
	void AddGeometry(IGeometry geometry);
}
