#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Mints <see cref="IGeometry"/> — the two builders (point-by-point <see cref="IPathBuilder"/> and whole-primitive
/// <see cref="IPrimitiveGeometryBuilder"/>) that produce it, registered together as one seam.
/// </summary>
/// <remarks>
/// Geometry is an independent, render-backend-agnostic seam (like <see cref="IFontProvider"/> and
/// <see cref="IImageEncoderDecoder"/>), <em>not</em> part of the drawing backend: composition builds geometry through the
/// registered factory, and a render backend consumes the neutral <see cref="IGeometry"/> it gets — runtime-checking
/// for the concrete types it knows to take a fast path (e.g. the Skia backend uses an <c>SKPath</c> directly and
/// converts anything else). A backend that has a native geometry representation registers a factory that produces it,
/// so its own draws hit that fast path; a backend without one (WebGPU flattens everything) can use any factory.
/// </remarks>
public interface IGeometryFactory
{
	/// <summary>Creates a point-by-point path builder.</summary>
	IPathBuilder CreatePathBuilder();

	/// <summary>Creates a whole-primitive geometry builder.</summary>
	IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder();
}
