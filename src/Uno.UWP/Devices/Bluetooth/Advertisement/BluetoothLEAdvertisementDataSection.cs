#nullable enable

namespace Windows.Devices.Bluetooth.Advertisement
{ 
	public partial class BluetoothLEAdvertisementDataSection
	{
		public  byte DataType { get; set; }
		public  Storage.Streams.IBuffer Data { get; set; }
		public BluetoothLEAdvertisementDataSection(byte dataType, Storage.Streams.IBuffer data)
		{
			DataType = dataType;
			Data = data;
		}
	}
}
