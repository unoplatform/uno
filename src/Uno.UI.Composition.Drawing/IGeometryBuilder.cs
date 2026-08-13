#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Common surface of the geometry builders (<see cref="IPathBuilder"/>, <see cref="IPrimitiveGeometryBuilder"/>):
/// a winding rule and a terminal <see cref="Build"/>. Split into two specializations so imperative "pen"
/// construction and whole-primitive construction can't be interleaved on one builder.
/// </summary>
public interface IGeometryBuilder
{
	/// <summary>The winding rule baked into the geometry at <see cref="Build"/> time. Defaults to <see cref="GeometryFillRule.NonZero"/>.</summary>
	GeometryFillRule FillRule { get; set; }

	/// <summary>
	/// Produces the geometry from the accumulated contours. This <em>resets</em> the builder — contours are
	/// cleared and <see cref="FillRule"/> returns to its default — so the same instance can immediately be used
	/// to build another, independent geometry. Callers may therefore cache and reuse one builder rather than
	/// allocating a fresh one per geometry.
	/// </summary>
	IGeometry Build();
}
