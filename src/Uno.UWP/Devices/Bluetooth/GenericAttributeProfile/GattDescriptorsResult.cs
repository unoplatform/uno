#nullable enable

using System.Collections.Generic;

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattDescriptorsResult
	{
		public IReadOnlyList<GattDescriptor> Descriptors { get; internal set; }
		public byte? ProtocolError { get; internal set; }
		public GattCommunicationStatus Status { get; internal set; }

		private GattDescriptorsResult(IReadOnlyList<GattDescriptor> param)
		{
			// dummy for Error CS8618  Non-nullable property 'Value' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
			Descriptors = param;
		}

	}

}
