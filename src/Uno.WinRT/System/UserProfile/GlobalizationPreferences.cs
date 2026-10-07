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
	public static string HomeGeographicRegion
	{
		get
		{
#if __SKIA__
			if (OperatingSystem.IsWindows() && GetWinUserDefaultGeoName() is { } geoName)
			{
				return geoName;
			}
#endif

			// TODO Uno: no OS API for the user's home location on this target; the current culture's region stands in.
			try
			{
				var region = global::System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;

				// The invariant culture reports "IV", which is not a region; Windows reports an unknown location as "ZZ".
				return string.IsNullOrEmpty(region) || region == "IV" ? UnknownGeographicRegion : region;
			}
			catch (Exception)
			{
				return UnknownGeographicRegion;
			}
		}
	}

	private const string UnknownGeographicRegion = "ZZ";

	// Uno-specific: the Win32 GetUserDefaultLocaleName used by WinUI controls, i.e. the user's regional-format
	// locale rather than the app language. Returns null where the Win32 call would return 0.
#nullable enable
	internal static string? GetUserDefaultLocaleName()
	{
#if __SKIA__
		if (OperatingSystem.IsWindows())
		{
			return GetWinUserDefaultLocaleName();
		}
#endif

		// TODO Uno: no OS API for the user default locale on this target; the culture the process started with
		// (before ApplicationLanguages.ApplyCulture replaced it) stands in. https://github.com/unoplatform/uno/issues/6908
		var originalCultureName = global::Windows.Globalization.ApplicationLanguages.OriginalCultureName;
		return string.IsNullOrEmpty(originalCultureName) ? null : originalCultureName;
	}
#nullable restore

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

#nullable enable
	private static unsafe string? GetWinUserDefaultLocaleName()
	{
		const int LOCALE_NAME_MAX_LENGTH = 85;
		char* currentLocale = stackalloc char[LOCALE_NAME_MAX_LENGTH];

		// The returned length includes the terminating null.
		var length = NativeMethods.GetUserDefaultLocaleName(currentLocale, LOCALE_NAME_MAX_LENGTH);
		return length != 0 ? new string(currentLocale, 0, length - 1) : null;
	}

	private static unsafe string? GetWinUserDefaultGeoName()
	{
		const int GeoNameMaxLength = 16;
		char* geoName = stackalloc char[GeoNameMaxLength];

		try
		{
			// The returned length includes the terminating null.
			var length = NativeMethods.GetUserDefaultGeoName(geoName, GeoNameMaxLength);
			return length > 1 ? new string(geoName, 0, length - 1) : null;
		}
		catch (EntryPointNotFoundException)
		{
			// GetUserDefaultGeoName needs Windows 10 1709 or later.
			return null;
		}
	}
#nullable restore

	private static unsafe class NativeMethods
	{
		[DllImport("kernel32.dll")]
		public static extern int GetUserDefaultLocaleName(char* lpLocaleName, int cchLocaleName);

		[DllImport("kernel32.dll")]
		public static extern int GetUserDefaultGeoName(char* geoName, int geoNameCount);

		[DllImport("winlangdb.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int EnsureLanguageProfileExists();

		[DllImport("bcp47langs.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetUserLanguages(char Delimiter, out IntPtr UserLanguages);
	}
#endif
}
