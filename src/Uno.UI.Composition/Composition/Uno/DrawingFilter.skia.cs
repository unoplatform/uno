#nullable enable

using System;
using System.Linq;
using SkiaSharp;
using Uno.Extensions;

namespace Uno.UI.Composition;

/// <summary>
/// A holding struct for effects that should be applied by children instead of visual itself, usually only on actual drawing instead of container visual.
/// </summary>
/// <param name="Opacity">
///		The opacity to apply to children.
///		This should be applied only on drawing and not the container to avoid fade-ou of child containers.
/// </param>
internal readonly record struct DrawingFilters(float Opacity)
{
	public static DrawingFilters Default { get; } = new(1.0f);

	public SKColorFilter? OpacityColorFilter => Opacity is 1.0f
		? null
		: _opacityToColorFilter[(byte)(0xFF * Opacity)] ??= SKColorFilter.CreateBlendMode(new SKColor(0xFF, 0xFF, 0xFF, (byte)(0xFF * Opacity)), SKBlendMode.Modulate);

	private static readonly SKColorFilter?[] _opacityToColorFilter = new SKColorFilter?[256];
}
