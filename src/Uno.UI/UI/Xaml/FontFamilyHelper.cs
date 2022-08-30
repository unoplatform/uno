using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
    public class FontFamilyHelper
    {
#if NETFX_CORE
		public static Windows.UI.Xaml.Media.FontFamily Create(string familyName)
		{
			return new Windows.UI.Xaml.Media.FontFamily(familyName);
		}
#elif XAMARIN
		public static string Create(string familyName)
		{
			return familyName;
		}
#endif

#if XAMARIN_IOS
		/// <summary>
		/// This methods removes the font files extensions, typically .otf or .ttf because in iOS
	    /// you need to refer to a FontFamily via its name without extension and in Android you need the extension.
		/// </summary>
		/// <param name="familyName"></param>
		/// <returns></returns>
		public static string RemoveExtension(string familyName)
		{
			return familyName
				.Replace(".otf", string.Empty)
				.Replace(".ttf", string.Empty);
        }
#endif

		public static string RemoveUri(string familyName)
		{
			var slashIndex = familyName.LastIndexOf("/");

			if (slashIndex != -1)
			{
				familyName = familyName.Substring(slashIndex + 1);
			}
			return familyName;
		}

	    public static string RemoveHashFamilyName(string familyName)
	    {
			var hashIndex = familyName.IndexOf("#");

			if (hashIndex != -1)
			{
				familyName = familyName.Substring(0, hashIndex);
			}
			return familyName;
		}

		/// <summary>
		/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
		/// </summary>
		public static void Preload(FontFamily family)
			=> FontFamily.Preload(family);

		/// <summary>
		/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
		/// </summary>
		public static void Preload(string familyName)
			=> Preload(new FontFamily(familyName));
	}
}
