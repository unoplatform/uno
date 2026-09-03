using SkiaSharp;

namespace Windows.UI.Text
{
	internal static class FontWeightExtensions
	{
		public static SKFontStyleWeight ToSkiaWeight(this FontWeight fontWeight)
		{
			// Uno weight values are using the same system,
			// so we can convert directly to Skia system
			// without need for a mapping.
			return (SKFontStyleWeight)fontWeight.Weight;
		}
	}
}
