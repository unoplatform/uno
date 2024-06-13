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

		public static SKFontStyleWidth ToSkiaWidth(this FontStretch stretch) =>
			stretch switch
			{
				FontStretch.Undefined => SKFontStyleWidth.Normal,
				FontStretch.UltraCondensed => SKFontStyleWidth.UltraCondensed,
				FontStretch.ExtraCondensed => SKFontStyleWidth.ExtraCondensed,
				FontStretch.Condensed => SKFontStyleWidth.Condensed,
				FontStretch.SemiCondensed => SKFontStyleWidth.SemiCondensed,
				FontStretch.Normal => SKFontStyleWidth.Normal,
				FontStretch.SemiExpanded => SKFontStyleWidth.SemiExpanded,
				FontStretch.Expanded => SKFontStyleWidth.Expanded,
				FontStretch.ExtraExpanded => SKFontStyleWidth.ExtraExpanded,
				FontStretch.UltraExpanded => SKFontStyleWidth.UltraExpanded,
				_ => SKFontStyleWidth.Normal,
			};
	}
}
