#nullable enable

using Uno.UI.Composition.Drawing;

namespace Uno.UI.Svg;

/// <summary>
/// Reflective bootstrap for the optional Svg.Skia-backed SVG renderer, invoked by name from the host builder (no
/// compile-time dependency either way). When referenced this add-in becomes the default <see cref="ISvgRenderer"/>,
/// else the framework falls back to the managed engine; an explicit host-builder registration wins over both.
/// </summary>
public static class SvgBackend
{
	// Returns a public neutral-seam instance the framework registers, so no framework internal or IVT is needed.
	// Kept internal: reflection binds it with NonPublic.
	internal static ISvgRenderer CreateSvgRenderer() => new SkiaSvgRenderer();
}
