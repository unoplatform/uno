#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Uno;

using NativeMethods = __Windows.Devices.Sensors.LightSensor.NativeMethods;

namespace Windows.Devices.Sensors
{
	public partial class LightSensor
	{
		private static LightSensor? TryCreateInstance()
		{
			return NativeMethods.Initialize() ? new() : null;
		}

		private void StartReading()
		{
			NativeMethods.StartReading();
		}

		private void StopReading()
		{
			NativeMethods.StopReading();
		}

		[JSExport]
		internal static int DispatchReading(float lux)
		{
			var reading = new LightSensorReading(lux, DateTimeOffset.UtcNow);
			OnReadingChanged(reading);

			return 0;
		}
	}
}
