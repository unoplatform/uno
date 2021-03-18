#if __ANDROID__
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private readonly Sensor _accelerometer;

		private ReadingChangedListener _readingChangedListener;
		private ShakeListener _shakeListener;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		private Accelerometer(Sensor accelerometer)
		{
			_accelerometer = accelerometer;
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
						StopReadingChanged();
						StartReadingChanged();
					}
				}
			}
		}

		private static Accelerometer TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var accelerometer = sensorManager?.GetDefaultSensor(Android.Hardware.SensorType.Accelerometer);
			if (accelerometer != null)
			{
				return new Accelerometer(accelerometer);
			}
			return null;
		}

		private void StartReadingChanged()
		{
			_readingChangedListener = new ReadingChangedListener(this);
			SensorHelpers.GetSensorManager().RegisterListener(
				_readingChangedListener,
				_accelerometer,
				(SensorDelay)(_reportInterval * 1000));
		}

		private void StopReadingChanged()
		{
			if (_readingChangedListener != null)
			{
				SensorHelpers.GetSensorManager().UnregisterListener(_readingChangedListener, _accelerometer);
				_readingChangedListener.Dispose();
				_readingChangedListener = null;
			}
		}

		private void StartShaken()
		{
			_shakeListener = new ShakeListener(this);
			SensorHelpers.GetSensorManager().RegisterListener(_shakeListener, _accelerometer, (SensorDelay)100000);
		}

		private void StopShaken()
		{
			if (_shakeListener != null)
			{
				SensorHelpers.GetSensorManager().UnregisterListener(_shakeListener, _accelerometer);
				_shakeListener.Dispose();
				_shakeListener = null;
			}
		}

		class ReadingChangedListener : Java.Lang.Object, ISensorEventListener
		{
			private readonly Accelerometer _accelerometer;
			private long _lastTimestamp;

			public ReadingChangedListener(Accelerometer accelerometer)
			{
				_accelerometer = accelerometer;
			}

			public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			public void OnSensorChanged(SensorEvent e)
			{
				var reading = new AccelerometerReading(
					e.Values[0] / SensorManager.GravityEarth * -1,
					e.Values[1] / SensorManager.GravityEarth * -1,
					e.Values[2] / SensorManager.GravityEarth * -1,
					SensorHelpers.TimestampToDateTimeOffset(e.Timestamp));

				_lastTimestamp = e.Timestamp;
				_accelerometer.OnReadingChanged(reading);
			}
		}

		class ShakeListener : Java.Lang.Object, ISensorEventListener
		{
			private readonly ShakeDetector _shakeDetector;

			public ShakeListener(Accelerometer accelerometer)
			{
				_shakeDetector = new ShakeDetector(accelerometer);
			}

			public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			public void OnSensorChanged(SensorEvent e)
			{
				var x = e.Values[0];
				var y = e.Values[1];
				var z = e.Values[2];

				_shakeDetector.OnSensorChanged(x, y, z, SensorHelpers.TimestampToDateTimeOffset(e.Timestamp));
			}
		}
	}
}
#endif
