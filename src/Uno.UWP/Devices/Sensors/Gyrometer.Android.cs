#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Hardware;
using Android.Runtime;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Gyrometer
	{
		private readonly Sensor _sensor;
		private uint _reportInterval = 0;

		private GyrometerListener _listener;

		private Gyrometer(Sensor barometerSensor)
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
						StopReading();
						StartReading();
					}
				}
			}
		}

		private static Gyrometer TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Gyroscope);
			if (sensor != null)
			{
				return new Gyrometer(sensor);
			}
			return null;
		}

		private void StartReading()
		{
			_listener = new GyrometerListener(this);
			SensorHelpers.GetSensorManager().RegisterListener(_listener, _sensor, SensorDelay.Normal);
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

		private class GyrometerListener : Java.Lang.Object, ISensorEventListener, IDisposable
		{
			private readonly Gyrometer _gyrometer;			

			public GyrometerListener(Gyrometer gyrometer)
			{
				_gyrometer = gyrometer;
			}

			public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			void ISensorEventListener.OnSensorChanged(SensorEvent e)
			{
				var gyrometerReading = new GyrometerReading(
					e.Values[0] * SensorConstants.RadToDeg,
					e.Values[1] * SensorConstants.RadToDeg,
					e.Values[2] * SensorConstants.RadToDeg,
					SensorHelpers.TimestampToDateTimeOffset(e.Timestamp)
				);
				_gyrometer.OnReadingChanged(gyrometerReading);
			}
		}
	}
}
#endif
