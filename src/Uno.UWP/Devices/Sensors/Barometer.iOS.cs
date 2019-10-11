#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using CoreMotion;
using Foundation;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private readonly CMAltimeter _altimeter;

		private Barometer(CMAltimeter altimeter)
		{
			_altimeter = altimeter;
		}

		private static Barometer TryCreateInstance()
		{
			if (CMAltimeter.IsRelativeAltitudeAvailable)
			{
				return new Barometer(new CMAltimeter());
			}
			return null;
		}

		private void StartReading()
		{
			_altimeter.StartRelativeAltitudeUpdates(new NSOperationQueue(), RelativeAltitudeUpdateReceived);
		}

		private void StopReading()
		{
			_altimeter.StopRelativeAltitudeUpdates();
		}

		private void RelativeAltitudeUpdateReceived(CMAltitudeData data, NSError error)
		{
			var barometerReading = new BarometerReading(
				KPaToHPa(data.Pressure.DoubleValue),
				SensorHelpers.TimestampToDateTimeOffset(data.Timestamp));
			_readingChanged?.Invoke(
				this,
				new BarometerReadingChangedEventArgs(barometerReading));
		}

		private double KPaToHPa(double pressureInKPa) => pressureInKPa * 10;
	}
}
#endif
