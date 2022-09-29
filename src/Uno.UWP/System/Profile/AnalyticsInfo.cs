using System;

namespace Windows.System.Profile;

/// <summary>
/// Provides version information about the device family.
/// </summary>
public static partial class AnalyticsInfo
{
	/// <summary>
	/// Initializing AnalyticsVersionInfo lazily as it accesses the DeviceForm property, which could otherwise
	/// happen while the static AnalyticsInfo class is still initializing - causing unpredicatable behavior.
	/// </summary>
	private static readonly Lazy<AnalyticsVersionInfo> _analyticsVersionInfo = new(() => new AnalyticsVersionInfo());

	/// <summary>
	/// Gets the device form factor running the OS. For example,
	/// the app could be running on a phone, tablet, desktop, and so on.
	/// </summary>
	public static string DeviceForm => GetDeviceForm().ToString();

	/// <summary>
	/// Gets version info about the device family.
	/// </summary>
	public static AnalyticsVersionInfo VersionInfo => _analyticsVersionInfo.Value;
}
