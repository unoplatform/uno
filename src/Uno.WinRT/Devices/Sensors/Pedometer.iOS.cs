#nullable enable

using System;
using CoreMotion;
using Foundation;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Pedometer
	{
		private const int SecondsPerDay = 24 * 60 * 60;
		private CMPedometer? _pedometer;
		private DateTimeOffset _lastReading = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

		private static Pedometer? TryCreateInstance() => !CMPedometer.IsStepCountingAvailable ?
			null :
			new Pedometer();

		public uint ReportInterval { get; set; }

		private void StartReading()
		{
			_pedometer ??= new();

			_pedometer.StartPedometerUpdates(
				NSDate.Now.AddSeconds(-SecondsPerDay),
				PedometerUpdateReceived);
		}

		private void StopReading()
		{

			if (_pedometer == null)
			{
				return;
			}

			_pedometer.StopPedometerUpdates();
			_pedometer.Dispose();
			_pedometer = null;
		}

		private void PedometerUpdateReceived(CMPedometerData data, NSError err)
		{
			if ((DateTimeOffset.UtcNow - _lastReading).TotalMilliseconds >= ReportInterval)
			{
				var startDate = SensorHelpers.NSDateToDateTimeOffset(data.StartDate);
				var endDate = SensorHelpers.NSDateToDateTimeOffset(data.EndDate);
				_lastReading = DateTime.UtcNow;
				OnReadingChanged(new PedometerReading(data.NumberOfSteps.Int32Value, endDate - startDate, PedometerStepKind.Unknown, endDate));
			}
		}
	}
}
