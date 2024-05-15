#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Java.Util;

namespace Windows.System.UserProfile
{
	public static partial class GlobalizationPreferences
	{
		public static IReadOnlyList<string> Languages => new[] { Locale.Default.ToLanguageTag() };
	}
}
#endif
