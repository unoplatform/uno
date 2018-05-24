#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using Foundation;

namespace Windows.Globalization
{
	public static partial class ApplicationLanguages
	{
		public static IReadOnlyList<string> Languages => NSBundle.MainBundle.PreferredLocalizations;

		public static IReadOnlyList<string> ManifestLanguages => NSBundle.MainBundle.Localizations;
	}
}
#endif