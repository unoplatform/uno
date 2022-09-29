#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Bluetooth
{
	public partial class BluetoothClassOfDevice
	{
		public BluetoothMajorClass MajorClass { get; }
		public BluetoothMinorClass MinorClass { get; }
		public uint RawValue { get; }
		public BluetoothServiceCapabilities ServiceCapabilities { get; }
		private BluetoothClassOfDevice(uint rawValue)
		{
			// I know that this seems strange, but neverless... UWP seems to works so
			RawValue = rawValue; // this value is preserved even when MajorClass/MinorClass/ServiceCapabilities all are 0

			// bits seems to be:
			// rawValue= 0000 0000 cccc cccc ccca aaaa iiii ii00 (s=service cap, a=major, i=minor)
			MinorClass = (BluetoothMinorClass)((rawValue >> 2) & 0x3f);
			MajorClass = (BluetoothMajorClass)((rawValue >> 8) & 0x1f);
			ServiceCapabilities = (BluetoothServiceCapabilities)((rawValue >> 13) & 0x7ff);
			//FromRawValue(1)		-> MinorClass/MajorClass/ServiceCapabilities are zero
			//FromRawValue(0x10)	-> MinorClass = 4, rest is zero
			//FromRawValue(0x100)	-> MajorClass = 1, rest is zero
			//FromRawValue(0xa00	-> MajorClass = 0xa, rest is zero
			//FromRawValue(0x1000)	-> MajorClass = 0x10, rest is zero
			//FromRawValue(0x2000)	-> ServiceCap = 1, rest is zero
			//FromRawValue(0x800000)-> still ServiceCap
			//FromRawValue(0x1000000) -> MinorClass/MajorClass/ServiceCapabilities are zero
		}

		public static BluetoothClassOfDevice FromRawValue(uint rawValue)
		{
			return new BluetoothClassOfDevice(rawValue);
		}

		private BluetoothClassOfDevice(BluetoothMajorClass majorClass, BluetoothMinorClass minorClass, BluetoothServiceCapabilities serviceCapabilities)
		{
			MajorClass = majorClass;
			MinorClass = minorClass;
			ServiceCapabilities = serviceCapabilities;
			RawValue = ((uint)serviceCapabilities << 13) |
				((uint)majorClass << 8) |
				((uint)minorClass << 2);
		}

		public static BluetoothClassOfDevice FromParts(BluetoothMajorClass majorClass, BluetoothMinorClass minorClass, BluetoothServiceCapabilities serviceCapabilities)
		{
			return new BluetoothClassOfDevice(majorClass, minorClass, serviceCapabilities);
		}
	}
}

