using System.Collections.Generic;
using Uno.Devices.Enumeration.Internal;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceInformation
	{
		internal DeviceInformation(DeviceIdentifier deviceIdentifier) => Id = deviceIdentifier.ToString();

		public string Id { get; }

		public bool IsDefault { get; internal set; }

		public bool IsEnabled { get; internal set; } = true;

		public string Name { get; internal set; }
	}
}
