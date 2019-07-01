#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using CoreMotion;
using Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private readonly CMMotionManager _motionManager;

		private Accelerometer(CMMotionManager motionManager)
		{
			_motionManager = motionManager;
		}

		private static Accelerometer TryCreateInstance()
		{
			var motionManager = new CMMotionManager();
			if (!motionManager.AccelerometerAvailable)
			{
				return null;
			}
			else
			{
				return new Accelerometer(motionManager);
			}
		}

		private void AccelerometerDataReceived(CMAccelerometerData data, NSError error)
		{
			if (data == null)
			{
				return;
			}

			var acceleration = data.Acceleration;
			var reading = new AccelerometerReading(
				data.Acceleration.X * -1,
				data.Acceleration.Y * -1,
				data.Acceleration.Z * -1,
				data.Timestamp);
			OnReadingChanged()
		}
	}
}
#endif
