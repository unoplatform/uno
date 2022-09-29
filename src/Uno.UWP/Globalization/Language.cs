#nullable disable

using System.Globalization;

namespace Windows.Globalization
{
	public partial class Language 
	{		
		public Language(string languageTag) 
		{
			var cultureInfo = new CultureInfo(languageTag, false);

			LanguageTag = languageTag;
			DisplayName = cultureInfo.DisplayName;
			NativeName = cultureInfo.NativeName;
		}

		public string DisplayName { get; private set; }

		public string LanguageTag { get; private set; }

		public string NativeName { get; private set; }
	}
}
