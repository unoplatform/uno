#if __WASM__
using Uno;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private const string JsType = "Windows.Devices.Sensors.Accelerometer";

		private Accelerometer()
		{
		}


		private static Accelerometer TryCreateInstance()
		{
			var command = $"{JsType}.initialize()";
			var initialized = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			if (bool.Parse(initialized) == true)
			{
				return new Accelerometer();
			}
			return null;
		}

		private void StartReadingChanged()
		{
			var command = $"{JsType}.startReading()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
		}

		private void StopReadingChanged()
		{
			var command = $"{JsType}.stopReading()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
		}

		private void StartShaken()
		{
		}

		private void StopShaken()
		{
		}

		[Preserve]
		public static int DispatchReading(float x, float y, float z)
		{
			_instance.OnReadingChanged(new AccelerometerReading(x, y, z, DateTimeOffset.UtcNow));
			return 0;
		}
	}
}
#endif
