using System.Collections.Generic;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceInformationUpdate
	{
		public DeviceInformationUpdate(string deviceClassGuid, string id) => Id = DeviceInformation.FormatDeviceId(deviceClassGuid, id);

		public string Id { get; }
	}
}
