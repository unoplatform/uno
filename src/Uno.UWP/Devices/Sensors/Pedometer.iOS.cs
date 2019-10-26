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
		private readonly EventWaitHandle _currentReadingWaiter =
			new EventWaitHandle(false, EventResetMode.AutoReset);

		private CMPedometerData _currentData = null;

		private Pedometer(CMPedometer pedometer)
		{
			_pedometer = pedometer;
		}

		public static Pedometer TryCreateInstance()
		{
			if (CMPedometer.IsStepCountingAvailable)
			{
				return new Pedometer(new CMPedometer());
			}
			return null;
		}

		public IReadOnlyDictionary<PedometerStepKind, PedometerReading> GetCurrentReadings()
		{
			var readings = new Dictionary<PedometerStepKind, PedometerReading>();
			_currentData = null;
			_pedometer.QueryPedometerData(
				NSDate.FromTimeIntervalSince1970(DateTimeOffset.Now.Subtract(DateTimeOffset.Now.TimeOfDay).ToUnixTimeSeconds()),
				NSDate.FromTimeIntervalSince1970(DateTimeOffset.Now.ToUnixTimeSeconds()), HandlePedometerData);
			_currentReadingWaiter.WaitOne();
			var timeDifferenceInSeconds = TimeSpan.FromSeconds(
				_currentData.EndDate.SecondsSinceReferenceDate -
				_currentData.StartDate.SecondsSinceReferenceDate);
			if (_currentData != null)
			{
				readings.Add(
					PedometerStepKind.Unknown,
					new PedometerReading(
						_currentData.NumberOfSteps.Int32Value,
						timeDifferenceInSeconds,
						PedometerStepKind.Unknown,
						SensorHelpers.NSDateToDateTimeOffset(_currentData.EndDate)));
			}

			return readings;
		}

		private void StartReading()
		{
			_pedometer.StartPedometerUpdates(
				SensorHelpers.DateTimeOffsetToNSDate(DateTimeOffset.Now),
				HandlePedometerData);
		}

		private void StopReading()
		{
			_pedometer.StopPedometerUpdates();
		}

		private void HandlePedometerData(CMPedometerData data, NSError err)
		{
			_currentData = data;
			_currentReadingWaiter.Set();
		}
	}
}
#endif
