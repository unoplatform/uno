#nullable disable

#if __IOS__
using CoreMotion;
using Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;

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
				this.Log().Error("DeviceMotion is available");
				_motionManager.DeviceMotionUpdateInterval = _updateInterval;
				_motionManager.StartDeviceMotionUpdates(operationQueue, (motion, error) =>
				{
					OnMotionChanged(motion);
				});
			}
			else // For iOS devices that don't support CoreMotion
			{
				this.Log().Error("SimpleOrientationSensor failed to initialize because CoreMotion is not available");
			}
		}

		private void OnMotionChanged(CMDeviceMotion motion)
		{
			var orientation = ToSimpleOrientation(motion.Gravity.X, motion.Gravity.Y, motion.Gravity.Z, _threshold, _previousOrientation);
			_previousOrientation = orientation;
			SetCurrentOrientation(orientation);
		}
	}
}
#endif
