
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	public partial class GattReadResult
	{
		public GattCommunicationStatus Status { get; internal set; }
		public Storage.Streams.IBuffer Value { get; internal set; }
		public byte? ProtocolError { get; internal set; }
	}
}
