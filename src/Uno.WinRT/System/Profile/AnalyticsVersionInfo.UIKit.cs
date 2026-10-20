using System;
using System.Globalization;
using UIKit;
using Windows.System.Profile.Internal;

namespace Windows.System.Profile;

public partial class AnalyticsVersionInfo
{
	private const string OsName = "iOS";

	partial void Initialize()
	{
		DeviceFamily = $"{OsName}.{AnalyticsInfo.DeviceForm}";

		if (Version.TryParse(UIDevice.CurrentDevice.SystemVersion, out var version))
		{
			DeviceFamilyVersion = VersionHelpers.ToLong(version).ToString(CultureInfo.InvariantCulture);
		}
	}
}
