using System;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides data for the sensor reading–changed event.
	/// </summary>
	public partial class SimpleOrientationSensorOrientationChangedEventArgs
	{
		/// <summary>
		/// Gets the current sensor orientation.
		/// </summary>
		public SimpleOrientation Orientation { get; internal set; }

		/// <summary>
		/// Gets the time of the current sensor reading.
		/// </summary>
		public DateTimeOffset Timestamp { get; internal set; }
	}
}
