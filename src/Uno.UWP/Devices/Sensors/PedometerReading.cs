using System;

namespace Windows.Devices.Sensors
{
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

		public int CumulativeSteps { get; }

		public TimeSpan CumulativeStepsDuration { get; }

		public PedometerStepKind StepKind { get; }

		public DateTimeOffset Timestamp { get; }
	}
}
