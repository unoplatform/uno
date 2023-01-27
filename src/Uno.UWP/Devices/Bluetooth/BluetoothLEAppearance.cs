using System;
using System.Collections.Generic;
using System.Text;


namespace Windows.Devices.Bluetooth
{
	public partial class BluetoothLEAppearance
	{
		public ushort Category { get; }

		public ushort RawValue { get; }

		public ushort SubCategory { get; }

		private BluetoothLEAppearance(ushort rawValue)
		{
			RawValue = rawValue;
			SubCategory = (ushort)(rawValue & 0x3f);
			Category = (ushort)((rawValue >> 6) & 0xff);
		}

		public static BluetoothLEAppearance FromRawValue(ushort rawValue)
		{
			return new BluetoothLEAppearance(rawValue);
		}

		private BluetoothLEAppearance(ushort appearanceCategory, ushort appearanceSubCategory)
		{
			SubCategory = appearanceSubCategory;
			Category = appearanceCategory;
			RawValue = (ushort)((Category << 6) | SubCategory);
		}
		public static BluetoothLEAppearance FromParts(ushort appearanceCategory, ushort appearanceSubCategory)
		{
			return new BluetoothLEAppearance(appearanceCategory, appearanceSubCategory);
		}
	}
}
