using System;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents a pedometer reading.
	/// </summary>
	public partial class PedometerReading
	{
		internal PedometerReading(
			int cumulativeSteps,
			TimeSpan cumulativeStepsDuration,
			PedometerStepKind stepKind,
			DateTimeOffset timestamp)
		{
			CumulativeSteps = cumulativeSteps;
			CumulativeStepsDuration = cumulativeStepsDuration;
			StepKind = stepKind;
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the total number of steps taken for this pedometer reading.
		/// </summary>
		public int CumulativeSteps { get; }

		/// <summary>
		/// Gets the amount of time that has elapsed for this pedometer reading.
		/// </summary>
		public TimeSpan CumulativeStepsDuration { get; }

		/// <summary>
		/// Indicates the type of steps taken for this pedometer reading.
		/// </summary>
		public PedometerStepKind StepKind { get; }

		/// <summary>
		/// Gets the time for the most recent pedometer reading.
		/// </summary>
		public DateTimeOffset Timestamp { get; }
	}
}
