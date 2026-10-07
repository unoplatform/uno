#nullable enable

using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.Skia;

/// <summary>
/// Adds the Svg.Skia renderer to <see cref="SkiaBackend"/> when this add-in is referenced. The host builder finds it by
/// name and makes it the default <see cref="ISvgRenderer"/>, over the managed engine; a head whose image is trimmed or
/// AOT-compiled cannot resolve it that way, so those register it explicitly
/// (<c>builder.SvgRenderer(SkiaBackend.CreateSvgRenderer())</c>). An explicit registration wins over both.
/// </summary>
public static class SkiaBackendSvgExtensions
{
	extension(SkiaBackend)
	{
		/// <summary>Creates the Svg.Skia <see cref="ISvgRenderer"/>.</summary>
		public static ISvgRenderer CreateSvgRenderer() => new global::Uno.UI.Svg.SkiaSvgRenderer();
	}
}
