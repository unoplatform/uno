#nullable disable

namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattReadResult
	{
		public GattCommunicationStatus Status { get; }
		public Storage.Streams.IBuffer Value { get; }
		public byte? ProtocolError { get; }

		private GattReadResult()
		{
		}
	}
}
