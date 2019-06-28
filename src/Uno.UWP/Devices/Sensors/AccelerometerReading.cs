using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	public partial class AccelerometerReading
	{
		public double AccelerationX { get; }

		public double AccelerationY { get; }

		public double AccelerationZ { get; }

		public global::System.DateTimeOffset Timestamp { get; }

		public global::System.TimeSpan? PerformanceCount { get; }

		public global::System.Collections.Generic.IReadOnlyDictionary<string, object> Properties { get; }
	}
}
