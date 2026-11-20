#nullable enable

using System;
using System.Globalization;
using System.Text;

namespace Windows.Globalization;

// The 12/24-hour setting of Android, iOS and macOS, which .NET leaves out of its cultures. Like a customized time
// format on Windows (which .NET does carry), it applies only to a culture that is the device's own locale.
internal static partial class DeviceClock
{
	// Stands in for the device in tests: its locale, its first preferred language (iOS), and whether it uses a
	// 24-hour clock.
	internal static (string LocaleTag, string? LanguageTag, bool? Is24Hour)? TestDevice { get; set; }

	// Replaces the platform's short and long time patterns (UTS #35) for a locale and a clock in tests, while TestDevice is
	// set.
	internal static Func<string, bool, (string? ShortPattern, string? LongPattern)>? TestTimePatterns { get; set; }

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

	// The culture with its locale's time patterns for the clock the device sets for it, when that isn't the culture's
	// own, as .NET applies a customized time format on Windows. The platform gives the patterns, for the culture's own
	// locale rather than the device's, which on iOS can be another language. Otherwise, or without usable patterns, the
	// culture itself. The combined date and time formats ("f", "F", "g", "G") follow; the name, the AM/PM designators and
	// the calendar stay. The result is read-only when the culture is, as the runtime's own is, and writable when it isn't,
	// as Android's system locale culture is, so the device's setting doesn't decide whether an app can change it.
	internal static CultureInfo FitToClock(CultureInfo culture)
	{
		if (GetClock(culture) is not { } clock || clock == Calendar.GetCultureClock(culture))
		{
			return culture;
		}

		string? shortPattern = null;
		string? longPattern = null;
		ReadTimePatterns(culture.Name, clock == ClockIdentifiers.TwentyFourHour, ref shortPattern, ref longPattern);
		var format = culture.DateTimeFormat;
		if (ToNetTimePattern(shortPattern, format.TimeSeparator, format.DateSeparator) is not { } shortTime
			|| ToNetTimePattern(longPattern, format.TimeSeparator, format.DateSeparator) is not { } longTime)
		{
			return culture;
		}

		var fitted = (CultureInfo)culture.Clone();
		fitted.DateTimeFormat.ShortTimePattern = shortTime;
		fitted.DateTimeFormat.LongTimePattern = longTime;
		if (!culture.IsReadOnly)
		{
			return fitted;
		}

		// CultureInfo.ReadOnly leaves a neutral culture's formats writable.
		fitted.DateTimeFormat = DateTimeFormatInfo.ReadOnly(fitted.DateTimeFormat);
		return CultureInfo.ReadOnly(fitted);
	}

	// A UTS #35 time pattern, as Android and Apple give them ("h:mm a", "HH 'h' mm"), as a .NET custom pattern, or null
	// without an hour or with a field .NET has no equivalent of. The period, and the day period some languages use
	// instead (b, B), become .NET's AM/PM. Literal text stays literal, escaped where .NET would read it otherwise: ":"
	// and "/" stand for the culture's time and date separators in .NET, so they're escaped when those differ. .NET has no
	// hour counted from 0 to 11 (K, which Japanese uses) or from 1 to 24 (k): they become 1 to 12 and 0 to 23, so 12:05 PM
	// shows as 12:05 rather than 0:05, and midnight as 0:05 rather than 24:05.
	internal static string? ToNetTimePattern(string? pattern, string timeSeparator, string dateSeparator)
	{
		if (pattern is null)
		{
			return null;
		}

		var result = new StringBuilder();
		var hasHour = false;
		var inQuote = false;
		for (var i = 0; i < pattern.Length; i++)
		{
			var c = pattern[i];
			if (c == '\'')
			{
				// Two quotes are an apostrophe, inside quoted text too; one opens or closes quoted text.
				if (i + 1 < pattern.Length && pattern[i + 1] == '\'')
				{
					result.Append("\\'");
					i++;
				}
				else
				{
					inQuote = !inQuote;
					result.Append('\'');
				}
			}
			else if (inQuote)
			{
				if (c == '\\')
				{
					result.Append('\\');
				}

				result.Append(c);
			}
			else if (char.IsAsciiLetter(c))
			{
				var count = 1;
				while (i + count < pattern.Length && pattern[i + count] == c)
				{
					count++;
				}

				i += count - 1;
				switch (c)
				{
					case 'a' or 'b' or 'B':
						result.Append("tt");
						break;
					case 'h' or 'K':
						result.Append('h', count);
						hasHour = true;
						break;
					case 'H' or 'k':
						result.Append('H', count);
						hasHour = true;
						break;
					case 'm' or 's':
						result.Append(c, count);
						break;
					default:
						return null;
				}
			}
			else
			{
				if (c is '%' or '\\' or '"' || (c == ':' && timeSeparator != ":") || (c == '/' && dateSeparator != "/"))
				{
					result.Append('\\');
				}

				result.Append(c);
			}
		}

		return hasHour && !inQuote ? result.ToString() : null;
	}

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

	// The platform's short and long time patterns for a locale and a clock, or the tests' while TestDevice is set.
	internal static void ReadTimePatterns(string localeTag, bool is24Hour, ref string? shortPattern, ref string? longPattern)
	{
		if (TestDevice is not null)
		{
			(shortPattern, longPattern) = TestTimePatterns?.Invoke(localeTag, is24Hour) ?? default;
		}
		else
		{
			ReadPlatformTimePatterns(localeTag, is24Hour, ref shortPattern, ref longPattern);
		}
	}

	static partial void ReadPlatformLocaleTags(ref string? localeTag, ref string? languageTag);

	static partial void ReadPlatform24Hour(ref bool? is24Hour);

	// The platform's short and long time patterns (UTS #35) for a locale and a clock.
	static partial void ReadPlatformTimePatterns(string localeTag, bool is24Hour, ref string? shortPattern, ref string? longPattern);
}
