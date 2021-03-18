#if __IOS__
using UIKit;

namespace Windows.System.Profile
{
	public partial class AnalyticsVersionInfo
	{
		private const string OsName = "iOS";

		internal AnalyticsVersionInfo()
		{
		}

		public string DeviceFamily => OsName + '.' + AnalyticsInfo.DeviceForm;

		public string DeviceFamilyVersion => UIDevice.CurrentDevice.SystemVersion;
	}
}
#endif
