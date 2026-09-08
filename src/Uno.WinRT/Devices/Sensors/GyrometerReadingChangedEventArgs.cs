namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides data for the gyrometer reading– changed event.
	/// </summary>
	public partial class GyrometerReadingChangedEventArgs
	{
		internal GyrometerReadingChangedEventArgs(GyrometerReading reading) =>
			Reading = reading;

		/// <summary>
		/// Gets the current gyrometer reading.
		/// </summary>
		public GyrometerReading Reading { get; }
	}
}
