#if __IOS__
using System;
using CoreMotion;
using Foundation;
using UIKit;
using Uno.Devices.Sensors.Helpers;

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
			set => _motionManager.AccelerometerUpdateInterval = value / 1000.0;
		}

		internal static void HandleShake()
		{
			_instance?.OnShaken(DateTimeOffset.UtcNow);
		}

		private static Accelerometer TryCreateInstance()
		{
			var motionManager = new CMMotionManager();
			return !motionManager.AccelerometerAvailable ?
				null :
				new Accelerometer(motionManager);
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
				data.Acceleration.X,
				data.Acceleration.Y,
				data.Acceleration.Z,
				SensorHelpers.TimestampToDateTimeOffset(data.Timestamp));

			OnReadingChanged(reading);
		}
	}
}
#endif
