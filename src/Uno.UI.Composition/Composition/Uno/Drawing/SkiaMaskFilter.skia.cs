#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IMaskFilter"/> wrapping an <see cref="SKMaskFilter"/>.</summary>
internal sealed class SkiaMaskFilter : IMaskFilter
{
	public SkiaMaskFilter(SKMaskFilter maskFilter) => MaskFilter = maskFilter;

	public SKMaskFilter MaskFilter { get; }

	public void Dispose() => MaskFilter.Dispose();
}
