#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using CoreMotion;
using Foundation;
using UIKit;
using Windows.Extensions;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private readonly CMMotionManager _motionManager;

		private Accelerometer(CMMotionManager motionManager)
		{
			_motionManager = motionManager;
		}

		public uint ReportInterval
		{
			get => (uint)_motionManager.AccelerometerUpdateInterval * 1000;
			set
			{
				_motionManager.AccelerometerUpdateInterval = value / 1000.0;
			}
		}

		internal static void HandleShake()
		{
			if (_instance != null)
			{
				_instance.OnShaken(DateTimeOffset.UtcNow);
			}
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

		private void StartReadingChanged()
		{
			_motionManager.StartAccelerometerUpdates(new NSOperationQueue(), AccelerometerDataReceived);
		}

		private void StopReadingChanged()
		{
			_motionManager.StopAccelerometerUpdates();
		}

		private void StartShaken()
		{
			UIApplication.SharedApplication.ApplicationSupportsShakeToEdit = true;
		}

		private void StopShaken()
		{
			UIApplication.SharedApplication.ApplicationSupportsShakeToEdit = false;
		}

		private void AccelerometerDataReceived(CMAccelerometerData data, NSError error)
		{
			if (data == null)
			{
				return;
			}

			var reading = new AccelerometerReading(
				data.Acceleration.X * -1,
				data.Acceleration.Y * -1,
				data.Acceleration.Z * -1,
				data.Timestamp.TimestampToDateTimeOffset());

			OnReadingChanged(reading);
		}
	}
}
#endif
