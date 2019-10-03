namespace Windows.Devices.Sensors
{
	public partial class GyrometerReadingChangedEventArgs
	{
		internal GyrometerReadingChangedEventArgs(GyrometerReading reading) =>
			Reading = reading;

		public GyrometerReading Reading { get; }
	}
}
