namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides data for the barometer reading– changed event.
	/// </summary>
	public partial class BarometerReadingChangedEventArgs
	{
		internal BarometerReadingChangedEventArgs(BarometerReading reading) =>
			Reading = reading;

		/// <summary>
		/// Gets the most recent barometer reading.
		/// </summary>
		public BarometerReading Reading { get; }
	}
}
