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
	void AddRectangle(Rect rect);
	void AddRoundedRectangle(Rect rect, float radiusX, float radiusY);
	void AddEllipse(Vector2 center, float radiusX, float radiusY);
	void Close();

	/// <summary>Produces the geometry and resets the builder for reuse.</summary>
	IGeometry Build();
}
