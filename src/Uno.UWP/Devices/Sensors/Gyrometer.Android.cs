#nullable enable

using System;
using Android.Hardware;
using Android.Runtime;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Gyrometer
	{
		private Sensor? _sensor;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		private GyrometerListener? _listener;

		/// <summary>
		/// Gets or sets the current report interval for the gyrometer.
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

		private static Gyrometer? TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Gyroscope);

			return sensor == null ? null : new Gyrometer();
		}

		private void StartReading()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			_sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Gyroscope);

			if (_sensor != null)
			{
				_listener = new GyrometerListener(this);
				SensorHelpers.GetSensorManager().RegisterListener(
					_listener,
					_sensor,
					(SensorDelay)(_reportInterval * 1000));
			}
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

		private class GyrometerListener : Java.Lang.Object, ISensorEventListener, IDisposable
		{
			private readonly Gyrometer _gyrometer;

			public GyrometerListener(Gyrometer gyrometer)
			{
				_gyrometer = gyrometer;
			}

			public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			void ISensorEventListener.OnSensorChanged(SensorEvent? e)
			{
				if (e?.Values is not { } values)
				{
					return;
				}

				var gyrometerReading = new GyrometerReading(
					values[0] * SensorConstants.RadToDeg,
					values[1] * SensorConstants.RadToDeg,
					values[2] * SensorConstants.RadToDeg,
					SensorHelpers.TimestampToDateTimeOffset(e.Timestamp)
				);
				_gyrometer.OnReadingChanged(gyrometerReading);
			}
		}
	}
}
