#if __IOS__ || __ANDROID__ || __WASM__
#nullable enable

using System;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors;

/// <summary>
/// Represents a compass sensor.
/// This sensor returns a heading with respect to Magnetic North and, possibly, True North. (The latter is dependent on the system capabilities.)
/// </summary>
public partial class Compass
{
	private readonly static Lazy<Compass?> _instance = new Lazy<Compass?>(() => TryCreateInstance());

	private readonly StartStopTypedEventWrapper<Compass, CompassReadingChangedEventArgs> _readingChangedWrapper;

	/// <summary>
	/// Hides the public parameterless constructor
	/// </summary>
	private Compass()
	{
		_readingChangedWrapper = new StartStopTypedEventWrapper<Compass, CompassReadingChangedEventArgs>(
			() => StartReadingChanged(),
			() => StopReadingChanged());
	}

	/// <summary>
	/// Returns the default compass.
	/// </summary>
	/// <returns>The default compass or null if no integrated compasses are found.</returns>
	public static Compass? GetDefault() => _instance.Value;

	/// <summary>
	/// Occurs each time the compass reports a new sensor reading.
	/// </summary>
	public event TypedEventHandler<Compass, CompassReadingChangedEventArgs> ReadingChanged
	{
		add => _readingChangedWrapper.AddHandler(value);
		remove => _readingChangedWrapper.RemoveHandler(value);
	}

	private void OnReadingChanged(CompassReading reading)
	{
		_readingChangedWrapper.Invoke(this, new CompassReadingChangedEventArgs(reading));
	}
}
#endif
