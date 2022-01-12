using System;
using Foundation;
using Windows.System.Profile.Internal;

namespace Windows.System.Profile;

public partial class AnalyticsVersionInfo
{
	private const string OsName = "macOS";

	partial void Initialize()
	{
		DeviceFamily = $"{OsName}.{AnalyticsInfo.DeviceForm}";

		if (Version.TryParse(NSProcessInfo.ProcessInfo.OperatingSystemVersionString, out var version))
		{
			DeviceFamilyVersion = VersionHelpers.ToLong(version).ToString();
		}
	}
}
