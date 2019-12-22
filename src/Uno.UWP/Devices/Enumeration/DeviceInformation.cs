using System.Collections.Generic;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceInformation
	{
		internal DeviceInformation(string deviceClassGuid, string id) => Id = FormatDeviceId(deviceClassGuid, id);

		public string Id { get; }

		public bool IsDefault { get; internal set; }

		public bool IsEnabled { get; internal set; } = true;

		public string Name { get; internal set; }
	}
}
