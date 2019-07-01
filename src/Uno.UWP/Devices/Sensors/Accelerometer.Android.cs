#if __ANDROID__
using Windows.Devices.Sensors.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Java.Lang;
using Math = System.Math;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private Sensor _accelerometer;
		private ReadingChangedListener _readingChangedListener;
		private ShakeListener _shakeListener;
		private uint _reportInterval = 0;

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
			var sensorManager = GetSensorManager();
			var accelerometer = sensorManager?.GetDefaultSensor(Android.Hardware.SensorType.Accelerometer);
			if (accelerometer != null)
			{
				return new Accelerometer(accelerometer);
			}
			return null;
		}

		private static SensorManager GetSensorManager() =>
			Application.Context.GetSystemService(Context.SensorService) as SensorManager;

		private void StartReadingChanged()
		{
			_readingChangedListener = new ReadingChangedListener(this);
			GetSensorManager().RegisterListener(
				_readingChangedListener,
				_accelerometer,
				(SensorDelay)(_reportInterval * 1000));
		}

		private void StopReadingChanged()
		{
			if (_readingChangedListener != null)
			{
				GetSensorManager().UnregisterListener(_readingChangedListener, _accelerometer);
				_readingChangedListener.Dispose();
				_readingChangedListener = null;
			}
		}

		private void StartShaken()
		{
			_shakeListener = new ShakeListener(this);
			GetSensorManager().RegisterListener(_shakeListener, _accelerometer, (SensorDelay)100000);
		}

		private void StopShaken()
		{
			if (_shakeListener != null)
			{
				GetSensorManager().UnregisterListener(_shakeListener, _accelerometer);
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
					e.Timestamp.SensorTimestampToDateTimeOffset());

				_lastTimestamp = e.Timestamp;
				_accelerometer.OnReadingChanged(reading);
			}
		}

		class ShakeListener : Java.Lang.Object, ISensorEventListener
		{
			private const int MinimumForce = 10;
			private const int MaxPauseBetweenDirectionChange = 200;
			private const int MinDirectionChange = 3;
			private const int MaxDurationOfShake = 400;

			private readonly Accelerometer _accelerometer;

			private long _firstDirectionChangeTime = 0;
			private long _lastDirectionChangeTime = 0;
			private int _directionChangeCount = 0;
			private float _lastX = 0;
			private float _lastY = 0;
			private float _lastZ = 0;
			

			public ShakeListener(Accelerometer accelerometer)
			{
				_accelerometer = accelerometer;
			}

			public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			public void OnSensorChanged(SensorEvent e)
			{
				var x = e.Values[0];
				var y = e.Values[1];
				var z = e.Values[2];

				// calculate movement
				float totalMovement = Math.Abs(x + y + z - _lastX - _lastY - _lastZ);

				if (totalMovement > MinimumForce)
				{

					// get time
					var now = JavaSystem.CurrentTimeMillis();

					// store first movement time
					if (_firstDirectionChangeTime == 0)
					{
						_firstDirectionChangeTime = now;
						_lastDirectionChangeTime = now;
					}

					// check if the last movement was not long ago
					var lastChangeWasAgo = now - _lastDirectionChangeTime;
					if (lastChangeWasAgo < MaxPauseBetweenDirectionChange)
					{

						// store movement data
						_lastDirectionChangeTime = now;
						_directionChangeCount++;

						// store last sensor data 
						_lastX = x;
						_lastY = y;
						_lastZ = z;

						// check how many movements are so far
						if (_directionChangeCount >= MinDirectionChange)
						{

							// check total duration
							long totalDuration = now - _firstDirectionChangeTime;
							if (totalDuration < MaxDurationOfShake)
							{
								ResetShake();
								_accelerometer.OnShaken(e.Timestamp.SensorTimestampToDateTimeOffset());
							}
						}

					}
					else
					{
						ResetShake();
					}
				}
			}

			private void ResetShake()
			{
				_firstDirectionChangeTime = 0;
				_directionChangeCount = 0;
				_lastDirectionChangeTime = 0;
				_lastX = 0;
				_lastY = 0;
				_lastZ = 0;
			}
		}
	}
}
#endif
