#if __WASM__
using System;
using System.Diagnostics;
using Uno;
using Uno.Devices.Sensors.Helpers;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

using NativeMethods = __Windows.Devices.Sensors.Magnetometer.NativeMethods;
#endif

namespace Windows.Devices.Sensors
{
	public partial class Magnetometer
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.Devices.Sensors.Magnetometer";
#endif

		private DateTimeOffset _lastReading = DateTimeOffset.MinValue;

		private Magnetometer()
		{
		}

		public uint ReportInterval { get; set; }

		private static Magnetometer TryCreateInstance()
		{
#if NET7_0_OR_GREATER
			return NativeMethods.Initialize() ? new() : null;
#else
			var command = $"{JsType}.initialize()";
			var initialized = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			if (bool.Parse(initialized) == true)
			{
				return new Magnetometer();
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
#if NET7_0_OR_GREATER
		[JSExport]
#endif
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
