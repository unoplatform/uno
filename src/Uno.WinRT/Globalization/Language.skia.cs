using System.Globalization;

namespace Windows.Globalization
{
	public partial class Language
	{
		public static string CurrentInputMethodLanguageTag => CultureInfo.CurrentCulture.Name;

		public static bool TrySetInputMethodLanguageTag(string languageTag) => false;
	}
}
