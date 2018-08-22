#if __ANDROID__ || __IOS__
using System.Globalization;

namespace Windows.Globalization
{
	public partial class Language 
	{
		public string DisplayName { get; private set; }

		public string LanguageTag { get; private set; }

		public string NativeName { get; private set; }

		public Language(string languageTag) 
		{
			var cultureInfo = new CultureInfo(languageTag, false);

			LanguageTag = languageTag;
			DisplayName = cultureInfo.DisplayName;
			NativeName = cultureInfo.NativeName;
		}
	}
}
#endif
