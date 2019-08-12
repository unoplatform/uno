#if __ANDROID__ || __IOS__

using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private static readonly object _syncLock = new object();
		private static bool _initializationAttempted = false;
		private static Barometer _instance = null;	

		private TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> _readingChanged;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private Barometer()
		{
		}

		public static Barometer GetDefault()
		{
			if (_instance == null && !_initializationAttempted)
			{
				_instance = TryCreateInstance();
				_initializationAttempted = true;
			}
			return _instance;
		}

		public event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> ReadingChanged
		{
			add
			{
				lock (_syncLock)
				{
					bool isFirstSubscriber = _readingChanged == null;
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
	}
}
#endif
