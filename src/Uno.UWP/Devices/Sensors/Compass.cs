using Windows.Foundation;

namespace Windows.Devices.Sensors;

public partial class Compass
{
	private readonly static object _syncLock = new();

	private static Compass _instance;
	private static bool _initializationAttempted;

	private TypedEventHandler<Compass, CompassReadingChangedEventArgs> _readingChanged;

	public static Compass GetDefault()
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

	public event TypedEventHandler<Compass, CompassReadingChangedEventArgs> ReadingChanged
	{
		add
		{
			lock (_syncLock)
			{
				var isFirstSubscriber = _readingChanged == null;
				_readingChanged += value;
				if (isFirstSubscriber)
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

	private void OnReadingChanged(CompassReading reading)
	{
		_readingChanged?.Invoke(this, new CompassReadingChangedEventArgs(reading));
	}
}
