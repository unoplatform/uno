#nullable disable

using System.Collections.Generic;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattCharacteristicsResult
	{
		public IReadOnlyList<GattCharacteristic> Characteristics { get; }
		public  byte? ProtocolError { get; }
		public  GattCommunicationStatus Status { get; }

		private GattCharacteristicsResult()
		{
		}
	}
}
