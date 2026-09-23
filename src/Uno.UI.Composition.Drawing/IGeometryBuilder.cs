#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Common surface of the geometry builders (<see cref="IPathBuilder"/>, <see cref="IPrimitiveGeometryBuilder"/>):
/// a winding rule and a terminal <see cref="Build"/>.
/// </summary>
public interface IGeometryBuilder
{
	/// <summary>The winding rule baked into the geometry at <see cref="Build"/> time. Defaults to <see cref="GeometryFillRule.NonZero"/>.</summary>
	GeometryFillRule FillRule { get; set; }

	/// <summary>
	/// Produces the geometry from the accumulated contours, and <em>resets</em> the builder (contours cleared,
	/// <see cref="FillRule"/> back to default) so the same instance can be reused for another geometry.
	/// </summary>
	IGeometry Build();
}
