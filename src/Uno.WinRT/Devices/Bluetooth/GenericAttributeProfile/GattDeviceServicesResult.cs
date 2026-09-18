#nullable disable

using System.Collections.Generic;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattDeviceServicesResult
	{
		public byte? ProtocolError { get; }
		public IReadOnlyList<GattDeviceService> Services { get; }
		public GattCommunicationStatus Status { get; }

		private GattDeviceServicesResult()
		{
		}
	}
}
