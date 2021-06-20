namespace Windows.Devices.Sensors
{
	public partial class LightSensorReadingChangedEventArgs
	{
		internal LightSensorReadingChangedEventArgs(LightSensorReading reading)
		{
			Reading = reading;
		}

		public LightSensorReading Reading { get; }
	}
}
