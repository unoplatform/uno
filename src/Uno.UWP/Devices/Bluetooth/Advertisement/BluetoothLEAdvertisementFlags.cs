#nullable enable

using System;

namespace Windows.Devices.Bluetooth.Advertisement
{
	[Flags]
	public enum BluetoothLEAdvertisementFlags : uint
	{
		None = 0,
		LimitedDiscoverableMode = 1,
		GeneralDiscoverableMode = 2,
		ClassicNotSupported = 4,
		DualModeControllerCapable = 8,
		DualModeHostCapable = 16,
	}
}
