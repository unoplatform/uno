using SkiaSharp;

namespace Windows.UI.Text
{
	internal static class FontStyleExtensions
	{
		public static SKFontStyleSlant ToSkiaSlant(this FontStyle style) =>
			style switch
			{
				FontStyle.Italic => SKFontStyleSlant.Italic,
				FontStyle.Normal => SKFontStyleSlant.Upright,
				FontStyle.Oblique => SKFontStyleSlant.Oblique,
				_ => SKFontStyleSlant.Upright
			};
	}
}
