using System;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents an ambient light–sensor reading.
	/// </summary>
	public partial class LightSensorReading
	{
		internal LightSensorReading(float illuminanceInLux, DateTimeOffset timestamp)
		{
			IlluminanceInLux = illuminanceInLux;
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the illuminance level in lux.
		/// </summary>
		public float IlluminanceInLux { get; }

		/// <summary>
		/// Gets the time at which the sensor reported the reading.
		/// </summary>
		public DateTimeOffset Timestamp { get; }
	}
}
