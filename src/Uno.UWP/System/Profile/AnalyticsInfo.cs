#if __ANDROID__ || __IOS__ || __WASM__
namespace Windows.System.Profile
{
	public static partial class AnalyticsInfo
	{
		public static string DeviceForm => GetDeviceForm().ToString();

		public static AnalyticsVersionInfo VersionInfo { get; } = new AnalyticsVersionInfo();
	}
}
#endif
