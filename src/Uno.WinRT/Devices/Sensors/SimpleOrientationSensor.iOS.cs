#nullable enable
using CoreMotion;
using Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Windows.Devices.Sensors
{
	public partial class SimpleOrientationSensor
	{
		private SimpleOrientation _previousOrientation;
		private static CMMotionManager? _motionManager;
		private const double _updateInterval = 0.5;
		private const double _threshold = 0.5;

		private static partial SimpleOrientationSensor? TryCreateInstance()
		{
			_motionManager ??= new CMMotionManager();

			return _motionManager.DeviceMotionAvailable
				? new SimpleOrientationSensor()
				: null;
		}

		partial void StartListeningOrientationChanged()
		{
			_motionManager ??= new();

			if (!_motionManager.DeviceMotionAvailable)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("SimpleOrientationSensor failed to Start. CoreMotion is not available");
				}
			}

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info("DeviceMotion is available");
			}

			var operationQueue = (NSOperationQueue.CurrentQueue == null || NSOperationQueue.CurrentQueue == NSOperationQueue.MainQueue)
				? new NSOperationQueue()
				: NSOperationQueue.CurrentQueue;

			_motionManager.DeviceMotionUpdateInterval = _updateInterval;

			_motionManager.StartDeviceMotionUpdates(operationQueue, OnMotionChanged);
		}

		partial void StopListeningOrientationChanged()
		{
			if (_motionManager is null)
			{
				return;
			}

			_motionManager.StopDeviceMotionUpdates();
			_motionManager.Dispose();
			_motionManager = null;
		}

		partial void Initialize()
		{
			if (_motionManager is { DeviceMotionAvailable: true })
			{
				// We should not start more than one CMMotionManager to avoid performance hit.
				_motionManager.StopDeviceMotionUpdates();
			}

			_motionManager = new CMMotionManager();
			if (_motionManager.DeviceMotionAvailable) // DeviceMotion is not available on all devices. iOS4+
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info("DeviceMotion is available");
				}

				var operationQueue = (NSOperationQueue.CurrentQueue == null || NSOperationQueue.CurrentQueue == NSOperationQueue.MainQueue) ? new NSOperationQueue() : NSOperationQueue.CurrentQueue;

				_motionManager.DeviceMotionUpdateInterval = _updateInterval;

				_motionManager.StartDeviceMotionUpdates(operationQueue, (motion, error) =>
				{
					// Motion and Error can be null: https://developer.apple.com/documentation/coremotion/cmdevicemotionhandler
					if (error is not null)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error($"SimpleOrientationSensor returned error when reading Device Motion updates. {error.Description}");
						}

						return;
					}

					if (motion is null)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error($"SimpleOrientationSensor failed read Device Motion updates.");
						}

						return;
					}

					OnMotionChanged(motion, error);
				});
			}
			else // For iOS devices that don't support CoreMotion
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("SimpleOrientationSensor failed to initialize because CoreMotion is not available");
				}
			}
		}

		private void OnMotionChanged(CMDeviceMotion? motion, NSError? error)
		{
			// Motion and Error can be null: https://developer.apple.com/documentation/coremotion/cmdevicemotionhandler
			if (error is not null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"SimpleOrientationSensor returned error when reading Device Motion updates. {error.Description}");
				}

				return;
			}

			if (motion is null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"SimpleOrientationSensor failed read Device Motion updates.");
				}

				return;
			}

			var orientation = ToSimpleOrientation(motion.Gravity.X, motion.Gravity.Y, motion.Gravity.Z, _threshold, _previousOrientation);

			_previousOrientation = orientation;
			SetCurrentOrientation(orientation);
		}
	}
}
