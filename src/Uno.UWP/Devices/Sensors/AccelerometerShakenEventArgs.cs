using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
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

		public DateTimeOffset Timestamp { get; }
#endif
	}
}
