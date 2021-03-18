#if __MACOS__
using AppKit;
using Foundation;

namespace Windows.System.Profile
{
	public partial class AnalyticsVersionInfo
	{
		private const string OsName = "macOS";

		internal AnalyticsVersionInfo()
		{
		}

		public string DeviceFamily => OsName + '.' + AnalyticsInfo.DeviceForm;

		public string DeviceFamilyVersion => NSProcessInfo.ProcessInfo.OperatingSystemVersionString;
	}
}
#endif
