namespace Windows.System.Profile
{
	public partial class AnalyticsInfo
	{
#if __ANDROID__ || __IOS__
		public static string DeviceForm
		{
			get
			{
				return VersionInfo.DeviceFamily;
			}
		}

		public static AnalyticsVersionInfo VersionInfo
		{
			get;
		} = new AnalyticsVersionInfo();
#endif
	}
}
