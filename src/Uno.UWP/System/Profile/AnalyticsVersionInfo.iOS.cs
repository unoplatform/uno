#if __IOS__
using UIKit;

namespace Windows.System.Profile
{
	public partial class AnalyticsVersionInfo
	{
		private const string OsManufacturer = "Apple";

		internal AnalyticsVersionInfo()
		{
		}

		public string DeviceFamily => $"{OsManufacturer}.{AnalyticsInfo.DeviceForm}";

		public string DeviceFamilyVersion => UIDevice.CurrentDevice.SystemVersion;
	}
}
#endif
