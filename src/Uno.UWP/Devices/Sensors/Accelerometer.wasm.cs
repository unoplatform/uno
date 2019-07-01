#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private static Accelerometer TryCreateInstance()
		{
			return null;
		}

		private void StartReadingChanged()
		{
		}

		private void StopReadingChanged()
		{
		}

		private void StartShaken()
		{
		}

		private void StopShaken()
		{
		}
	}
}
#endif
