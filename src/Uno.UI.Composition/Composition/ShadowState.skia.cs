#nullable enable

using Microsoft.UI.Composition;
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
			// Equivalent (I think) to SKImageFilter.CreateDropShadow(Dx, Dy, SigmaX, SigmaY, Color.ToSKColor()) but much much faster
			ImageFilter = SKImageFilter.CreateMerge(SKImageFilter.CreateOffset(Dx, Dy, SKImageFilter.CreateCompose(SKImageFilter.CreateBlur(SigmaX, SigmaY), SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(Color.ToSKColor(), SKBlendMode.Modulate)))), SKImageFilter.CreateOffset(0, 0))
		};
}
