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

	// Note: This SKColorFilter might be created more than once since this Filter is a struct.
	//       However since this Filter is copied (pushed to the stack) only when something changes, it should still catch most cases.
	public SKColorFilter? OpacityColorFilter => Opacity is 1.0f
		? null
		: _opacityToColorFilter.Invoke((byte)(255 * Opacity));

	// only 255 possible values
	private static readonly Func<byte, SKColorFilter> _opacityToColorFilter = FuncMemoizeExtensions.AsMemoized((byte opacity)
		=>
	{
		Console.WriteLine($"Ramez created a new color filter for opacity {opacity}");
		return SKColorFilter.CreateBlendMode(new SKColor(0xFF, 0xFF, 0xFF, opacity), SKBlendMode.Modulate);
	});
}
