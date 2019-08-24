using System;

namespace Windows.Devices.Sensors
{
	public partial class GyrometerReading
	{
		public GyrometerReading(
			float angularVelocityX,
			float angularVelocityY,
			float angularVelocityZ,
			DateTimeOffset timestamp)
		{
			AngularVelocityX = angularVelocityX;
			AngularVelocityY = angularVelocityY;
			AngularVelocityZ = angularVelocityZ;
			Timestamp = timestamp;
		}

		public float AngularVelocityX { get; }

		public float AngularVelocityY { get; }

		public float AngularVelocityZ { get; }

		public DateTimeOffset Timestamp { get; }
	}
}
