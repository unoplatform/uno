#if __ANDROID__
using System;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides access to the data exposed by the hinge angle sensor in a dual-screen device.
	/// </summary>
	public partial class HingeAngleReading
	{
		internal HingeAngleReading(double angleInDegrees, DateTimeOffset timestamp)
		{
			AngleInDegrees = angleInDegrees;
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the angle reported by the hinge angle sensor.
		/// </summary>
		public double AngleInDegrees { get; }

		/// <summary>
		/// Gets the time when the hinge angle reading was obtained.
		/// </summary>
		public DateTimeOffset Timestamp { get; }
	}
}
#endif
