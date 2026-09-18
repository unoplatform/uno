#if __ANDROID__ || __IOS__
#nullable enable

using System;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides an interface for a barometric sensor to measure atmospheric pressure.
	/// </summary>
	public partial class Barometer
	{
		private readonly static Lazy<Barometer?> _instance = new Lazy<Barometer?>(() => TryCreateInstance());

		private readonly StartStopTypedEventWrapper<Barometer, BarometerReadingChangedEventArgs> _readingChangedWrapper;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private Barometer()
		{
			_readingChangedWrapper = new StartStopTypedEventWrapper<Barometer, BarometerReadingChangedEventArgs>(
				() => StartReading(),
				() => StopReading());
		}

		/// <summary>
		/// Returns the default barometer sensor.
		/// </summary>
		/// <returns>If no barometer sensor is available, this method will return null.</returns>
		public static Barometer? GetDefault() => _instance.Value;

		/// <summary>
		/// Occurs each time the barometer reports a new sensor reading.
		/// </summary>
		public event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}
	}
}
#endif
