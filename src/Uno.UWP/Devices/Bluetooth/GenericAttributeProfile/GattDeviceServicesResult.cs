#nullable enable
using System.Collections.Generic;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattDeviceServicesResult
	{
		public byte? ProtocolError { get; internal set; }
		public IReadOnlyList<GattDeviceService> Services { get; internal set; }
		public GattCommunicationStatus Status { get; internal set; }

		private GattDeviceServicesResult(IReadOnlyList<GattDeviceService> services)
		{
			// dummy for Error CS8618  Non-nullable property 'Value' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
			Services = services;
		}

	}

}
