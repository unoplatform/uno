using System;
using System.Collections.Generic;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents a barometer reading.
	/// </summary>
	public partial class BarometerReading
	{
		internal BarometerReading(
			double stationPressureInHectopascals,
			DateTimeOffset timestamp)
		{
			StationPressureInHectopascals = stationPressureInHectopascals;
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the barometric pressure determined by the barometer sensor.
		/// </summary>
		public double StationPressureInHectopascals { get; }

		/// <summary>
		/// Gets the time for the most recent barometer reading.
		/// </summary>
		public DateTimeOffset Timestamp { get; }

		[Uno.NotImplemented]
		public TimeSpan? PerformanceCount { get; }

		[Uno.NotImplemented]
		public IReadOnlyDictionary<string, object>? Properties { get; }
	}
}
