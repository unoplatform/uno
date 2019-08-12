#if __IOS__ || __ANDROID__ || __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	public partial class AccelerometerReadingChangedEventArgs
	{
		internal AccelerometerReadingChangedEventArgs(AccelerometerReading reading)
		{
			Reading = reading;
		}

		public AccelerometerReading Reading { get; }
	}
}
#endif
