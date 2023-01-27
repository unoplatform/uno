#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Globalization
{
	public partial class Language
	{
		public static string CurrentInputMethodLanguageTag { get; private set; } = "";

		public static bool TrySetInputMethodLanguageTag(string languageTag)
		{
			CurrentInputMethodLanguageTag = languageTag;
			return true;
		}
	}
}
#endif
