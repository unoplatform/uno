#nullable enable

using Android.Hardware;
using Android.Runtime;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private Sensor? _accelerometer;

		private ReadingChangedListener? _readingChangedListener;
		private ShakeListener? _shakeListener;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		/// <summary>
		/// Gets or sets the current report interval for the accelerometer.
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
						StopReadingChanged();
						StartReadingChanged();
					}
				}
			}
		}

		private static Accelerometer? TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var accelerometer = sensorManager?.GetDefaultSensor(Android.Hardware.SensorType.Accelerometer);

			return accelerometer == null ? null : new Accelerometer();
		}

		private void StartReadingChanged()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			_accelerometer ??= sensorManager?.GetDefaultSensor(Android.Hardware.SensorType.Accelerometer);

			if (_accelerometer != null)
			{
				_readingChangedListener = new ReadingChangedListener(this);
				SensorHelpers.GetSensorManager().RegisterListener(
					_readingChangedListener,
					_accelerometer,
					(SensorDelay)(_reportInterval * 1000));
			}
		}

		private void StopReadingChanged()
		{
			if (_readingChangedListener != null)
			{
				SensorHelpers.GetSensorManager().UnregisterListener(_readingChangedListener, _accelerometer);
				_readingChangedListener.Dispose();
				_readingChangedListener = null;
			}

			if (_shakeListener == null)
			{
				_accelerometer?.Dispose();
				_accelerometer = null;
			}
		}

		private void StartShaken()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			_accelerometer ??= sensorManager?.GetDefaultSensor(Android.Hardware.SensorType.Accelerometer);

			_shakeListener = new ShakeListener(this);
			SensorHelpers.GetSensorManager().RegisterListener(
				_shakeListener,
				_accelerometer,
				(SensorDelay)100000);
		}

		private void StopShaken()
		{
			if (_shakeListener != null)
			{
				SensorHelpers.GetSensorManager().UnregisterListener(_shakeListener, _accelerometer);
				_shakeListener.Dispose();
				_shakeListener = null;
			}

			if (_readingChangedListener == null)
			{
				_accelerometer?.Dispose();
				_accelerometer = null;
			}
		}

		class ReadingChangedListener : Java.Lang.Object, ISensorEventListener
		{
			private readonly Accelerometer _accelerometer;

			public ReadingChangedListener(Accelerometer accelerometer)
			{
				_accelerometer = accelerometer;
			}

			public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			public void OnSensorChanged(SensorEvent? e)
			{
				if (e?.Values is not { } values)
				{
					return;
				}

				var reading = new AccelerometerReading(
					values[0] / SensorManager.GravityEarth * -1,
					values[1] / SensorManager.GravityEarth * -1,
					values[2] / SensorManager.GravityEarth * -1,
					SensorHelpers.TimestampToDateTimeOffset(e.Timestamp));

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

			public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			public void OnSensorChanged(SensorEvent? e)
			{
				if (e?.Values is not { } values)
				{
					return;
				}

				var x = values[0];
				var y = values[1];
				var z = values[2];

				_shakeDetector.OnSensorChanged(x, y, z, SensorHelpers.TimestampToDateTimeOffset(e.Timestamp));
			}
		}
	}
}
