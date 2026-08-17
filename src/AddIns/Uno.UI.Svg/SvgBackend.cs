#nullable enable

using Uno.UI.Composition.Drawing;

namespace Uno.UI.Svg;

/// <summary>
/// Reflective bootstrap for the optional Svg.Skia-backed SVG renderer, invoked by name from
/// <c>DrawingBackendFallback</c> in the neutral Drawing assembly (neither side has a compile-time dependency on the
/// other). When this add-in is referenced it becomes the default <see cref="ISvgRenderer"/>; without it the framework
/// falls back to the managed engine. An explicit host-builder registration always wins over both.
/// </summary>
public static class SvgBackend
{
	// Returns a public neutral-seam instance; the framework registers it via its own internal RegisterDefault, so this
	// add-in reaches no framework internal and needs no InternalsVisibleTo. Internal: reflection binds it with NonPublic.
	internal static ISvgRenderer CreateSvgRenderer() => new SkiaSvgRenderer();
}
