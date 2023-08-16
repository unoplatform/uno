using CoreLocation;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors;

public partial class Compass
{
	private readonly CLLocationManager _locationManager;

	private Compass(CLLocationManager locationManager)
	{
		_locationManager = locationManager;
	}

	public uint ReportInterval { set; get; }

	private static Compass TryCreateInstance()
	{
		var locationManager = new CLLocationManager();
		return !CLLocationManager.HeadingAvailable ?
			null :
			new Compass(locationManager);
	}

	private void StartReadingChanged()
	{
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
	}

	void LocationManagerUpdatedHeading(object sender, CLHeadingUpdatedEventArgs e)
	{
		var data = new CompassReading(
			e.NewHeading.MagneticHeading,
			e.NewHeading.TrueHeading,
			SensorHelpers.TimestampToDateTimeOffset(e.NewHeading.Timestamp.SecondsSince1970));

		OnReadingChanged(data);
	}
}
