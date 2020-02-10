using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Devices.Sensors.Helpers
{
	public class NativeHingeAngleReading
	{
		public NativeHingeAngleReading(double angleInDegrees, DateTimeOffset timestamp)
		{
			AngleInDegrees = angleInDegrees;
			Timestamp = timestamp;
		}

		public double AngleInDegrees { get; }

		public DateTimeOffset Timestamp { get; }
	}
}
