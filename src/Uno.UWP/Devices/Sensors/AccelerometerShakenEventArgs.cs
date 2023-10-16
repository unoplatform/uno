using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides data for the accelerometer-shaken event.
	/// </summary>
	public partial class AccelerometerShakenEventArgs
	{
		internal AccelerometerShakenEventArgs()
		{
		}

#if __IOS__ || __ANDROID__ || __WASM__
		internal AccelerometerShakenEventArgs(DateTimeOffset timestamp)
		{
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the time at which the sensor reported the shaken event.
		/// </summary>
		public DateTimeOffset Timestamp { get; }
#endif
	}
}
