#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-backed <see cref="IGeometryFactory"/>: mints <c>SKPath</c>-backed geometry
/// (<see cref="SkiaGeometrySource2D"/>) via <see cref="SkiaPathBuilder"/>. This is the geometry the Skia render
/// backend draws on its fast path (no conversion). Registered as the default geometry engine by the Skia backend.
/// </summary>
internal sealed class SkiaGeometryFactory : IGeometryFactory
{
	public IPathBuilder CreatePathBuilder() => new SkiaPathBuilder();

	public IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder() => new SkiaPathBuilder();
}
