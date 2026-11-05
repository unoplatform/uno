#nullable enable

using System;
using Uno.Foundation.Extensibility;

namespace Windows.Globalization;

partial class DeviceClock
{
	private static IDeviceClockExtension? _extension;

	// Without LANG, .NET's culture, which the app language comes from on macOS, reads this same current locale.
	static partial void ReadPlatformLocaleTags(ref string? localeTag, ref string? languageTag)
		=> localeTag = GetExtension()?.GetLocaleTag();

	static partial void ReadPlatform24Hour(ref bool? is24Hour)
		=> is24Hour = IsTwentyFourHourPattern(GetExtension()?.GetFormatFromTemplate("j", null));

	// The locale's patterns for the clock, which the H or h of the template forces.
	static partial void ReadPlatformTimePatterns(string localeTag, bool is24Hour, ref string? shortPattern, ref string? longPattern)
	{
		shortPattern = GetExtension()?.GetFormatFromTemplate(is24Hour ? "Hmm" : "hmm", localeTag);
		longPattern = GetExtension()?.GetFormatFromTemplate(is24Hour ? "Hmmss" : "hmmss", localeTag);
	}

	// Only macOS has one: Windows' setting is in .NET's culture already, and Linux has none.
	private static IDeviceClockExtension? GetExtension()
	{
		if (_extension is null && OperatingSystem.IsMacOS())
		{
			ApiExtensibility.CreateInstance(typeof(DeviceClock), out _extension);
		}

		return _extension;
	}
}
