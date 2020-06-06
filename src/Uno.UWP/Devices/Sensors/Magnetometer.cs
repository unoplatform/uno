#if __IOS__ || __ANDROID__ || __WASM__
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Magnetometer
	{
		private readonly static object _syncLock = new object();

		private static Magnetometer _instance;
		private static bool _initializationAttempted;

		private StartStopEventWrapper<TypedEventHandler<Magnetometer, MagnetometerReadingChangedEventArgs>> _readingChangedWrapper;

		private Magnetometer()
		{
			_readingChangedWrapper = new StartStopEventWrapper<TypedEventHandler<Magnetometer, MagnetometerReadingChangedEventArgs>>(
				() => StartReading(),
				() => StopReading(),
				_syncLock);
		}

		public static Magnetometer GetDefault()
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

		public event TypedEventHandler<Magnetometer, MagnetometerReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}

		private void OnReadingChanged(MagnetometerReading reading)
		{
			_readingChangedWrapper.Event?.Invoke(this, new MagnetometerReadingChangedEventArgs(reading));
		}
	}
}
#endif
