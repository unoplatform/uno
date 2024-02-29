using Uno.UI;
using Settings = Android.Provider.Settings;

namespace Windows.Security.ExchangeActiveSyncProvisioning
{
	public partial class EasClientDeviceInformation
	{
		partial void Initialize()
		{
			OperatingSystem = "ANDROID";
			SystemManufacturer = Android.OS.Build.Manufacturer!;
			SystemProductName = Android.OS.Build.Model!;
			FriendlyName = Settings.Global.GetString(ContextHelper.Current.ContentResolver, Settings.Global.DeviceName)!;
		}
	}
}
