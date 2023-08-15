namespace Windows.Devices.Sensors;

public partial class CompassReadingChangedEventArgs
{
	internal CompassReadingChangedEventArgs(CompassReading reading)
	{
		Reading = reading;
	}

	public CompassReading Reading { get; }
}
