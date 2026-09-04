using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

#if __ANDROID__
using Java.Util;
#elif __IOS__
using Foundation;
#elif __SKIA__
using Windows.WinRT;
#endif

namespace Windows.System.UserProfile;

public static partial class GlobalizationPreferences
{

#if __ANDROID__ || __IOS__ || __SKIA__
	public static IReadOnlyList<string> Languages =>
#if __ANDROID__
		new[] { Locale.Default.ToLanguageTag() };
#elif __IOS__
		NSLocale.PreferredLanguages;
#elif __SKIA__
		OperatingSystem.IsWindows() ? GetWinUserLanguageList() : Array.Empty<string>();
#endif
#endif

#if __SKIA__
	private static string[] GetWinUserLanguageList()
	{
		if (NativeMethods.EnsureLanguageProfileExists() >= 0)
		{
			const char Delimiter = ';';
			if (NativeMethods.GetUserLanguages(Delimiter, out var handle) >= 0)
			{
				var languages = MarshalString.FromAbi(handle).Split(Delimiter);
				MarshalString.DisposeAbi(handle);

				return languages;
			}
		}

		return Array.Empty<string>();
	}

	private static class NativeMethods
	{
		[DllImport("winlangdb.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int EnsureLanguageProfileExists();

		[DllImport("bcp47langs.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetUserLanguages(char Delimiter, out IntPtr UserLanguages);
	}
#endif
}
