#nullable enable

using System;
using CoreMotion;
using Foundation;
using UIKit;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private CMMotionManager? _motionManager;

		private uint _reportInterval;

		/// <summary>
		/// Gets or sets the current report interval for the accelerometer.
		/// </summary>
		public uint ReportInterval
		{
			get
			{
				if (_motionManager != null)
				{
					return (uint)_motionManager.AccelerometerUpdateInterval * 1000;
				}

				return _reportInterval;
			}
			set
			{
				_reportInterval = value;
				if (_motionManager != null)
				{
					_motionManager.AccelerometerUpdateInterval = UpdateAccelerometer(value);
				}
			}
		}

		internal static void HandleShake()
		{
			_instance.Value?.OnShaken(DateTimeOffset.UtcNow);
		}

		private static Accelerometer? TryCreateInstance()
		{
			var motionManager = new CMMotionManager();
			return !motionManager.AccelerometerAvailable ?
				null :
				new Accelerometer();
		}

		private void StartReadingChanged()
		{
			_motionManager ??= new();

			_motionManager.AccelerometerUpdateInterval = UpdateAccelerometer(_reportInterval);
			_motionManager.StartAccelerometerUpdates(new NSOperationQueue(), AccelerometerDataReceived);
		}

		private void StopReadingChanged()
		{
			if (_motionManager == null)
			{
				return;
			}

			_motionManager.StopAccelerometerUpdates();

			_motionManager.Dispose();
			_motionManager = null;
		}

		private void StartShaken()
		{
			UIApplication.SharedApplication.ApplicationSupportsShakeToEdit = true;
		}

		private void StopShaken()
		{
			UIApplication.SharedApplication.ApplicationSupportsShakeToEdit = false;
		}

		private double UpdateAccelerometer(uint value) => value / 1000.0;

		private void AccelerometerDataReceived(CMAccelerometerData? data, NSError? error)
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
