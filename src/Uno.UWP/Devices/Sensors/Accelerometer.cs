#if __IOS__ || __ANDROID__ || __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private readonly static object _syncLock = new object();

		private static Accelerometer _instance;
		private static bool _initializationAttempted;

		private Foundation.TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs> _readingChanged;
		private Foundation.TypedEventHandler<Accelerometer, AccelerometerShakenEventArgs> _shaken;

		/// <summary>
		/// Gets or sets the transformation that needs to be applied to sensor data. Transformations to be applied are tied to the display orientation with which to align the sensor data.
		/// </summary>
		/// <remarks>
		/// This is not currently implemented, and acts as if <see cref="ReadingTransform" /> was set to <see cref="Graphics.Display.DisplayOrientations.Portrait" />.
		/// </remarks>
		[Uno.NotImplemented]
		public Graphics.Display.DisplayOrientations ReadingTransform { get; set; } = Graphics.Display.DisplayOrientations.Portrait;

		public static Accelerometer GetDefault()
		{
			if (_initializationAttempted)
			{
				return _instance;
			}
			lock (_syncLock)
			{
				if (!_initializationAttempted)
				{
					_instance = TryCreateInstance();
					_initializationAttempted = true;
				}
				return _instance;
			}
		}

		public event Foundation.TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs> ReadingChanged
		{			
			add
			{
				lock (_syncLock)
				{
					bool isFirstSubscriber = _readingChanged == null;
					_readingChanged += value;
					if ( isFirstSubscriber)
					{
						StartReadingChanged();
					}
				}
			}			
			remove
			{
				lock (_syncLock)
				{
					_readingChanged -= value;
					if (_readingChanged == null)
					{
						StopReadingChanged();
					}
				}
			}
		}

		public event Foundation.TypedEventHandler<Accelerometer, AccelerometerShakenEventArgs> Shaken
		{
			add
			{
				lock (_syncLock)
				{
					bool isFirstSubscriber = _shaken == null;
					_shaken += value;
					if (isFirstSubscriber)
					{
						StartShaken();
					}
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_shaken -= value;
					if (_shaken == null)
					{
						StopShaken();
					}
				}
			}
		}

		private void OnReadingChanged(AccelerometerReading reading)
		{
			_readingChanged?.Invoke(this, new AccelerometerReadingChangedEventArgs(reading));
		}

		internal void OnShaken(DateTimeOffset timestamp)
		{
			_shaken?.Invoke(this, new AccelerometerShakenEventArgs(timestamp));
		}
	}
}
#endif
