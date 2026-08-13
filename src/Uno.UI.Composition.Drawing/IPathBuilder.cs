#nullable enable

using System.Numerics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Imperative "pen" builder: a single contour advanced by move/line/curve/arc verbs, terminated by
/// <see cref="IGeometryBuilder.Build"/>. This is the surface used by path/stream geometries and the
/// D2D command stream. For whole-shape construction use <see cref="IPrimitiveGeometryBuilder"/> instead.
/// </summary>
public interface IPathBuilder : IGeometryBuilder
{
	void MoveTo(Vector2 point);
	void LineTo(Vector2 point);
	void CubicTo(Vector2 control1, Vector2 control2, Vector2 end);
	void QuadraticTo(Vector2 control, Vector2 end);
	/// <summary>Adds an elliptical arc to <paramref name="end"/> (SVG/D2D-style: radii, x-axis rotation in degrees, large-arc and clockwise flags).</summary>
	void ArcTo(Vector2 radius, float rotationAngle, bool isLargeArc, bool clockwise, Vector2 end);
	void Close();
}
