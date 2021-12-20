using Android.OS;

namespace Windows.System.Profile;

public partial class AnalyticsVersionInfo
{
	private const string OsName = "Android";

	partial void Initialize()
	{
		DeviceFamily = $"{OsName}.{AnalyticsInfo.DeviceForm}";
		DeviceFamilyVersion = Build.VERSION.Release;
	}
}
