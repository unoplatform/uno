#nullable enable

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	public enum DeviceAccessStatus
	{
		Unspecified = 0,
		Allowed = 1,
		DeniedByUser = 2,
		DeniedBySystem = 3,
	}
}
