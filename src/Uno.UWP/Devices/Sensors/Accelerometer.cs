using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private static readonly object _initializaitonLock = new object();
		private readonly static Dictionary<AccelerometerReadingType, Accelerometer> _instances =
			new Dictionary<AccelerometerReadingType, Accelerometer>();
		private readonly static Dictionary<AccelerometerReadingType, bool> _initializationAttempted =
			new Dictionary<AccelerometerReadingType, bool>();

		private readonly object _instanceLock = new object();
		private int _readingChangedSubscribers = 0;

		private Foundation.TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs> _readingChanged;
		private Foundation.TypedEventHandler<Accelerometer, AccelerometerShakenEventArgs> _shaken;

		/// <summary>
		/// Gets or sets the transformation that needs to be applied to sensor data. Transformations to be applied are tied to the display orientation with which to align the sensor data.
		/// </summary>
		/// <remarks>
		/// This is not currently implemented, and acts as if <see cref="ReadingTransform" /> was set to <see cref="DisplayOrientation.Portrait" />.
		/// </remarks>
		[Uno.NotImplemented]
		public Graphics.Display.DisplayOrientations ReadingTransform { get; set; } = Graphics.Display.DisplayOrientations.Portrait;

		public static Accelerometer GetDefault() => CreateAccelerometer(AccelerometerReadingType.Standard);

		private static Accelerometer CreateAccelerometer(AccelerometerReadingType type)
		{
			if (_initializationAttempted[type])
			{
				return _instances[type];
			}
			lock (_initializaitonLock)
			{
				if ( !_initializationAttempted[type])
				{
					_instances[type] = null; //TODO Implement
				}
				return _instances[type];
			}
		}

		public event Foundation.TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs> ReadingChanged
		{			
			add
			{
				lock (_instanceLock)
				{
					_readingChanged += value;
				}
			}			
			remove
			{
				lock (_instanceLock)
				{
					_readingChanged -= value;
					if (_readingChanged == null)
					{

					}
				}
			}
		}

		public event Foundation.TypedEventHandler<Accelerometer, AccelerometerShakenEventArgs> Shaken
		{
			add
			{
				lock (_instanceLock)
				{
					_shaken += value;
				}
			}
			remove
			{
				lock (_instanceLock)
				{
					_shaken -= value;
					if (_shaken == null)
					{

					}
				}
			}
		}
	}
}
