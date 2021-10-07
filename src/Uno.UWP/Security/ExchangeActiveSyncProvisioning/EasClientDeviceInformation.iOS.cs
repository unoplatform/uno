namespace Windows.Security.ExchangeActiveSyncProvisioning
{
    public partial class EasClientDeviceInformation
    {
        partial void Initialize()
        {
			OperatingSystem = "IOS";
			SystemManufacturer = "Apple inc.";
			SystemProductName = Android.OS.Build.Model;
			FriendlyName = Settings.Global.GetString(ContextHelper.Current.ContentResolver, Settings.Global.DeviceName);
		}
	}
}
