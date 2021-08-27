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

#pragma warning disable CA1822 // Mark members as static
		public string DeviceFamily => OsName + '.' + AnalyticsInfo.DeviceForm;

		public string DeviceFamilyVersion => Build.VERSION.Release;
#pragma warning restore CA1822 // Mark members as static
	}
}
#endif
