#nullable enable

namespace Windows.Globalization;

internal interface IDeviceClockExtension
{
	/// <summary>
	/// The identifier of the device's locale.
	/// </summary>
	string? GetLocaleTag();

	/// <summary>
	/// The device's ICU date format for its preferred hour (the "j" template), which carries its 12/24-hour setting.
	/// </summary>
	string? GetHourFormat();
}
