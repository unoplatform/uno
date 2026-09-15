#nullable enable

using CoreLocation;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors;

public partial class Compass
{
	private CLLocationManager? _locationManager;

	private static Compass? TryCreateInstance() => !CLLocationManager.HeadingAvailable ?
			null :
			new Compass();

	private void StartReadingChanged()
	{
		_locationManager ??= new();
		_locationManager.UpdatedHeading += LocationManagerUpdatedHeading;
		_locationManager.StartUpdatingHeading();
	}

	private void StopReadingChanged()
	{
		if (_locationManager == null)
		{
			return;
		}

		_locationManager.UpdatedHeading -= LocationManagerUpdatedHeading;
		_locationManager.StopUpdatingHeading();
		_locationManager.Dispose();
		_locationManager = null;
	}

	void LocationManagerUpdatedHeading(object? sender, CLHeadingUpdatedEventArgs e)
	{
		var data = new CompassReading(
			e.NewHeading.MagneticHeading,
			e.NewHeading.TrueHeading,
			SensorHelpers.TimestampToDateTimeOffset(e.NewHeading.Timestamp.SecondsSince1970));

		OnReadingChanged(data);
	}
}
