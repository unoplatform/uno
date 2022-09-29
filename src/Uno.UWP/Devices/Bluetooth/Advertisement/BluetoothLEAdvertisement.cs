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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public BluetoothLEAdvertisement()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{
		}

		internal BluetoothLEAdvertisement(string localName, IList<BluetoothLEAdvertisementDataSection> dataSections, IList<BluetoothLEManufacturerData> manufacturerData, IList<Guid> serviceUuids)
		{
			LocalName = localName;
			DataSections = dataSections;
			ManufacturerData = manufacturerData;
			ServiceUuids = serviceUuids;
		}
	}

}
