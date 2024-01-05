using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides data for the accelerometer reading– changed event.
	/// </summary>
	public partial class AccelerometerReadingChangedEventArgs
	{
		internal AccelerometerReadingChangedEventArgs(AccelerometerReading reading)
		{
			Reading = reading;
		}

#if __IOS__ || __ANDROID__ || __WASM__

		/// <summary>
		/// Gets the most recent accelerometer reading.
		/// </summary>
		public AccelerometerReading Reading { get; }
#endif
	}
}
