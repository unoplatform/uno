using System;

namespace Windows.Devices.Sensors
{
	public partial class LightSensorReading
	{
		internal LightSensorReading(float illuminanceInLux, DateTimeOffset timestamp)
		{
			IlluminanceInLux = illuminanceInLux;
			Timestamp = timestamp;
		}

		public float IlluminanceInLux { get; }

		public DateTimeOffset Timestamp { get; }
	}
}
