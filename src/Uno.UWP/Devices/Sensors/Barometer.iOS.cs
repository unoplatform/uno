#nullable enable

using CoreMotion;
using Foundation;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private CMAltimeter? _altimeter;

		private static Barometer? TryCreateInstance() => !CMAltimeter.IsRelativeAltitudeAvailable ?
			null :
			new Barometer();

		private void StartReading()
		{
			_altimeter ??= new();

			_altimeter.StartRelativeAltitudeUpdates(new NSOperationQueue(), RelativeAltitudeUpdateReceived);
		}

		private void StopReading()
		{
			if (_altimeter == null)
			{
				return;
			}

			_altimeter.StopRelativeAltitudeUpdates();
			_altimeter.Dispose();
			_altimeter = null;
		}

		private void RelativeAltitudeUpdateReceived(CMAltitudeData data, NSError error)
		{
			var barometerReading = new BarometerReading(
				KPaToHPa(data.Pressure.DoubleValue),
				SensorHelpers.TimestampToDateTimeOffset(data.Timestamp));
			_readingChangedWrapper.Invoke(
				this,
				new BarometerReadingChangedEventArgs(barometerReading));
		}

		private double KPaToHPa(double pressureInKPa) => pressureInKPa * 10;
	}
}
