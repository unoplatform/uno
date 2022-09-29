#nullable disable

using Android.App;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using System;
using System.Linq;
using Uno.Devices.Sensors;
using Uno.Devices.Sensors.Helpers;

namespace Uno.UI.Foldable
{
	/// <summary>
	/// Uses sensor manager to listen for hinge or fold angle changes
	/// </summary>
	/// <remarks>
	/// Some code from 
	/// https://github.com/xamarin/XamarinComponents/blob/main/Android/SurfaceDuo/source/SurfaceDuo/Additions/HingeSensor.cs
	/// 
	/// Choosing the sensor by "name contains 'hinge'" is a little rough - 
	/// Android 30 and higher introduces a hinge sensor constant `android.sensor.hinge_angle`
	/// https://developer.android.com/reference/android/hardware/Sensor#STRING_TYPE_HINGE_ANGLE
	///
	/// Reference for other implementations:
	/// 
	/// const string HINGE_SENSOR_TYPE = "microsoft.sensor.hinge_angle"; // Surface Duo specific
	///	const string HINGE_SENSOR_TYPE = "android.sensor.hinge_angle"; // API 30 - future use
	///
	/// _hingeSensor = sensors.FirstOrDefault(s => s.StringType.Equals(HINGE_SENSOR_TYPE, StringComparison.OrdinalIgnoreCase)); // TYPE - Surface Duo specific
	/// </remarks>
	public class FoldableHingeAngleSensor : INativeHingeAngleSensor
    {
		const string HINGE_SENSOR_NAME = "hinge"; // works on multiple OEM devices

		private static SensorManager _sensorManager;
	
		Sensor _hingeSensor;
		HingeSensorEventListener _sensorListener;

		private EventHandler<NativeHingeAngleReading> _readingChanged;

		public bool DeviceHasHinge { 
			get {
				return _hingeSensor != null;
			}
		}

		public FoldableHingeAngleSensor(object owner)
		{
			_hingeSensor = GetHingeSensor();
		}

		internal static bool HasHinge => GetHingeSensor() is not null;

		private static Sensor GetHingeSensor()
		{
			if (!(ContextHelper.Current is Activity currentActivity))
			{
				throw new InvalidOperationException("FoldableHingeAngleSensor must be initialized on the UI Thread");
			}

			_sensorManager ??= SensorManager.FromContext(currentActivity);

			if (_sensorManager is null)
			{
				return null;
			}
			
			var sensors = _sensorManager.GetSensorList(Android.Hardware.SensorType.All);

			return sensors.FirstOrDefault(s => s.Name.Contains(HINGE_SENSOR_NAME, StringComparison.OrdinalIgnoreCase)); // NAME - generic foldable device/s
		}

		public event EventHandler<NativeHingeAngleReading> ReadingChanged
		{
			add
			{
				var isFirstSubscriber = _readingChanged == null;
				_readingChanged += value;
				if (isFirstSubscriber)
				{
					StartListening();
				}
			}
			remove
			{
				_readingChanged -= value;
				if (_readingChanged == null)
				{
					StopListening();
				}
			}
		}

		public void StartListening()
		{
			if (_sensorManager != null && _hingeSensor != null)
			{
				if (_sensorListener == null)
				{
					_sensorListener = new HingeSensorEventListener
					{
						SensorChangedHandler = se =>
						{
							if (se.Sensor == _hingeSensor)
								_readingChanged?.Invoke(_hingeSensor,
									new NativeHingeAngleReading(se.Values[0], TimestampToDateTimeOffset(se.Timestamp)));
						}
					};
				}

				_sensorManager.RegisterListener(_sensorListener, _hingeSensor, SensorDelay.Normal);
			}
		}

		public void StopListening()
		{
			if (_sensorManager != null && _hingeSensor != null)
				_sensorManager.UnregisterListener(_sensorListener, _hingeSensor);
		}

        class HingeSensorEventListener : Java.Lang.Object, ISensorEventListener
        {
            public Action<SensorEvent> SensorChangedHandler { get; set; }
            public Action<Sensor, SensorStatus> AccuracyChangedHandler { get; set; }

            public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
                => AccuracyChangedHandler?.Invoke(sensor, accuracy);

            public void OnSensorChanged(SensorEvent e)
                => SensorChangedHandler?.Invoke(e);
        }

        static DateTimeOffset TimestampToDateTimeOffset(long timestamp)
		{
			return DateTimeOffset.Now
				.AddMilliseconds(-SystemClock.ElapsedRealtime())
				.AddMilliseconds(timestamp / 1000000.0);
		}
	}
}
