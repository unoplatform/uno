#nullable enable

using CoreMotion;
using Foundation;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Magnetometer
	{
		private CMMotionManager? _motionManager;

		private uint _reportInterval;

		/// <summary>
		/// Gets or sets the current report interval for the magnetometer.
		/// </summary>
		public uint ReportInterval
		{
			get
			{
				if (_motionManager != null)
				{
					return (uint)_motionManager.MagnetometerUpdateInterval * 1000;
				}

				return _reportInterval;
			}
			set
			{
				_reportInterval = value;
				if (_motionManager != null)
				{
					_motionManager.MagnetometerUpdateInterval = UpdateMagnetometer(value);
				}
			}
		}

		private static Magnetometer? TryCreateInstance()
		{
			var motionManager = new CMMotionManager();
			return !motionManager.GyroAvailable ?
				null :
				new Magnetometer();
		}

		private void StartReading()
		{
			_motionManager ??= new();

			_motionManager.MagnetometerUpdateInterval = UpdateMagnetometer(_reportInterval);
			_motionManager.StartMagnetometerUpdates(new NSOperationQueue(), MagnetometerUpdateReceived);
		}

		private void StopReading()
		{
			if (_motionManager == null)
			{
				return;
			}

			_motionManager.StopMagnetometerUpdates();
			_motionManager.Dispose();
			_motionManager = null;
		}

		private double UpdateMagnetometer(uint value) => value / 1000.0;

		private void MagnetometerUpdateReceived(CMMagnetometerData? data, NSError? error)
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
