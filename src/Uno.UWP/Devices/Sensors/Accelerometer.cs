#if __IOS__ || __ANDROID__ || __WASM__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private readonly static object _syncLock = new object();

		private static Accelerometer _instance;
		private static bool _initializationAttempted;

		private StartStopEventWrapper<TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>> _readingChangedWrapper;
		private StartStopEventWrapper<TypedEventHandler<Accelerometer, AccelerometerShakenEventArgs>> _shakenWrapper;

		private Accelerometer()
		{
			_readingChangedWrapper = new StartStopEventWrapper<TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>>(
				() => StartReadingChanged(),
				() => StopReadingChanged(),
				_syncLock);
			_shakenWrapper = new StartStopEventWrapper<TypedEventHandler<Accelerometer, AccelerometerShakenEventArgs>>(
				() => StartShaken(),
				() => StopShaken(),
				_syncLock);

			InitializePlatform();
		}

		partial void InitializePlatform();

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

		public event TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}

		public event TypedEventHandler<Accelerometer, AccelerometerShakenEventArgs> Shaken
		{
			add => _shakenWrapper.AddHandler(value);
			remove => _shakenWrapper.RemoveHandler(value);
		}

		private void OnReadingChanged(AccelerometerReading reading)
		{
			_readingChangedWrapper.Event?.Invoke(this, new AccelerometerReadingChangedEventArgs(reading));
		}

		internal void OnShaken(DateTimeOffset timestamp)
		{
			_shakenWrapper.Event?.Invoke(this, new AccelerometerShakenEventArgs(timestamp));
		}
	}
}
#endif
