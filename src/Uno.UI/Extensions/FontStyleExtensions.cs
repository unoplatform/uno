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

		/// <summary>
		/// The OpenType <c>wdth</c> variation axis value (in percent of normal width) for a <see cref="FontStretch"/>,
		/// used to position a variable font on its width axis.
		/// </summary>
		public static float ToVariableFontWidth(this FontStretch stretch) =>
			stretch switch
			{
				FontStretch.UltraCondensed => 50f,
				FontStretch.ExtraCondensed => 62.5f,
				FontStretch.Condensed => 75f,
				FontStretch.SemiCondensed => 87.5f,
				FontStretch.SemiExpanded => 112.5f,
				FontStretch.Expanded => 125f,
				FontStretch.ExtraExpanded => 150f,
				FontStretch.UltraExpanded => 200f,
				_ => 100f, // Normal / Undefined
			};
	}
}
