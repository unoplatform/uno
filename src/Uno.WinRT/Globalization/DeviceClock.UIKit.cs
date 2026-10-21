#nullable enable

using Foundation;

namespace Windows.Globalization;

partial class DeviceClock
{
	// The current locale takes the language of the app's own localization, so an app without Korean resources on
	// a Korean device gets en_KR, while the app language (ApplicationLanguages) is the first preferred language.
	static partial void ReadPlatformLocaleTags(ref string? localeTag, ref string? languageTag)
	{
		localeTag = NSLocale.CurrentLocale.LocaleIdentifier;
		languageTag = NSLocale.PreferredLanguages is { Length: > 0 } languages ? languages[0] : null;
	}

	// The "j" template asks for the locale's own hour, which the 24-Hour Time switch overrides.
	static partial void ReadPlatform24Hour(ref bool? is24Hour)
		=> is24Hour = IsTwentyFourHourPattern(NSDateFormatter.GetDateFormatFromTemplate("j", 0, NSLocale.CurrentLocale));
}
