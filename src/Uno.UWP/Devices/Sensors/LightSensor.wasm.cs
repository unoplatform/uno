#nullable enable

using System;
using Uno;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

using NativeMethods = __Windows.Devices.Sensors.LightSensor.NativeMethods;
#endif

namespace Windows.Devices.Sensors
{
	public partial class LightSensor
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.Devices.Sensors.LightSensor";
#endif

		private static LightSensor? TryCreateInstance()
		{
#if NET7_0_OR_GREATER
			return NativeMethods.Initialize() ? new() : null;
#else
			var command = $"{JsType}.initialize()";
			var initialized = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			if (bool.Parse(initialized) == true)
			{
				return new LightSensor();
			}
			return null;
#endif
		}

		private void StartReading()
		{
#if NET7_0_OR_GREATER
			NativeMethods.StartReading();
#else
			var command = $"{JsType}.startReading()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
#endif
		}

		private void StopReading()
		{
#if NET7_0_OR_GREATER
			NativeMethods.StopReading();
#else
			var command = $"{JsType}.stopReading()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
#endif
		}

#if NET7_0_OR_GREATER
		[JSExport]
#endif
		public static int DispatchReading(float lux)
		{
			var reading = new LightSensorReading(lux, DateTimeOffset.UtcNow);
			OnReadingChanged(reading);

			return 0;
		}
	}
}
