#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using CoreMotion;

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


	public event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> ReadingChanged
	{
		add
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Barometer", "event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> Barometer.ReadingChanged");
		}
		remove
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Barometer", "event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> Barometer.ReadingChanged");
		}
	}
}
}
#endif
