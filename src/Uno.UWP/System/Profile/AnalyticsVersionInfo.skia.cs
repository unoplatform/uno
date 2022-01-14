using System;

namespace Windows.System.Profile;

public partial class AnalyticsVersionInfo
{
	partial void Initialize()
	{
		DeviceFamily = $"{Environment.OSVersion.Platform}.{AnalyticsInfo.DeviceForm}";
	}
}
