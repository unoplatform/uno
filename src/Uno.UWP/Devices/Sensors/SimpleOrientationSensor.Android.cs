#if __ANDROID__
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Runtime;

namespace Windows.Devices.Sensors
{
	public partial class SimpleOrientationSensor : Java.Lang.Object, ISensorEventListener
	{
		private SensorManager _sensorManager;
		// Threshold, in meters per second squared, closely equivalent to an angle of 25 degrees which correspond to the value when Android detect new screen orientation 
		private const double _threshold = 4.55;
		private const Android.Hardware.SensorType _sensorType = Android.Hardware.SensorType.Gravity;

		partial void Initialize()
		{
			_sensorManager = (SensorManager)Application.Context.GetSystemService(Context.SensorService);
			_sensorManager.RegisterListener(this, _sensorManager.GetDefaultSensor(_sensorType), SensorDelay.Normal);
		}

		public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
		{
		}

		public void OnSensorChanged(SensorEvent e)
		{
			if (e.Sensor.Type != _sensorType)
			{
				return;
			}

			// All units are negatives compared to iOS : https://developer.android.com/reference/android/hardware/SensorEvent#values
			var gravityX = -(double)e.Values[0];
			var gravityY = -(double)e.Values[1];
			var gravityZ = -(double)e.Values[2];

			var simpleOrientation = ToSimpleOrientation(gravityX, gravityY, gravityZ, _threshold, _currentOrientation);
			SetCurrentOrientation(simpleOrientation);
		}
	}
}
#endif