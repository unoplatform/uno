
using System.Collections.Generic;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattCharacteristicsResult
	{
		public IReadOnlyList<GattCharacteristic> Characteristics { get; internal set; }
		public  byte? ProtocolError { get; internal set; }
		public  GattCommunicationStatus Status { get; internal set; }
	}
}
