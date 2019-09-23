#if __IOS__
using CoreMotion;
using Foundation;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Gyrometer
	{
		private readonly CMMotionManager _motionManager;

		private Gyrometer(CMMotionManager motionManager)
		{
			_motionManager = motionManager;
		}

		public uint ReportInterval
		{
			get => (uint)_motionManager.GyroUpdateInterval * 1000;
			set => _motionManager.GyroUpdateInterval = value / 1000.0;
		}

		private static Gyrometer TryCreateInstance()
		{
			var motionManager = new CMMotionManager();
			return motionManager.GyroAvailable ?
				new Gyrometer(motionManager) : null;
		}

		private void StartReading()
		{
			_motionManager.StartGyroUpdates(new NSOperationQueue(), GyrometerUpdateReceived);
		}

		private void StopReading()
		{
			_motionManager.StopGyroUpdates();
		}

		private void GyrometerUpdateReceived(CMGyroData data, NSError error)
		{
			if (data == null)
			{
				return;
			}
			
			var gyrometerReading = new GyrometerReading(
				(float)data.RotationRate.x,
				(float)data.RotationRate.y,
				(float)data.RotationRate.z,	
				SensorHelpers.TimestampToDateTimeOffset(data.Timestamp));

			OnReadingChanged(gyrometerReading);
		}
	}
}
#endif
