#nullable enable

using System;
using System.Collections.Generic;

namespace Windows.Devices.Bluetooth.Advertisement
{
	public partial class BluetoothLEAdvertisement
	{
		public  string LocalName { get; set; }
		public  BluetoothLEAdvertisementFlags? Flags { get; set; }
		public  IList<BluetoothLEAdvertisementDataSection> DataSections { get; internal set; }
		public  IList<BluetoothLEManufacturerData> ManufacturerData { get; internal set; }
		public IList<Guid> ServiceUuids { get; internal set; }
	}

}
