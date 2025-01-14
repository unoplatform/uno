#nullable enable

using System;
using Android.Hardware;
using Android.Runtime;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Magnetometer
	{
		private Sensor? _sensor;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		private MagnetometerListener? _listener;

		/// <summary>
		/// Gets or sets the current report interval for the magnetometer.
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

		private static Magnetometer? TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.MagneticField);

			return sensor == null ? null : new();
		}

		private void StartReading()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			_sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.MagneticField);

			_listener = new MagnetometerListener(this);
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

		private class MagnetometerListener : Java.Lang.Object, ISensorEventListener, IDisposable
		{
			private readonly Magnetometer _magnetometer;
			private SensorStatus? _lastAccuracy;

			public MagnetometerListener(Magnetometer magnetometer)
			{
				_magnetometer = magnetometer;
			}

			void ISensorEventListener.OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy) =>
				_lastAccuracy = accuracy;

			void ISensorEventListener.OnSensorChanged(SensorEvent? e)
			{
				if (e?.Values is not { } values)
				{
					return;
				}

				var magnetometerReading = new MagnetometerReading(
					values[0],
					values[1],
					values[2],
					SensorStatusToAccuracy(),
					SensorHelpers.TimestampToDateTimeOffset(e.Timestamp)
				);
				_magnetometer.OnReadingChanged(magnetometerReading);
			}

			private MagnetometerAccuracy SensorStatusToAccuracy()
			{
				if (_lastAccuracy == null)
				{
					return MagnetometerAccuracy.Unknown;
				}
				switch (_lastAccuracy.Value)
				{
					case SensorStatus.AccuracyHigh:
						return MagnetometerAccuracy.High;
					case SensorStatus.AccuracyLow:
					case SensorStatus.AccuracyMedium:
						return MagnetometerAccuracy.Approximate;
					case SensorStatus.NoContact:
					case SensorStatus.Unreliable:
					default:
						return MagnetometerAccuracy.Unreliable;
				}
			}
		}
	}
}
