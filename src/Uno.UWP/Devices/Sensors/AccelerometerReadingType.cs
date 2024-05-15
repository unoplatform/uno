#if __IOS__ || __ANDROID__ || __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Indicates the type of accelerometer.
	/// </summary>
	public enum AccelerometerReadingType
	{
		Standard,
		Linear,
		Gravity,
	}
}
#endif
