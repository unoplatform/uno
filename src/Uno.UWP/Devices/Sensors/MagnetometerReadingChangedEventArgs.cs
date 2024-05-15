namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides data for the magnetometer reading– changed event.
	/// </summary>
	public partial class MagnetometerReadingChangedEventArgs
	{
		internal MagnetometerReadingChangedEventArgs(MagnetometerReading reading) =>
			Reading = reading;

		/// <summary>
		/// Gets the current magnetometer reading.
		/// </summary>
		public MagnetometerReading Reading { get; }
	}
}
