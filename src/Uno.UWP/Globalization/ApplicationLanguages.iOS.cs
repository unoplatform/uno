using System.Collections.Generic;
using System.Linq;
using Foundation;

namespace Windows.Globalization
{
	public static partial class ApplicationLanguages
	{
		public static IReadOnlyList<string> ManifestLanguages { get; } = NSBundle.MainBundle.PreferredLocalizations.Concat(NSBundle.MainBundle.Localizations).Distinct().ToArray();
	}
}
