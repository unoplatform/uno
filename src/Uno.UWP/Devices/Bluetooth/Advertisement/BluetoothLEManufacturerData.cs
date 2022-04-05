
namespace Windows.Devices.Bluetooth.Advertisement
{
	public partial class BluetoothLEManufacturerData
	{
		public  Storage.Streams.IBuffer Data { get; set; }
		public  ushort CompanyId { get; set; }
		public BluetoothLEManufacturerData( ushort companyId,  Storage.Streams.IBuffer data) 
		{
			CompanyId = companyId;
			Data = data;
		}

	}
}
