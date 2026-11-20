#nullable enable

namespace Windows.Globalization;

internal interface IDeviceClockExtension
{
	/// <summary>
	/// The identifier of the device's locale.
	/// </summary>
	string? GetLocaleTag();

	/// <summary>
	/// The ICU date format for a template in a locale, or in the device's own locale when null. The device's own format
	/// for the "j" template, its preferred hour, carries its 12/24-hour setting.
	/// </summary>
	string? GetFormatFromTemplate(string template, string? localeTag);
}
