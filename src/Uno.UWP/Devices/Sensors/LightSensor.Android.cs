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
	public partial class LightSensor
	{
		private readonly Sensor _sensor;
		private LightSensorListener _listener;
		private uint _reportInterval = SensorHelpers.UiReportingInterval;

		private LightSensor(Sensor lightSensorSensor) : this()
		{
			_sensor = lightSensorSensor;
		}

		public uint ReportInterval
		{
			get => _reportInterval;
			set
			{
				lock (_readingChangedWrapper.SyncLock)
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

		private static LightSensor TryCreateInstance()
		{
			var sensorManager = SensorHelpers.GetSensorManager();
			var sensor = sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Light);
			if (sensor != null)
			{
				return new LightSensor(sensor);
			}
			return null;
		}

		private void StartReading()
		{
			_listener = new LightSensorListener(this);
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
		}

		private class LightSensorListener : Java.Lang.Object, ISensorEventListener, IDisposable
		{
			private readonly LightSensor _lightSensor;

			public LightSensorListener(LightSensor lightSensor)
			{
				_lightSensor = lightSensor;
			}

			void ISensorEventListener.OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			void ISensorEventListener.OnSensorChanged(SensorEvent e)
			{
				var reading = new LightSensorReading(
					e.Values[0],
					SensorHelpers.TimestampToDateTimeOffset(e.Timestamp));
				LightSensor.OnReadingChanged(reading);
			}
		}
	}
}
