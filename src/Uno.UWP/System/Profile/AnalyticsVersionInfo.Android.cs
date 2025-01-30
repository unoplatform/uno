using System;
using System.Globalization;
using Android.OS;
using Windows.System.Profile.Internal;

namespace Windows.System.Profile;

public partial class AnalyticsVersionInfo
{
	private const string OsName = "Android";

	partial void Initialize()
	{
		DeviceFamily = $"{OsName}.{AnalyticsInfo.DeviceForm}";
		var versionString = Build.VERSION.Release;
		if (int.TryParse(versionString, CultureInfo.InvariantCulture, out var intVersion))
		{
			versionString = $"{intVersion}.0.0.0";
		}
		if (Version.TryParse(versionString, out var version))
		{
			DeviceFamilyVersion = VersionHelpers.ToLong(version).ToString(CultureInfo.InvariantCulture);
		}
	}
}
