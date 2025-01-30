#nullable enable

using System;
using System.Numerics;

namespace Windows.Devices.Sensors;

public partial class Compass
{
	private Accelerometer? _accelerometer;
	private Magnetometer? _magnetometer;

	private Vector3? _lastAccelerometer;
	private Vector3? _lastMagnetometer;

	private uint _reportInterval;

	/// <summary>
	/// Gets or sets the current report interval for the compass.
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

				if (_accelerometer == null || _magnetometer == null)
				{
					return;
				}

				if (_readingChangedWrapper.Event != null)
				{
					//restart reading to apply interval
					StopReadingChanged();
					StartReadingChanged();
				}
			}
		}
	}

	private static Compass? TryCreateInstance()
	{
		var accelerometer = Accelerometer.GetDefault();
		var magnetometer = Magnetometer.GetDefault();

		if (accelerometer != null && magnetometer != null)
		{
			return new Compass();
		}

		return null;
	}

	private void StartReadingChanged()
	{
		_accelerometer = Accelerometer.GetDefault();
		_magnetometer = Magnetometer.GetDefault();

		if (_accelerometer != null && _magnetometer != null)
		{
			_accelerometer.ReportInterval = _reportInterval;
			_magnetometer.ReportInterval = _reportInterval;

			_accelerometer.ReadingChanged += (_, args) =>
			{
				OnReadingChanged(args, null);
			};

			_magnetometer.ReadingChanged += (_, args) =>
			{
				OnReadingChanged(null, args);
			};
		}
	}

	private void StopReadingChanged()
	{
		if (_accelerometer != null)
		{
			_accelerometer.ReadingChanged -= (_, args) =>
			{
				OnReadingChanged(args, null);
			};
		}

		if (_magnetometer != null)
		{
			_magnetometer.ReadingChanged -= (_, args) =>
			{
				OnReadingChanged(null, args);
			};
		}
	}

	private void OnReadingChanged(AccelerometerReadingChangedEventArgs? accelerometerData, MagnetometerReadingChangedEventArgs? magnetometerData)
	{
		if (accelerometerData?.Reading is { } lastAccelerometerReading)
		{
			_lastAccelerometer = new Vector3(
				(float)lastAccelerometerReading.AccelerationX,
				(float)lastAccelerometerReading.AccelerationY,
				(float)lastAccelerometerReading.AccelerationZ);
		}

		if (magnetometerData?.Reading is { } lastMagnetometerReading)
		{
			_lastMagnetometer = new Vector3(
				(float)lastMagnetometerReading.MagneticFieldX,
				(float)lastMagnetometerReading.MagneticFieldY,
				(float)lastMagnetometerReading.MagneticFieldZ);
		}

		if (_lastAccelerometer == null || _lastMagnetometer == null)
		{
			return;
		}

		var azimuthInRadians = GetAzimuth(_lastAccelerometer.Value, _lastMagnetometer.Value);

		var azimuthInDegrees = ((azimuthInRadians * (180.0 / Math.PI)) + 360.0) % 360.0;

		var data = new CompassReading(
			azimuthInDegrees,
			double.NaN,
			new DateTimeOffset(DateTime.Now));

		OnReadingChanged(data);
	}

	private static float GetAzimuth(Vector3 accelerometer, Vector3 magnetometer)
	{
		var forward = Vector3.Normalize(accelerometer);
		var east = Vector3.Normalize(Vector3.Cross(magnetometer, forward));
		var north = Vector3.Cross(forward, east);

		return (float)Math.Atan2(north.X, east.X);
	}

}
