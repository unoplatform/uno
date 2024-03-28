using HarfBuzzSharp;
using SkiaSharp;

namespace Windows.UI.Xaml.Documents.TextFormatting;

internal record FontDetails(SKFont SKFont, float SKFontSize, float SKFontScaleX, SKFontMetrics SKFontMetrics, SKTypeface SKTypeface, Font Font, Face Face)
{
	internal float LineHeight
	{
		get
		{
			var metrics = SKFontMetrics;
			return metrics.Descent - metrics.Ascent;
		}
	}
}
