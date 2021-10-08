namespace Windows.System.Profile
{
	/// <summary>
	/// Provides version information about the device family.
	/// </summary>
	public static partial class AnalyticsInfo
	{
		/// <summary>
		/// Gets the device form factor running the OS. For example,
		/// the app could be running on a phone, tablet, desktop, and so on.
		/// </summary>
		public static string DeviceForm => GetDeviceForm().ToString();

		/// <summary>
		/// Gets version info about the device family.
		/// </summary>
		public static AnalyticsVersionInfo VersionInfo { get; } = new AnalyticsVersionInfo();
	}
}
