#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IColorFilter"/> wrapping an <see cref="SKColorFilter"/>.</summary>
internal sealed class SkiaColorFilter : DrawingResource, IColorFilter
{
	public SkiaColorFilter(SKColorFilter colorFilter) => ColorFilter = colorFilter;

	public SKColorFilter ColorFilter { get; }

	protected override void Free() => ColorFilter.Dispose();
}
