#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Mints <see cref="IGeometry"/> via the two builders (point-by-point <see cref="IPathBuilder"/> and whole-primitive
/// <see cref="IPrimitiveGeometryBuilder"/>), registered together as one render-backend-agnostic seam.
/// </summary>
/// <remarks>
/// A backend with a native geometry representation registers a factory that produces it, so its own draws hit a
/// fast path (e.g. the Skia backend recognizes an <c>SKPath</c>); a backend without one can consume any factory's
/// neutral <see cref="IGeometry"/>.
/// </remarks>
public interface IGeometryFactory
{
	/// <summary>Creates a point-by-point path builder.</summary>
	IPathBuilder CreatePathBuilder();

	/// <summary>Creates a whole-primitive geometry builder.</summary>
	IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder();
}
