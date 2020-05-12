using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uno.Devices.Enumeration.Internal;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceInformation
	{
		internal DeviceInformation(DeviceIdentifier deviceIdentifier)
			: this(deviceIdentifier, new Dictionary<string, object>())
		{
		}

		internal DeviceInformation(DeviceIdentifier deviceIdentifier, Dictionary<string, object> properties)
		{
			if (deviceIdentifier is null)
			{
				throw new ArgumentNullException(nameof(deviceIdentifier));
			}

			Id = deviceIdentifier.ToString();
			Properties = properties;
		}

		public string Id { get; }

		public bool IsDefault { get; internal set; }

		public bool IsEnabled { get; internal set; } = true;

		public string Name { get; internal set; }

		public IReadOnlyDictionary<string, object> Properties { get; internal set; }
	}
}
