#nullable disable

using System.Collections.Generic;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattDescriptorsResult
	{
		public IReadOnlyList<GattDescriptor> Descriptors { get; }
		public byte? ProtocolError { get; }
		public GattCommunicationStatus Status { get; }

		private GattDescriptorsResult()
		{
		}

	}

}
