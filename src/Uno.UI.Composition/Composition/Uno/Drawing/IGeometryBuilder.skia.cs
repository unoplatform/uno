#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Common surface of the geometry builders (<see cref="IPathBuilder"/>, <see cref="IPrimitiveGeometryBuilder"/>):
/// a winding rule and a terminal <see cref="Build"/>. Split into two specializations so imperative "pen"
/// construction and whole-primitive construction can't be interleaved on one builder.
/// </summary>
internal interface IGeometryBuilder
{
	/// <summary>The winding rule baked into the geometry at <see cref="Build"/> time. Defaults to <see cref="GeometryFillRule.NonZero"/>.</summary>
	GeometryFillRule FillRule { get; set; }

	/// <summary>Produces the geometry and resets the builder for reuse.</summary>
	IGeometry Build();
}
