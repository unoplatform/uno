#if __WASM__
using Uno;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private const string JsType = "Windows.Devices.Sensors.Accelerometer";
		private const float Gravity = 9.81f;

		private ShakeDetector _shakeDetector = null;

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

		private void StartReadingChanged() => AttachDeviceMotion();

		private void StopReadingChanged() => DetachDeviceMotion();

		private void StartShaken()
		{
			if (_shakeDetector == null)
			{
				_shakeDetector = new ShakeDetector(this);
			}
			AttachDeviceMotion();
		}

		private void StopShaken() => DetachDeviceMotion();


		private void AttachDeviceMotion()
		{
			//if both delegates are not null,
			//we have already started reading previously
			if (_shaken == null || _readingChanged == null)
			{
				var command = $"{JsType}.startReading()";
				Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			}
		}


		private void DetachDeviceMotion()
		{
			//we only stop when both are null
			if (_shaken == null && _readingChanged == null)
			{
				var command = $"{JsType}.stopReading()";
				Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			}
		}

		[Preserve]
		public static int DispatchReading(float x, float y, float z)
		{
			_instance?.OnReadingChanged(
				new AccelerometerReading(
					x / Gravity * -1,
					y / Gravity * -1,
					z / Gravity * -1,
					DateTimeOffset.UtcNow));
			_instance?._shakeDetector?.OnSensorChanged(x, y, z, DateTimeOffset.UtcNow);
			return 0;
		}
	}
}
#endif
