using System;

namespace Windows.Devices.Sensors
{
	public  partial class MagnetometerReading 
	{
		internal MagnetometerReading(
			float magneticFieldX,
			float magneticFieldY,
			float magneticFieldZ,
			MagnetometerAccuracy directionalAccuracy,
			DateTimeOffset timestamp)
		{
			MagneticFieldX = magneticFieldX;
			MagneticFieldY = magneticFieldY;
			MagneticFieldZ = magneticFieldZ;
			DirectionalAccuracy = directionalAccuracy;
			Timestamp = timestamp;
		}

		public  global::Windows.Devices.Sensors.MagnetometerAccuracy DirectionalAccuracy { get; }

		public  float MagneticFieldX { get; }

		public  float MagneticFieldY { get; }

		public  float MagneticFieldZ { get; }

		public DateTimeOffset Timestamp { get; }
	}
}
