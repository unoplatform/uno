#if __IOS__
using System;
using System.Collections.Generic;
using System.Threading;
using CoreMotion;
using Foundation;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Pedometer
	{
		private readonly CMPedometer _pedometer;
		private DateTimeOffset _lastReading = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

		private Pedometer(CMPedometer pedometer) => _pedometer = pedometer;

		public static Pedometer TryCreateInstance()
		{
			if (CMPedometer.IsStepCountingAvailable)
			{
				return new Pedometer(new CMPedometer());
			}
			return null;
		}

		public uint ReportInterval { get; set; }

		private void StartReading()
		{
			_pedometer.StartPedometerUpdates(
				SensorHelpers.DateTimeOffsetToNSDate(DateTimeOffset.Now.Date),
				PedometerUpdateReceived);
		}

		private void StopReading()
		{
			_pedometer.StopPedometerUpdates();
		}

		private void PedometerUpdateReceived(CMPedometerData data, NSError err)
		{
			if ((DateTimeOffset.UtcNow - _lastReading).TotalMilliseconds >= ReportInterval)
			{
				var startDate = SensorHelpers.NSDateToDateTimeOffset(data.StartDate);
				var endDate = SensorHelpers.NSDateToDateTimeOffset(data.EndDate);
				OnReadingChanged(new PedometerReading(data.NumberOfSteps.Int32Value, endDate - startDate, PedometerStepKind.Unknown, endDate));
			}
		}
	}
}
#endif
