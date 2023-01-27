using System;
using System.Collections.Generic;

namespace Windows.Devices.Sensors
{
	public partial class BarometerReading
	{
		internal BarometerReading(
			double stationPressureInHectopascals,
			DateTimeOffset timestamp)
		{
			StationPressureInHectopascals = stationPressureInHectopascals;
			Timestamp = timestamp;
		}

		public double StationPressureInHectopascals { get; }

		public DateTimeOffset Timestamp { get; }

		[Uno.NotImplemented]
		public TimeSpan? PerformanceCount { get; }

		[Uno.NotImplemented]
		public IReadOnlyDictionary<string, object> Properties { get; }
	}
}
