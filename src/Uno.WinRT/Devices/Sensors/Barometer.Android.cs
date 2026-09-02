#nullable enable

using System;
using Android.Hardware;
using Android.Runtime;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private Sensor? _sensor;
		private BarometerListener? _listener;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		/// <summary>
		/// Gets or sets the current report interval for the barometer.
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

		private static Barometer? TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Pressure);

			return sensor == null ? null : new Barometer();
		}

		private void StartReading()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			_sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Pressure);

			_listener = new BarometerListener(this);
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

		private class BarometerListener : Java.Lang.Object, ISensorEventListener, IDisposable
		{
			private readonly Barometer _barometer;

			public BarometerListener(Barometer barometer)
			{
				_barometer = barometer;
			}

			void ISensorEventListener.OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			void ISensorEventListener.OnSensorChanged(SensorEvent? e)
			{
				if (e?.Values is not { } values)
				{
					return;
				}

				var barometerReading = new BarometerReading(
					values[0],
					SensorHelpers.TimestampToDateTimeOffset(e.Timestamp));
				_barometer._readingChangedWrapper?.Invoke(
					_barometer,
					new BarometerReadingChangedEventArgs(barometerReading));
			}
		}
	}
}
