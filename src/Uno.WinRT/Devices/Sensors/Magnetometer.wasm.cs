using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Uno;
using Uno.Devices.Sensors.Helpers;

using NativeMethods = __Windows.Devices.Sensors.Magnetometer.NativeMethods;

namespace Windows.Devices.Sensors
{
	public partial class Magnetometer
	{
		private DateTimeOffset _lastReading = DateTimeOffset.MinValue;

		/// <summary>
		/// This method is not supported directly. An approximation in the form of raising the 
		/// ReadingChanged event only when enough time has passed since the last report.
		/// </summary>
		public uint ReportInterval { get; set; }

		private static Magnetometer TryCreateInstance()
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
		/// Handles readings from Magnetometer.
		/// Filters the readings if too frequent to match chosen ReportInterval.
		/// Uses value ReportInterval * 0.8 to make sure that reporting is
		/// still more frequent rather than less frequent than requested,
		/// which is in line with documentation
		/// </summary>
		/// <param name="x">Magnetic field X</param>
		/// <param name="y">Magnetic field Y</param>
		/// <param name="z">Magnetic field Z</param>
		[JSExport]
		internal static void DispatchReading(float x, float y, float z)
		{
			if (_instance.Value == null)
			{
				throw new InvalidOperationException("Magnetometer:DispatchReading can be called only after Magnetometer is initialized");
			}
			var now = DateTimeOffset.UtcNow;
			if ((now - _instance.Value._lastReading).TotalMilliseconds >= _instance.Value.ReportInterval * 0.8)
			{
				_instance.Value._lastReading = now;
				_instance.Value.OnReadingChanged(
					new MagnetometerReading(
						x,
						y,
						z,
						MagnetometerAccuracy.Unknown,
						now));
			}
		}

		[JSExport]
		internal static Task DispatchReadingAsync(float x, float y, float z)
		{
			DispatchReading(x, y, z);

			return Task.CompletedTask;
		}
	}
}
