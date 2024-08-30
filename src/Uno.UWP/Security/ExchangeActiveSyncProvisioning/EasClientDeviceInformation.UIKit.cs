using UIKit;

namespace Windows.Security.ExchangeActiveSyncProvisioning
{
	public partial class EasClientDeviceInformation
	{
		partial void Initialize()
		{
			OperatingSystem = "IOS";
			SystemManufacturer = "Apple inc.";
			SystemFirmwareVersion = UIDevice.CurrentDevice.SystemVersion;
			SystemProductName = UIDevice.CurrentDevice.Model;
			FriendlyName = UIDevice.CurrentDevice.Name;
		}
	}
}
