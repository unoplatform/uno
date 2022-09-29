using System;
using Uno;

namespace Windows.Devices.Sensors
{
	public partial class LightSensor
	{
		private const string JsType = "Windows.Devices.Sensors.LightSensor";

		private static LightSensor? TryCreateInstance()
		{
			var command = $"{JsType}.initialize()";
			var initialized = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			if (bool.Parse(initialized) == true)
			{
				return new LightSensor();
			}
			return null;
		}

		private void StartReading()
		{
			var command = $"{JsType}.startReading()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
		}

		private void StopReading()
		{
			var command = $"{JsType}.stopReading()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
		}

		[Preserve]
		public static int DispatchReading(float lux)
		{
			var reading = new LightSensorReading(lux, DateTimeOffset.UtcNow);
			OnReadingChanged(reading);

			return 0;
		}
	}
}
