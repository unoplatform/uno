namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides data for the ambient-light sensor reading-changed event.
	/// </summary>
	public partial class LightSensorReadingChangedEventArgs
	{
		internal LightSensorReadingChangedEventArgs(LightSensorReading reading) =>
			Reading = reading;

		/// <summary>
		/// Gets the current ambient light-sensor reading.
		/// </summary>
		public LightSensorReading Reading { get; }
	}
}
