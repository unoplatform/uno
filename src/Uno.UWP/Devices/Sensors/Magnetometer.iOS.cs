#nullable disable

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
	public partial class Magnetometer
	{
		private readonly CMMotionManager _motionManager;

		private Magnetometer(CMMotionManager motionManager)
		{
			_motionManager = motionManager;
		}

		public uint ReportInterval
		{
			get => (uint)_motionManager.MagnetometerUpdateInterval * 1000;
			set => _motionManager.MagnetometerUpdateInterval = value / 1000.0;
		}

		private static Magnetometer TryCreateInstance()
		{
			var motionManager = new CMMotionManager();
			return motionManager.MagnetometerAvailable ?
				new Magnetometer(motionManager) : null;
		}

		private void StartReading()
		{
			_motionManager.StartMagnetometerUpdates(new NSOperationQueue(), MagnetometerUpdateReceived);
		}

		private void StopReading()
		{
			_motionManager.StopMagnetometerUpdates();
		}

		private void MagnetometerUpdateReceived(CMMagnetometerData data, NSError error)
		{
			if (data == null)
			{
				return;
			}

			var magnetometerReading = new MagnetometerReading(
				(float)data.MagneticField.X,
				(float)data.MagneticField.Y,
				(float)data.MagneticField.Z,
				MagnetometerAccuracy.Unknown, //iOS does not report Magnetometer accuracy
				SensorHelpers.TimestampToDateTimeOffset(data.Timestamp));

			OnReadingChanged(magnetometerReading);
		}
	}
}
#endif
