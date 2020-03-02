#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Threading;
using Uno.Devices.Sensors.Helpers;
using Android.Hardware;
using Android.Runtime;

namespace Windows.Devices.Sensors
{
	public partial class Pedometer
	{
		private readonly Sensor _sensor;
		private StepCounterListener _listener;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		private Pedometer(Sensor stepCounterSensor)
		{
			_sensor = stepCounterSensor;
		}

		public uint ReportInterval
		{
			get => _reportInterval;
			set
			{
				if (_reportInterval != value)
				{
					lock (_syncLock)
					{
						_reportInterval = value;

						if (_readingChanged != null)
						{
							//restart reading to apply interval
							StopReading();
							StartReading();
						}
					}
				}
			}
		}

		private static Pedometer TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.StepCounter);
			if (sensor != null)
			{
				return new Pedometer(sensor);
			}
			return null;
		}

		private void StartReading()
		{
			_listener = new StepCounterListener(this);
			SensorHelpers.GetSensorManager().RegisterListener(
				_listener,
				_sensor,
				(SensorDelay)(_reportInterval * 1000));
		}

		private void StopReading()
		{
			if (_listener != null)
			{
				SensorHelpers.GetSensorManager().UnregisterListener(_listener, _sensor);
				_listener.Dispose();
				_listener = null;
			}
		}

		private class StepCounterListener : Java.Lang.Object, ISensorEventListener, IDisposable
		{
			private readonly Pedometer _pedometer;
			private DateTimeOffset _lastReading = DateTimeOffset.MinValue;

			public StepCounterListener(Pedometer pedometer) => _pedometer = pedometer;

			void ISensorEventListener.OnAccuracyChanged(Sensor sensor, [GeneratedEnum]SensorStatus accuracy)
			{
			}

			void ISensorEventListener.OnSensorChanged(SensorEvent e)
			{
				if ((DateTimeOffset.UtcNow - _lastReading).TotalMilliseconds >= _pedometer.ReportInterval)
				{
					var lastStepTimestamp = SensorHelpers.TimestampToDateTimeOffset(e.Timestamp);
					var timeDifference = lastStepTimestamp - SensorHelpers.SystemBootDateTimeOffset;
					var currentSteps = Convert.ToInt32(e.Values[0]);
					var pedometerReading = new PedometerReading(
						currentSteps,
						timeDifference,
						PedometerStepKind.Unknown,
						lastStepTimestamp);

					_lastReading = DateTimeOffset.UtcNow;
					_pedometer.OnReadingChanged(pedometerReading);					
				}
			}
		}
	}
}
#endif
