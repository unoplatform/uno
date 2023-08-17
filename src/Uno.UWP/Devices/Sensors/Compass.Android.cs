using System;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Locations;
using Uno.Devices.Sensors.Helpers;
using Windows.Devices.Geolocation;
using Windows.Extensions;

namespace Windows.Devices.Sensors;

public partial class Compass
{
	private readonly Sensor _accelerometer;
	private readonly Sensor _magnetometer;

	private static bool isLocationAccessDeclared;

	private SensorListener _listener;
	private uint _reportInterval = SensorHelpers.UiReportingInterval;

	private Compass(Sensor accelerometer, Sensor magnetometer)
	{
		_accelerometer = accelerometer;
		_magnetometer = magnetometer;
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

	private static Compass TryCreateInstance()
	{
		var sensorManager = SensorHelpers.GetSensorManager();
		var accelerometer = sensorManager?.GetDefaultSensor(Android.Hardware.SensorType.Accelerometer);
		var magnetometer = sensorManager?.GetDefaultSensor(Android.Hardware.SensorType.MagneticField);

		isLocationAccessDeclared = PermissionsHelper.IsDeclaredInManifest(Manifest.Permission.AccessFineLocation);

		if (accelerometer != null && magnetometer != null)
		{
			return new Compass(accelerometer, magnetometer);
		}

		return null;
	}

	private void StartReadingChanged()
	{
		_listener = new SensorListener(this, _accelerometer.Name, _magnetometer.Name);
		SensorHelpers.GetSensorManager().RegisterListener(
			_listener,
			_accelerometer,
			(SensorDelay)(_reportInterval * 1000));

		SensorHelpers.GetSensorManager().RegisterListener(
			_listener,
			_magnetometer,
			(SensorDelay)(_reportInterval * 1000));
	}

	private void StopReadingChanged()
	{
		if (_listener == null)
		{
			return;
		}

		SensorHelpers.GetSensorManager().UnregisterListener(_listener, _accelerometer);
		SensorHelpers.GetSensorManager().UnregisterListener(_listener, _magnetometer);

		_listener.Dispose();
		_listener = null;
	}

	class SensorListener : Java.Lang.Object, ISensorEventListener
	{
		private readonly float[] _lastAccelerometer = new float[3];
		private readonly float[] _lastMagnetometer = new float[3];
		private bool _lastAccelerometerSet;
		private bool _lastMagnetometerSet;
		private readonly float[] _r = new float[9];
		private readonly float[] _orientation = new float[3];

		private readonly string _magnetometer;
		private readonly string _accelerometer;

		private readonly Compass _compass;
		private Geolocator _geolocator;

		internal SensorListener(Compass compass, string accelerometer, string magnetometer)
		{
			_compass = compass;
			_magnetometer = magnetometer;
			_accelerometer = accelerometer;

			GetGeolocatorAsync();
		}

		internal async void GetGeolocatorAsync() 
		{
			if (isLocationAccessDeclared)
			{
				var accessStatus = await Geolocator.RequestAccessAsync();

				if (accessStatus is GeolocationAccessStatus.Allowed)
				{
					_geolocator = new Geolocator();
				}
			}
		}

		void ISensorEventListener.OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
		{
		}

		async void ISensorEventListener.OnSensorChanged(SensorEvent e)
		{
			if (e.Sensor.Name == _accelerometer && !_lastAccelerometerSet)
			{
				e.Values.CopyTo(_lastAccelerometer, 0);
				_lastAccelerometerSet = true;
			}
			else
			if (e.Sensor.Name == _magnetometer && !_lastMagnetometerSet)
			{
				e.Values.CopyTo(_lastMagnetometer, 0);
				_lastMagnetometerSet = true;
			}

			if (_lastAccelerometerSet && _lastMagnetometerSet)
			{
				SensorManager.GetRotationMatrix(_r, null, _lastAccelerometer, _lastMagnetometer);
				SensorManager.GetOrientation(_r, _orientation);

				var azimuthInRadians = _orientation[0];
				var magneticNorth = (Java.Lang.Math.ToDegrees(azimuthInRadians) + 360.0) % 360.0;

				var trueNorth = double.NaN;

				if (_geolocator is not null)
				{
					var geoposition = await _geolocator.GetGeopositionAsync();

					var geomagneticField = new GeomagneticField(
						(float)geoposition.Coordinate.Point.Position.Latitude,
						(float)geoposition.Coordinate.Point.Position.Longitude,
						(float)geoposition.Coordinate.Point.Position.Altitude,
						0);

					trueNorth = (magneticNorth + geomagneticField.Declination);
				}

				var data = new CompassReading(
					magneticNorth,
					trueNorth,
					SensorHelpers.TimestampToDateTimeOffset(e.Timestamp));

				_compass.OnReadingChanged(data);

				_lastMagnetometerSet = false;
				_lastAccelerometerSet = false;
			}
		}
	}
}
