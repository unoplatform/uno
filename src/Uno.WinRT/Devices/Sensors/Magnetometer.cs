#if __IOS__ || __ANDROID__ || __WASM__
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents a magnetic sensor.
	/// </summary>
	public partial class Magnetometer
	{
		private readonly static Lazy<Magnetometer?> _instance = new Lazy<Magnetometer?>(() => TryCreateInstance());

		private readonly StartStopTypedEventWrapper<Magnetometer, MagnetometerReadingChangedEventArgs> _readingChangedWrapper;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private Magnetometer()
		{
			_readingChangedWrapper = new StartStopTypedEventWrapper<Magnetometer, MagnetometerReadingChangedEventArgs>(
				() => StartReading(),
				() => StopReading());
		}

		/// <summary>
		/// Returns the default magnetometer.
		/// </summary>
		/// <returns>The default magnetometer.</returns>
		public static Magnetometer? GetDefault() => _instance.Value;

		/// <summary>
		/// Occurs each time the compass reports a new sensor reading.
		/// </summary>
		public event TypedEventHandler<Magnetometer, MagnetometerReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}

		private void OnReadingChanged(MagnetometerReading reading)
		{
			_readingChangedWrapper.Invoke(this, new MagnetometerReadingChangedEventArgs(reading));
		}
	}
}
#endif
