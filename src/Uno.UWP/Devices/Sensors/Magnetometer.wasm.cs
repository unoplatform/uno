#if __WASM__
using System;
using System.Diagnostics;
using Uno;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Magnetometer
	{
		private const string JsType = "Windows.Devices.Sensors.Magnetometer";		

		private DateTimeOffset _lastReading = DateTimeOffset.MinValue;

		private Magnetometer()
		{
		}

		public uint ReportInterval { get; set; }

		private static Magnetometer TryCreateInstance()
		{
			var command = $"{JsType}.initialize()";
			var initialized = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			if (bool.Parse(initialized) == true)
			{
				return new Magnetometer();
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

		/// <summary>
		/// Handles readings from Magnetometer.
		/// Filters the readings if too frequent to match chosen ReportInterval.
		/// Uses value ReportInterval * 0.8 to make sure that reporting is
		/// still more frequent rather than less frequent than requested,
		/// which is in line with documentation
		/// </summary>
		/// <param name="x">Magnetic field X</param>
		/// <param name="y">Magnetic field Y</param>
		/// <param name="z">Magnetic field Z</param>
		/// <returns>0 - needed to bind method from WASM</returns>
		[Preserve]
		public static int DispatchReading(float x, float y, float z)
		{
			if (_instance == null)
			{
				throw new InvalidOperationException("Magnetometer:DispatchReading can be called only after Magnetometer is initialized");
			}
			var now = DateTimeOffset.UtcNow;
			if ((now - _instance._lastReading).TotalMilliseconds >= _instance.ReportInterval * 0.8)
			{
				_instance._lastReading = now;
				_instance.OnReadingChanged(
					new MagnetometerReading(
						x,
						y,
						z,
						MagnetometerAccuracy.Unknown,
						now));
			}			
			return 0;
		}
	}
}
#endif
