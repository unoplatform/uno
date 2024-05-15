#nullable enable

using SkiaSharp;
using Windows.UI;

namespace Uno.UI.Composition.Composition;

/// <summary>
/// Captures the state of drop shadow for a visual.
/// </summary>
internal record ShadowState(float Dx, float Dy, float SigmaX, float SigmaY, Color Color)
{
	private SKPaint? _paint;

	public SKPaint Paint =>
		_paint ??= new SKPaint()
		{
			ImageFilter = SkiaCompat.SKImageFilter_CreateDropShadow(Dx, Dy, SigmaX, SigmaY, Color)
		};
}
