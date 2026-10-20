#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-backed <see cref="IGeometryFactory"/>: mints <c>SKPath</c>-backed geometry
/// (<see cref="SkiaGeometrySource2D"/>) via <see cref="SkiaPathBuilder"/>, which the Skia backend draws with no
/// conversion. Registered as the default geometry engine.
/// </summary>
internal sealed class SkiaGeometryFactory : IGeometryFactory
{
	public IPathBuilder CreatePathBuilder() => new SkiaPathBuilder();

	public IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder() => new SkiaPathBuilder();
}
