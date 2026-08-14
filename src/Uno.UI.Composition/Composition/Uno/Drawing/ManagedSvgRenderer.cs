#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The framework's default <see cref="ISvgRenderer"/>: the SkiaSharp-free managed SVG engine
/// (<see cref="ManagedSvg"/>). Registered as <see cref="SvgRenderer.Current"/> unless the app supplies its own.
/// </summary>
public sealed class ManagedSvgRenderer : ISvgRenderer
{
	public ISvgDocument? Parse(byte[] svg) => ManagedSvg.TryParse(svg, out var document) ? document : null;
}
