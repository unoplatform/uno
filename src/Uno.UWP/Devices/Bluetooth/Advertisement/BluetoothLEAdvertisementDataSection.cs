#nullable enable

namespace Windows.Devices.Bluetooth.Advertisement
{
	public partial class BluetoothLEAdvertisementDataSection
	{
		public byte DataType { get; set; }
		public Storage.Streams.IBuffer Data { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public BluetoothLEAdvertisementDataSection()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{
		}

		public BluetoothLEAdvertisementDataSection(byte dataType, Storage.Streams.IBuffer data)
		{
			DataType = dataType;
			Data = data;
		}
	}
}
