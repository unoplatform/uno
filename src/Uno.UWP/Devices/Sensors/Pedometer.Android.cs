#nullable enable

using System;
using Android.Hardware;
using Android.Runtime;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Pedometer
	{
		private Sensor? _sensor;
		private StepCounterListener? _listener;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		/// <summary>
		/// Gets or sets the current report interval for the pedometer.
		/// </summary>
		public uint ReportInterval
		{
			get => _reportInterval;
			set
			{
				if (_reportInterval == value)
				{
					return;
				}

				lock (_syncLock)
				{
					_reportInterval = value;

					if (_readingChangedWrapper.Event != null)
					{
						//restart reading to apply interval
						StopReading();
						StartReading();
					}
				}
			}
		}

		private static Pedometer? TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.StepCounter);

			return sensor == null ? null : new();
		}

		private void StartReading()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			_sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.StepCounter);

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

			_sensor?.Dispose();
			_sensor = null;
		}

		private class StepCounterListener : Java.Lang.Object, ISensorEventListener, IDisposable
		{
			private readonly Pedometer _pedometer;
			private DateTimeOffset _lastReading = DateTimeOffset.MinValue;

			public StepCounterListener(Pedometer pedometer) => _pedometer = pedometer;

			void ISensorEventListener.OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			void ISensorEventListener.OnSensorChanged(SensorEvent? e)
			{
				if (e?.Values is not { } values)
				{
					return;
				}

				if ((DateTimeOffset.UtcNow - _lastReading).TotalMilliseconds >= _pedometer.ReportInterval)
				{
					var lastStepTimestamp = SensorHelpers.TimestampToDateTimeOffset(e.Timestamp);
					var timeDifference = lastStepTimestamp - SensorHelpers.SystemBootDateTimeOffset;
					var currentSteps = Convert.ToInt32(values[0]);
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
