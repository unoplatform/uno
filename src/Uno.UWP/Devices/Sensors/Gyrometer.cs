#if __IOS__ || __ANDROID__ || __WASM__
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public  partial class Gyrometer 
	{
		private readonly static object _syncLock = new object();

		private static Gyrometer _instance;
		private static bool _initializationAttempted;

		private TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs> _readingChanged;

		public static Gyrometer GetDefault()
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

		public event TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs> ReadingChanged
		{
			add
			{
				lock (_syncLock)
				{
					var isFirstSubscriber = _readingChanged == null;
					_readingChanged += value;
					if (isFirstSubscriber)
					{
						StartReading();
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
						StopReading();
					}
				}
			}
		}

		private void OnReadingChanged(GyrometerReading reading)
		{
			_readingChanged?.Invoke(this, new GyrometerReadingChangedEventArgs(reading));
		}
	}
}
#endif
