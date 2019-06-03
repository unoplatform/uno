#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Content;
using Android.Hardware;

namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private readonly Sensor _sensor;

		private Barometer(Sensor barometerSensor)
		{
			_sensor = barometerSensor;
		}

		private static Barometer TryCreateInstance()
		{
			var sensorManager = Application.Context.GetSystemService(Context.SensorService) as SensorManager;
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Pressure);
			if (sensor != null)
			{
				return new Barometer(sensor);
			}
			return null;
		}
	}
}
#endif
