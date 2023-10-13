using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents an accelerometer reading.
	/// </summary>
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

		/// <summary>
		/// Gets the g-force acceleration along the x-axis.
		/// </summary>
		public double AccelerationX { get; }

		/// <summary>
		/// Gets the g-force acceleration along the y-axis.
		/// </summary>
		public double AccelerationY { get; }

		/// <summary>
		/// Gets the g-force acceleration along the z-axis.
		/// </summary>
		public double AccelerationZ { get; }

		/// <summary>
		/// Gets the time at which the sensor reported the reading.
		/// </summary>
		public DateTimeOffset Timestamp { get; }
#endif
	}
}
