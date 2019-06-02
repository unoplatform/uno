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

		public static Barometer GetDefault()
		{
			if (_instance == null && !_initializationAttempted)
			{
				if (CMAltimeter.IsRelativeAltitudeAvailable)
				{
					_instance = new Barometer(new CMAltimeter());
				}
				_initializationAttempted = true;
			}
			return _instance;
		}

	}
}
#endif
