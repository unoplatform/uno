namespace Windows.System.Profile
{
	public static partial class AnalyticsInfo
	{
#if __ANDROID__ || __IOS__ || __WASM__ || __MACOS__
		public static string DeviceForm => GetDeviceForm().ToString();

		public static AnalyticsVersionInfo VersionInfo { get; } = new AnalyticsVersionInfo();
#endif
	}
}
