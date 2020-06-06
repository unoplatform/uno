#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private Sensor _sensor;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		private BarometerListener _listener;

		public uint ReportInterval
		{
			get => _reportInterval;
			set
			{
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

		private static Barometer TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Pressure);
			if (sensor != null)
			{
				var barometer = new Barometer();
				barometer._sensor = sensor;
				return barometer;
			}
			return null;
		}

		private void StartReading()
		{
			_listener = new BarometerListener(this);
			SensorHelpers.GetSensorManager().RegisterListener(
				_listener,
				_sensor,
				(SensorDelay)(_reportInterval * 1000));
		}

		private void StopReading()
		{
			if ( _listener != null)
			{
				SensorHelpers.GetSensorManager().UnregisterListener(_listener, _sensor);
				_listener.Dispose();
				_listener = null;
			}
		}

		private class BarometerListener : Java.Lang.Object, ISensorEventListener, IDisposable
		{
			private readonly Barometer _barometer;

			public BarometerListener(Barometer barometer)
			{
				_barometer = barometer;
			}

			void ISensorEventListener.OnAccuracyChanged(Sensor sensor, [GeneratedEnum]SensorStatus accuracy)
			{
			}

			void ISensorEventListener.OnSensorChanged(SensorEvent e)
			{
				var barometerReading = new BarometerReading(
					e.Values[0],
					SensorHelpers.TimestampToDateTimeOffset(e.Timestamp));
				_barometer._readingChangedWrapper.Event?.Invoke(
					_barometer,
					new BarometerReadingChangedEventArgs(barometerReading));
			}
		}
	}
}
#endif
