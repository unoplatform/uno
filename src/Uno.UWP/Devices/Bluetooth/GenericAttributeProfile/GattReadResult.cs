#nullable enable

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattReadResult
	{
		public GattCommunicationStatus Status { get; internal set; }
		public Storage.Streams.IBuffer Value { get; internal set; }
		public byte? ProtocolError { get; internal set; }

		private GattReadResult(Storage.Streams.IBuffer buffer)
		{
			// dummy for Error CS8618  Non-nullable property 'Value' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
			Value = buffer;
		}
	}
}
