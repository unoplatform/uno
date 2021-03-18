#if __ANDROID__
using System;

namespace Windows.Devices.Sensors
{
	public partial class HingeAngleReading
	{
		internal HingeAngleReading(double angleInDegrees, DateTimeOffset timestamp)
		{
			AngleInDegrees = angleInDegrees;
			Timestamp = timestamp;
		}

		public double AngleInDegrees { get; }

		public DateTimeOffset Timestamp { get; }
	}
}
#endif
