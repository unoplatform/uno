using System;

namespace Windows.System.Profile;

public partial class AnalyticsVersionInfo
{
	partial void Initialize()
	{
		DeviceFamily = GetDeviceFamily(
			OperatingSystem.IsWindows(),
			Environment.OSVersion.Platform,
			AnalyticsInfo.DeviceForm);
	}
}
