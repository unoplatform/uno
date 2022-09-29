using System;
using Android.Hardware;
using Android.Runtime;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class LightSensor
	{
		private Sensor _sensor = null!;
		private LightSensorListener? _listener;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		/// <summary>
		/// Gets or sets the current report interval for the ambient light sensor.
		/// </summary>
		public uint ReportInterval
		{
			get => _reportInterval;
			set
			{
				lock (_readingChangedWrapper.SyncLock)
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

		private static LightSensor? TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Light);
			if (sensor != null)
			{
				return new LightSensor()
				{
					_sensor = sensor
				};
			}
			return null;
		}

		private void StartReading()
		{
			_listener = new LightSensorListener(this);
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

		private class LightSensorListener : Java.Lang.Object, ISensorEventListener, IDisposable
		{
			private readonly LightSensor _lightSensor;

			public LightSensorListener(LightSensor lightSensor)
			{
				_lightSensor = lightSensor;
			}

			void ISensorEventListener.OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			void ISensorEventListener.OnSensorChanged(SensorEvent? e)
			{
				if (e?.Values != null)
				{
					var reading = new LightSensorReading(
						e.Values[0],
						SensorHelpers.TimestampToDateTimeOffset(e.Timestamp));
					OnReadingChanged(reading);
				}
			}
		}
	}
}
