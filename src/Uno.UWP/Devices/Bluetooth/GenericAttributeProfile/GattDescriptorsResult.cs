using System.Collections.Generic;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattDescriptorsResult
	{
		public  IReadOnlyList<GattDescriptor> Descriptors { get; internal set; }
		public  byte? ProtocolError { get; internal set; }
		public  GattCommunicationStatus Status { get; internal set; }
	}

}
