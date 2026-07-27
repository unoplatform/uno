#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

#if __ANDROID__
using Java.Util;
#elif __IOS__ || __TVOS__
using Foundation;
#elif __SKIA__
using Windows.WinRT;
#endif

namespace Windows.System.UserProfile;

public static partial class GlobalizationPreferences
{

#if __ANDROID__ || __IOS__ || __TVOS__ || __WASM__ || __SKIA__
	public static IReadOnlyList<string> Languages =>
#if __ANDROID__
		new[] { Locale.Default.ToLanguageTag() };
#elif __IOS__ || __TVOS__
		NSLocale.PreferredLanguages;
#elif __WASM__
		GetCurrentCultureLanguages();
#elif __SKIA__
		OperatingSystem.IsWindows() ? GetWinUserLanguageList() : GetCurrentCultureLanguages();
#endif
#endif

#if __ANDROID__ || __IOS__ || __TVOS__ || __WASM__ || __SKIA__
	public static string HomeGeographicRegion
	{
		get
		{
#if __ANDROID__
			return NormalizeRegionOrFallback(Locale.Default.Country);
#elif __IOS__ || __TVOS__
			return NormalizeRegionOrFallback(NSLocale.CurrentLocale.CountryCode);
#elif __SKIA__
			if (OperatingSystem.IsWindows() && TryGetWinUserRegion(out var region))
			{
				return region;
			}

			return NormalizeRegionOrFallback(null);
#elif __WASM__
			return NormalizeRegionOrFallback(null);
#endif
		}
	}
#endif

#if __WASM__ || __SKIA__
	private static string[] GetCurrentCultureLanguages() =>
		global::System.Globalization.CultureInfo.CurrentUICulture.Name is { Length: > 0 } language
			? [language]
			: ["en-US"];
#endif

#if __ANDROID__ || __IOS__ || __TVOS__ || __WASM__ || __SKIA__
	private static string NormalizeRegionOrFallback(string? region)
	{
		if (TryNormalizeRegion(region, out var normalizedRegion) ||
			TryNormalizeRegion(GetCurrentCultureRegion(), out normalizedRegion))
		{
			return normalizedRegion;
		}

		return "US";
	}

	private static string? GetCurrentCultureRegion()
	{
		try
		{
			return new global::System.Globalization.RegionInfo(
				global::System.Globalization.CultureInfo.CurrentCulture.Name).TwoLetterISORegionName;
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	private static bool TryNormalizeRegion(string? region, out string normalizedRegion)
	{
		normalizedRegion = string.Empty;
		if (string.IsNullOrEmpty(region))
		{
			return false;
		}

		var candidate = region.ToUpperInvariant();
		if ((candidate.Length == 2 &&
			candidate[0] is >= 'A' and <= 'Z' &&
			candidate[1] is >= 'A' and <= 'Z') ||
			(candidate.Length == 3 &&
			candidate[0] is >= '0' and <= '9' &&
			candidate[1] is >= '0' and <= '9' &&
			candidate[2] is >= '0' and <= '9'))
		{
			normalizedRegion = candidate;
			return true;
		}

		return false;
	}
#endif

#if __SKIA__
	private static bool TryGetWinUserRegion(out string region)
	{
		const int GeoNameLength = 85;
		var builder = new StringBuilder(GeoNameLength);
		if (NativeMethods.GetUserDefaultGeoName(builder, builder.Capacity) > 0)
		{
			return TryNormalizeRegion(builder.ToString(), out region);
		}

		region = string.Empty;
		return false;
	}

	private static string[] GetWinUserLanguageList()
	{
		if (NativeMethods.EnsureLanguageProfileExists() >= 0)
		{
			const char Delimiter = ';';
			if (NativeMethods.GetUserLanguages(Delimiter, out var handle) >= 0)
			{
				try
				{
					var languages = MarshalString.FromAbi(handle).Split(Delimiter);
					return languages.Length > 0 ? languages : GetCurrentCultureLanguages();
				}
				finally
				{
					MarshalString.DisposeAbi(handle);
				}
			}
		}

		return GetCurrentCultureLanguages();
	}

	private static class NativeMethods
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetUserDefaultGeoName(StringBuilder geoName, int geoNameCount);

		[DllImport("winlangdb.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int EnsureLanguageProfileExists();

		[DllImport("bcp47langs.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetUserLanguages(char Delimiter, out IntPtr UserLanguages);
	}
#endif
}
