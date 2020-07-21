using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	public partial class AccelerometerReading
	{
		private AccelerometerReading()
		{

		}

#if __IOS__ || __ANDROID__ || __WASM__
		internal AccelerometerReading(
			double accelerationX,
			double accelerationY,
			double accelerationZ,
			DateTimeOffset timestamp)
		{
			AccelerationX = accelerationX;
			AccelerationY = accelerationY;
			AccelerationZ = accelerationZ;
			Timestamp = timestamp;
		}

		public double AccelerationX { get; }

		public double AccelerationY { get; }

		public double AccelerationZ { get; }

		public DateTimeOffset Timestamp { get; }
#endif
	}
}
