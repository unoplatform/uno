using System;
using Windows.UI.Core;

namespace Windows.Devices.Sensors
{
	public partial class SimpleOrientationSensor
	{
		#region Static

		public static SimpleOrientationSensor _instance;

		public static SimpleOrientationSensor GetDefault()
		{
			if (_instance == null)
			{
				_instance = new SimpleOrientationSensor();
			}

			return _instance;
		}

		#endregion

		private SimpleOrientation _currentOrientation;

		/// <summary>
		/// Represents a simple orientation sensor.
		/// </summary>
		private SimpleOrientationSensor()
		{
			Initialize();
		}

		partial void Initialize();

		/// <summary>
		/// Gets the device identifier.
		/// </summary>
		[Uno.NotImplemented]
		public string DeviceId { get; }

		/// <summary>
		/// Gets or sets the transformation that needs to be applied to sensor data. Transformations to be applied are tied to the display orientation with which to align the sensor data.
		/// </summary>
		/// <remarks>
		/// This is not currently implemented, and acts as if <see cref="ReadingTransform" /> was set to <see cref="Graphics.Display.DisplayOrientations.Portrait" />.
		/// </remarks>
		[Uno.NotImplemented]
		public Graphics.Display.DisplayOrientations ReadingTransform { get; set; } = Graphics.Display.DisplayOrientations.Portrait;

		/// <summary>
		/// Gets the default simple orientation sensor.
		/// </summary>
		/// <returns></returns>
		public SimpleOrientation GetCurrentOrientation() => _currentOrientation;

		/// <summary>
		/// Occurs each time the simple orientation sensor reports a new sensor reading.
		/// </summary>
		public event Foundation.TypedEventHandler<SimpleOrientationSensor, SimpleOrientationSensorOrientationChangedEventArgs> OrientationChanged;

		private void SetCurrentOrientation(SimpleOrientation orientation)
		{
			if (CoreDispatcher.Main.HasThreadAccess)
			{
				CalculateCurrentOrientation(orientation);
			}
			else
			{
				CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					CalculateCurrentOrientation(orientation);
				});
			}
		}

		private void CalculateCurrentOrientation(SimpleOrientation orientation)
		{
			if (_currentOrientation != orientation)
			{
				_currentOrientation = orientation;
				var args = new SimpleOrientationSensorOrientationChangedEventArgs()
				{
					Orientation = orientation,
					Timestamp = DateTimeOffset.Now,
				};
				OrientationChanged?.Invoke(this, args);
			}
		}

		private static SimpleOrientation ToSimpleOrientation(double gravityX, double gravityY, double gravityZ, double threshold, SimpleOrientation previous)
		{
			// Ensures orientation only changes when within close range to new orientation.
			if (Math.Abs(gravityX) > Math.Abs(gravityY) + threshold && Math.Abs(gravityX) > Math.Abs(gravityZ) + threshold)
			{
				if (gravityX > 0)
				{
					return SimpleOrientation.Rotated270DegreesCounterclockwise;
				}

				return SimpleOrientation.Rotated90DegreesCounterclockwise;
			}
			else if (Math.Abs(gravityY) > Math.Abs(gravityX) + threshold && Math.Abs(gravityY) > Math.Abs(gravityZ) + threshold)
			{
				if (gravityY > 0)
				{
					return SimpleOrientation.Rotated180DegreesCounterclockwise;
				}

				return SimpleOrientation.NotRotated;
			}
			else if (Math.Abs(gravityZ) > Math.Abs(gravityY) + threshold && Math.Abs(gravityZ) > Math.Abs(gravityX) + threshold)
			{
				if (gravityZ > 0)
				{
					return SimpleOrientation.Facedown;
				}

				return SimpleOrientation.Faceup;
			}

			return previous;
		}
	}
}
