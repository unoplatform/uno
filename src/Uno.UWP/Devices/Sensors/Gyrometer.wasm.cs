using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Uno;
using Uno.Devices.Sensors.Helpers;

using NativeMethods = __Windows.Devices.Sensors.Gyrometer.NativeMethods;

namespace Windows.Devices.Sensors
{
	public partial class Gyrometer
	{
		private DateTimeOffset _lastReading = DateTimeOffset.MinValue;

		/// <summary>
		/// This method is not supported directly. An approximation in the form of raising the 
		/// ReadingChanged event only when enough time has passed since the last report.
		/// </summary>
		public uint ReportInterval { get; set; }

		private static Gyrometer? TryCreateInstance()
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

		/// <summary>
		/// Handles readings from Gyrometer.
		/// Filters the readings if too frequent to match chosen ReportInterval.
		/// Uses value ReportInterval * 0.8 to make sure that reporting is
		/// still more frequent rather than less frequent than requested,
		/// which is in line with documentation
		/// </summary>
		/// <param name="x">AngularVelocity X in radians/s</param>
		/// <param name="y">AngularVelocity Y in radians/s</param>
		/// <param name="z">AngularVelocity Z in radians/s</param>
		/// <returns>0 - needed to bind method from WASM</returns>
		[JSExport]
		internal static int DispatchReading(float x, float y, float z)
		{
			if (_instance == null)
			{
				throw new InvalidOperationException("Gyrometer:DispatchReading can be called only after Gyrometer is initialized");
			}
			var now = DateTimeOffset.UtcNow;
			if ((now - _instance._lastReading).TotalMilliseconds >= _instance.ReportInterval * 0.8)
			{
				_instance._lastReading = now;
				_instance.OnReadingChanged(
					new GyrometerReading(
						x * SensorConstants.RadToDeg,
						y * SensorConstants.RadToDeg,
						z * SensorConstants.RadToDeg,
						now));
			}
			return 0;
		}
	}
}
