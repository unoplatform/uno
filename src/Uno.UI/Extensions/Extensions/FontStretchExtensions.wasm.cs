using Windows.UI.Text;

namespace Uno.UI.Extensions
{
	internal static class FontStretchExtensions
	{
		public static string ToCssFontStretch(this FontStretch fontStretch)
		{
			return fontStretch switch
			{
				FontStretch.Undefined or FontStretch.UltraCondensed => "ultra-condensed",
				FontStretch.ExtraCondensed => "extra-condensed",
				FontStretch.Condensed => "condensed",
				FontStretch.SemiCondensed => "semi-condensed",
				FontStretch.Normal => "normal",
				FontStretch.SemiExpanded => "semi-expanded",
				FontStretch.Expanded => "expanded",
				FontStretch.ExtraExpanded => "extra-expanded",
				FontStretch.UltraExpanded => "ultra-expanded",
				_ => "", // invalid FontStretch value.
			};
		}

		public static string ToCssFontVariationSettings(this FontStretch fontStretch)
		{
			return fontStretch switch
			{
				FontStretch.Undefined or FontStretch.UltraCondensed => "\"wdth\" 50",
				FontStretch.ExtraCondensed => "\"wdth\" 62.5",
				FontStretch.Condensed => "\"wdth\" 75",
				FontStretch.SemiCondensed => "\"wdth\" 87.5",
				FontStretch.Normal => "\"wdth\" 100",
				FontStretch.SemiExpanded => "\"wdth\" 112.5",
				FontStretch.Expanded => "\"wdth\" 125",
				FontStretch.ExtraExpanded => "\"wdth\" 150",
				FontStretch.UltraExpanded => "\"wdth\" 200",
				_ => "", // invalid FontStretch value.
			};
		}
	}
}
