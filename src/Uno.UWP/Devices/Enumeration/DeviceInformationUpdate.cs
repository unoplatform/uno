using System.Collections.Generic;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceInformationUpdate
	{
		public DeviceInformationUpdate(string deviceClassGuid, string id) => Id = $"{deviceClassGuid}_{id}";

		public string Id { get; }

		public IReadOnlyDictionary<string, object> Properties { get; internal set; }

		public DeviceInformationKind Kind { get; internal set; }
	}
}
