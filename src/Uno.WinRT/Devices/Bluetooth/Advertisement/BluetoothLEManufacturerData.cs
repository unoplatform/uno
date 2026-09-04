#nullable enable

namespace Windows.Devices.Bluetooth.Advertisement
{
	public partial class BluetoothLEManufacturerData
	{
		public Storage.Streams.IBuffer Data { get; set; }
		public ushort CompanyId { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public BluetoothLEManufacturerData()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{
		}

		public BluetoothLEManufacturerData(ushort companyId, Storage.Streams.IBuffer data)
		{
			CompanyId = companyId;
			Data = data;
		}

	}
}
