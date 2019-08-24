#if __IOS__
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			
			var magnetometerReading = new GyrometerReading(
				(float)data.RotationRate.x,
				(float)data.RotationRate.y,
				(float)data.RotationRate.z,	
				SensorHelpers.TimestampToDateTimeOffset(data.Timestamp));

			OnReadingChanged(magnetometerReading);
		}
	}
}
#endif
