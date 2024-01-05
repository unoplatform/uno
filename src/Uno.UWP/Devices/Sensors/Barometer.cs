#if __ANDROID__ || __IOS__

using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides an interface for a barometric sensor to measure atmospheric pressure.
	/// </summary>
	public partial class Barometer
	{
		private static readonly object _syncLock = new object();
		private static bool _initializationAttempted;
		private static Barometer? _instance;

		private TypedEventHandler<Barometer, BarometerReadingChangedEventArgs>? _readingChanged;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private Barometer()
		{
		}

		/// <summary>
		/// Returns the default barometer sensor.
		/// </summary>
		/// <returns>If no barometer sensor is available, this method will return null.</returns>
		public static Barometer? GetDefault()
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

		/// <summary>
		/// Occurs each time the barometer reports a new sensor reading.
		/// </summary>
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
