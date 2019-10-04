using System;

namespace Windows.Devices.Sensors
{
	public partial class GyrometerReading
	{
		internal GyrometerReading(
			double angularVelocityX,
			double angularVelocityY,
			double angularVelocityZ,
			DateTimeOffset timestamp)
		{
			AngularVelocityX = angularVelocityX;
			AngularVelocityY = angularVelocityY;
			AngularVelocityZ = angularVelocityZ;
			Timestamp = timestamp;
		}

		public double AngularVelocityX { get; }

		public double AngularVelocityY { get; }

		public double AngularVelocityZ { get; }

		public DateTimeOffset Timestamp { get; }
	}
}
