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

		private Barometer(Sensor barometerSensor)
		{
			_sensor = barometerSensor;
		}

		private static Barometer TryCreateInstance()
		{
			var sensorManager = GetSensorManager();
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Pressure);
			if (sensor != null)
			{
				return new Barometer(sensor);
			}
			return null;
		}

		private static SensorManager GetSensorManager() =>
			Application.Context.GetSystemService(Context.SensorService) as SensorManager;

		private void StartReading()
		{
			_listener = new BarometerListener(this);
			GetSensorManager().RegisterListener(_listener, _sensor, SensorDelay.Normal);
		}

		private void StopReading()
		{
			if ( _listener != null)
			{
				GetSensorManager().UnregisterListener(_listener, _sensor);
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
