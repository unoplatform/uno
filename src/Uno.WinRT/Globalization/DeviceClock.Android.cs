#nullable enable

using Android.Content;
using Android.Text.Format;
using Uno.UI;

namespace Windows.Globalization;

partial class DeviceClock
{
	// The locale Android formats the app in, read as ApplicationLanguages reads the app language: the first system
	// locale, or the app's own language if the user set one.
	static partial void ReadPlatformLocaleTags(ref string? localeTag, ref string? languageTag)
		=> localeTag = GetContext().Resources?.Configuration?.Locales.Get(0)?.ToLanguageTag();

	// The "Use 24-hour format" switch or, under "Use locale default", that locale's clock.
	static partial void ReadPlatform24Hour(ref bool? is24Hour)
		=> is24Hour = DateFormat.Is24HourFormat(GetContext());

	// The locale's patterns for the clock: the short one with the skeletons of Android's own DateFormat.getTimeFormat,
	// the long one with ICU's medium time skeleton.
	static partial void ReadPlatformTimePatterns(string localeTag, bool is24Hour, ref string? shortPattern, ref string? longPattern)
	{
		using var locale = Java.Util.Locale.ForLanguageTag(localeTag);
		shortPattern = DateFormat.GetBestDateTimePattern(locale, is24Hour ? "Hm" : "hm");
		longPattern = DateFormat.GetBestDateTimePattern(locale, is24Hour ? "Hmmss" : "hmmss");
	}

	// Below API 33, AppCompat applies an app language to activities only.
	private static Context GetContext()
		=> ContextHelper.TryGetCurrent(out var context) && context is not null ? context : ContextHelper.ApplicationContext;
}
