using System;

namespace Windows.System.Profile;

public partial class AnalyticsVersionInfo
{
	partial void Initialize()
	{
		var osName = OperatingSystem.IsWindows() ? "Windows" : Environment.OSVersion.Platform.ToString();
		DeviceFamily = $"{osName}.{AnalyticsInfo.DeviceForm}";
	}
}
