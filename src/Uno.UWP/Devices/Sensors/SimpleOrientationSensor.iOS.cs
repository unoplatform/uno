#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using CoreMotion;
using Foundation;
using UIKit;
using Uno.Extensions;
using Uno.Logging;
using Windows.Graphics.Display;
namespace Windows.Devices.Sensors
{
	public partial class SimpleOrientationSensor
	{
		private SimpleOrientation _previousOrientation;
		private static CMMotionManager _motionManager;
		private const double _updateInterval = 0.5;
		private const double _threshold = 0.5;
		partial void Initialize()
		{
			_motionManager = new CMMotionManager();
			if (_motionManager.DeviceMotionAvailable) // DeviceMotion is not available on all devices. iOS4+
			{
				var operationQueue = (NSOperationQueue.CurrentQueue == null || NSOperationQueue.CurrentQueue == NSOperationQueue.MainQueue) ? new NSOperationQueue() : NSOperationQueue.CurrentQueue;
				this.Log().ErrorIfEnabled(() => "DeviceMotion is available");
				_motionManager.DeviceMotionUpdateInterval = _updateInterval;
				_motionManager.StartDeviceMotionUpdates(operationQueue, (motion, error) =>
				{
					OnMotionChanged(motion);
				});
			}
			else // For iOS devices that don't support CoreMotion
			{
				this.Log().ErrorIfEnabled(() => "SimpleOrientationSensor failed to initialize because CoreMotion is not available");
			}
		}
		private void OnMotionChanged(CMDeviceMotion motion)
		{
			var orientation = ToSimpleOrientation(motion.Gravity.X, motion.Gravity.Y, motion.Gravity.Z, _previousOrientation);
			_previousOrientation = orientation;
			SetCurrentOrientation(orientation);
		}
		private static SimpleOrientation ToSimpleOrientation(double gravityX, double gravityY, double gravityZ, SimpleOrientation previous)
		{
			// Ensures orientation only changes when within close range to new orientation.
			if (Math.Abs(gravityX) > Math.Abs(gravityY) + _threshold && Math.Abs(gravityX) > Math.Abs(gravityZ) + _threshold)
			{
				if (gravityX > 0)
				{
					return SimpleOrientation.Rotated270DegreesCounterclockwise;
				}

				return SimpleOrientation.Rotated90DegreesCounterclockwise;

			}
			else if (Math.Abs(gravityY) > Math.Abs(gravityX) + _threshold && Math.Abs(gravityY) > Math.Abs(gravityZ) + _threshold)
			{
				if (gravityY >= 0)
				{
					return SimpleOrientation.Rotated180DegreesCounterclockwise;
				}

				return SimpleOrientation.NotRotated;

			}
			else if (Math.Abs(gravityZ) > Math.Abs(gravityY) + _threshold && Math.Abs(gravityZ) > Math.Abs(gravityX) + _threshold)
			{
				if (gravityZ >= 0)
				{
					return SimpleOrientation.Facedown;
				}
				return SimpleOrientation.Faceup;

			}
			return previous;
		}
	}
}
#endif