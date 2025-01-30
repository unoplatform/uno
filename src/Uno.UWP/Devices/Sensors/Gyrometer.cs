#if __IOS__ || __ANDROID__ || __WASM__
#nullable enable

using System;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents a gyrometer sensor that provides angular velocity values with respect to the x, y, and z axes.
	/// </summary>
	public partial class Gyrometer
	{
		private readonly static Lazy<Gyrometer?> _instance = new Lazy<Gyrometer?>(() => TryCreateInstance());

		private readonly StartStopTypedEventWrapper<Gyrometer, GyrometerReadingChangedEventArgs> _readingChangedWrapper;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private Gyrometer()
		{
			_readingChangedWrapper = new StartStopTypedEventWrapper<Gyrometer, GyrometerReadingChangedEventArgs>(
				() => StartReading(),
				() => StopReading());
		}

		/// <summary>
		/// Returns the default gyrometer.
		/// </summary>
		/// <returns>Null if no integrated gyrometers are found.</returns>
		public static Gyrometer? GetDefault() => _instance.Value;

		/// <summary>
		/// Occurs each time the gyrometer reports the current sensor reading.
		/// </summary>
		public event TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}

		private void OnReadingChanged(GyrometerReading reading)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat($"Gyrometer reading received " +
					$"X:{reading.AngularVelocityX}, Y:{reading.AngularVelocityY}, Z:{reading.AngularVelocityZ}");
			}
			_readingChangedWrapper.Invoke(this, new GyrometerReadingChangedEventArgs(reading));
		}
	}
}
#endif
