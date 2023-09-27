namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides data for the pedometer reading– changed event.
	/// </summary>
	public partial class PedometerReadingChangedEventArgs
	{
		internal PedometerReadingChangedEventArgs(
			PedometerReading reading)
		{
			Reading = reading;
		}

		/// <summary>
		/// Gets the most recent pedometer reading.
		/// </summary>
		public PedometerReading Reading { get; }
	}
}
