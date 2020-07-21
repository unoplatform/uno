using Uno;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private Accelerometer()
		{
		}

		private static Accelerometer TryCreateInstance()
		{
			return null;
		}

		private void StartReadingChanged() { }

		private void StopReadingChanged() { }

		private void StartShaken() { }

		private void StopShaken() { }
	}
}
