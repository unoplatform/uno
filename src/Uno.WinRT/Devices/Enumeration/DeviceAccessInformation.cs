#nullable enable

namespace Windows.Devices.Enumeration
{
	public partial class DeviceAccessInformation
	{
		internal DeviceAccessInformation()
		{
		}

		public DeviceAccessStatus CurrentStatus { get; internal set; }

	}
}
