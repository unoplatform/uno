#nullable enable

using System;
using System.Globalization;

namespace Windows.Globalization;

// The 12/24-hour setting of Android, iOS and macOS, which .NET leaves out of its cultures. Like a customized time
// format on Windows (which .NET does carry), it applies only to a culture that is the device's own locale.
internal static partial class DeviceClock
{
	// Stands in for the device in tests: its locale, its first preferred language (iOS), and whether it uses a
	// 24-hour clock.
	internal static (string LocaleTag, string? LanguageTag, bool? Is24Hour)? TestDevice { get; set; }

	// The clock the device sets for the culture, or null when it sets none.
	internal static string? GetClock(CultureInfo culture)
	{
		string? localeTag = null;
		string? languageTag = null;
		ReadLocaleTags(ref localeTag, ref languageTag);

		if (!IsLocale(culture.Name, localeTag) && !IsLocale(culture.Name, languageTag))
		{
			return null;
		}

		return ToClock(Read24Hour());
	}

	// The device setting or, without one (Windows, Linux, WebAssembly), the clock of the runtime's culture, which on
	// Windows carries the customizations of the regional format.
	internal static string GetUserClock()
		=> ToClock(Read24Hour()) ?? Calendar.GetDefaultClock(CultureInfo.InstalledUICulture);

	// Whether an ICU date format (Apple's) uses a 24-hour clock, or null without an hour.
	internal static bool? IsTwentyFourHourPattern(string? pattern)
	{
		if (pattern is null)
		{
			return null;
		}

		var quoted = false;
		foreach (var c in pattern)
		{
			if (c == '\'')
			{
				quoted = !quoted;
			}
			else if (!quoted && c is 'H' or 'k')
			{
				return true;
			}
			else if (!quoted && c is 'h' or 'K')
			{
				return false;
			}
		}

		return null;
	}

	// Whether a culture is this device locale: the same language, region and variants, without conflicting scripts
	// (.NET names Apple's zh-Hans_CN zh-CN). Extensions (Android 14 regional preferences, "@rg=") don't count.
	private static bool IsLocale(string cultureName, string? tag)
	{
		if (tag is null)
		{
			return false;
		}

		var culture = Parse(cultureName);
		var device = Parse(tag);

		return string.Equals(culture.Language, device.Language, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(culture.Region, device.Region, StringComparison.OrdinalIgnoreCase)
			&& (culture.Script.Length == 0 || device.Script.Length == 0 || string.Equals(culture.Script, device.Script, StringComparison.OrdinalIgnoreCase));
	}

	// A BCP-47 tag or an Apple locale identifier, as its language, its script, and its region with any variants, up to
	// its extensions.
	private static (string Language, string Script, string Region) Parse(string tag)
	{
		var at = tag.IndexOf('@');
		var subtags = (at >= 0 ? tag.Substring(0, at) : tag).Split('-', '_');

		var script = "";
		var region = "";
		for (var i = 1; i < subtags.Length && subtags[i].Length > 1; i++)
		{
			if (i == 1 && subtags[i].Length == 4 && char.IsLetter(subtags[i][0]))
			{
				script = subtags[i];
			}
			else
			{
				region += "-" + subtags[i];
			}
		}

		return (subtags[0], script, region);
	}

	private static string? ToClock(bool? is24Hour)
		=> is24Hour switch
		{
			true => ClockIdentifiers.TwentyFourHour,
			false => ClockIdentifiers.TwelveHour,
			null => null,
		};

	private static void ReadLocaleTags(ref string? localeTag, ref string? languageTag)
	{
		if (TestDevice is { } device)
		{
			localeTag = device.LocaleTag;
			languageTag = device.LanguageTag;
		}
		else
		{
			ReadPlatformLocaleTags(ref localeTag, ref languageTag);
		}
	}

	private static bool? Read24Hour()
	{
		if (TestDevice is { } device)
		{
			return device.Is24Hour;
		}

		bool? is24Hour = null;
		ReadPlatform24Hour(ref is24Hour);
		return is24Hour;
	}

	static partial void ReadPlatformLocaleTags(ref string? localeTag, ref string? languageTag);

	static partial void ReadPlatform24Hour(ref bool? is24Hour);
}
