namespace Windows.Devices.Sensors;

/// <summary>
/// Provides data for the compass reading–changed event.
/// </summary>
public partial class CompassReadingChangedEventArgs
{
	internal CompassReadingChangedEventArgs(CompassReading reading)
	{
		Reading = reading;
	}

	/// <summary>
	/// Gets the current compass reading.
	/// </summary>
	public CompassReading Reading { get; }
}
