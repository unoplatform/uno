#if __ANDROID__
using Android.OS;

namespace Windows.System.Profile
{
	public partial class AnalyticsVersionInfo
	{
		private const string OsName = "Android";

		internal AnalyticsVersionInfo()
		{
		}

		public string DeviceFamily => OsName + '.' + AnalyticsInfo.DeviceForm;

		public string DeviceFamilyVersion => Build.VERSION.Release;
	}
}
#endif
