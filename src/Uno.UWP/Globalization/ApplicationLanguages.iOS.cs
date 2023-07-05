#nullable disable

using System.Collections.Generic;
using System.Linq;

namespace Windows.Globalization
{
	public static partial class ApplicationLanguages
	{
		public static IReadOnlyList<string> ManifestLanguages { get; } = GetManifestLanguages();

		private static string[] GetManifestLanguages()
		{
			var manifestLanguages = global::Foundation.NSLocale.PreferredLanguages
				.Concat(global::Foundation.NSBundle.MainBundle.PreferredLocalizations)
				.Concat(global::Foundation.NSBundle.MainBundle.Localizations)
				.Distinct()
				.ToArray();

			return manifestLanguages;
		}
	}
}
