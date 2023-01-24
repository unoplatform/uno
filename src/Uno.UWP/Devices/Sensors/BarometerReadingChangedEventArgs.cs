namespace Windows.Devices.Sensors
{
	public partial class BarometerReadingChangedEventArgs
	{
		internal BarometerReadingChangedEventArgs(BarometerReading reading) =>
			Reading = reading;

		public BarometerReading Reading { get; }
	}
}
