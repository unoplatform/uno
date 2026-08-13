#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IEffectFilter"/> wrapping an <see cref="SKImageFilter"/>.</summary>
internal sealed class SkiaEffectFilter : IEffectFilter
{
	internal SKImageFilter Filter { get; }

	internal SkiaEffectFilter(SKImageFilter filter) => Filter = filter;

	public void Dispose() => Filter.Dispose();
}
