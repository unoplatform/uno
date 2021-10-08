namespace Windows.System.Profile
{
	public partial class AnalyticsVersionInfo
	{
		private const string OsName = "iOS";

		partial void Initialize()
		{
			DeviceFamily = $"{OsName}.{AnalyticsInfo.DeviceForm}";
			DeviceFamilyVersion = UIDevice.CurrentDevice.SystemVersion;
		}
	}
}
