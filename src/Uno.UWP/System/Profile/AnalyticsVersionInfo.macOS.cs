using AppKit;
using Foundation;

namespace Windows.System.Profile
{
	public partial class AnalyticsVersionInfo
	{
		private const string OsName = "macOS";

		partial void Initialize()
		{
			DeviceFamily = $"{OsName}.{AnalyticsInfo.DeviceForm}";
			DeviceFamilyVersion = UIDevice.CurrentDevice.SystemVersion;
		}

		public string DeviceFamily => OsName + '.' + AnalyticsInfo.DeviceForm;

		public string DeviceFamilyVersion => NSProcessInfo.ProcessInfo.OperatingSystemVersionString;
	}
}
