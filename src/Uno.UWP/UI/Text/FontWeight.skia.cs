using SkiaSharp;

namespace Windows.UI.Text
{
	partial struct FontWeight
	{
		public SKFontStyleWeight ToSkiaWeight()
		{
			// Uno weight values are using the same system,
			// so we can convert directly to Skia system
			// without need for a mapping.
			return (SKFontStyleWeight)this.Weight;
		}
	}
}
