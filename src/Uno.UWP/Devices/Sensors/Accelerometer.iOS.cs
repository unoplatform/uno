#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private static Accelerometer TryCreateInstance(AccelerometerReadingType type)
		{
			if (type != AccelerometerReadingType.Standard) return null;
		}
	}
}
#endif
