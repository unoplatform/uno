namespace Windows.System.Profile
{
	public partial class AnalyticsInfo
	{
		public static string DeviceForm => GetDeviceForm().ToString();

		public static AnalyticsVersionInfo VersionInfo { get; } = new AnalyticsVersionInfo();
	}
}
