#if __ANDROID__ || __IOS__

using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private static bool _initializationAttempted = false;
		private static Barometer _instance = null;
		private static int _activeSubscribers = 0;

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
				_activeSubscribers++;
				_readingChanged += value;
				if (_activeSubscribers == 1)
				{
					StartReading();
				}
			}
			remove
			{
				_readingChanged -= value;
				_activeSubscribers--;
				if (_activeSubscribers == 0)
				{
					StopReading();
				}
			}
		}
	}
}
#endif
