using System.Collections.Generic;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceInformation
	{
		internal DeviceInformation(string deviceClassGuid, string id) => Id = $"{deviceClassGuid}_{id}";

		public string Id { get; }

		public bool IsDefault { get; internal set; }

		public bool IsEnabled { get; internal set; }

		public string Name { get; internal set; }

		public IReadOnlyDictionary<string, object> Properties { get; internal set; }

		public DeviceInformationKind Kind { get; internal set; }
	}
}
