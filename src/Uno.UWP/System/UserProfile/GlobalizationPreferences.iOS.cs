#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using Foundation;

namespace Windows.System.UserProfile
{
	public static partial class GlobalizationPreferences
	{
		public static IReadOnlyList<string> Languages => NSLocale.PreferredLanguages;
	}
}
#endif
