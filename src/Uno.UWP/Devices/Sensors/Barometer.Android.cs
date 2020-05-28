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
		private readonly Sensor _sensor;
		private BarometerListener _listener;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		private Barometer(Sensor barometerSensor)
		{
			_sensor = barometerSensor;
		}

		public uint ReportInterval
		{
			get => _reportInterval;
			set
			{
				lock (_syncLock)
				{
					_reportInterval = value;

					if (_readingChanged != null)
					{
						//restart reading to apply interval
						StartReading();
						StopReading();
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
				return new Barometer(sensor);
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
				_barometer._readingChanged?.Invoke(
					_barometer,
					new BarometerReadingChangedEventArgs(barometerReading));
			}
		}
	}
}
#endif
