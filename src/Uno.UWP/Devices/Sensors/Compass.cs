#if __IOS__ || __ANDROID__ || __WASM__
using Windows.Foundation;

namespace Windows.Devices.Sensors;

/// <summary>
/// Represents a compass sensor.
/// This sensor returns a heading with respect to Magnetic North and, possibly, True North. (The latter is dependent on the system capabilities.)
/// </summary>
public partial class Compass
{
	private readonly static object _syncLock = new();

	private static Compass? _instance;
	private static bool _initializationAttempted;

	private TypedEventHandler<Compass, CompassReadingChangedEventArgs>? _readingChanged;

	/// <summary>
	/// Hides the public parameterless constructor
	/// </summary>
	private Compass()
	{
	}

	/// <summary>
	/// Returns the default compass.
	/// </summary>
	/// <returns>The default compass or null if no integrated compasses are found.</returns>
	public static Compass? GetDefault()
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
	/// Occurs each time the compass reports a new sensor reading.
	/// </summary>
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
#endif
