#if __WASM__ || __ANDROID__
#nullable enable

using System;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents an ambient-light sensor.
	/// This sensor returns the ambient-light reading as a LUX value.
	/// </summary>
	public partial class LightSensor
	{
		private readonly static Lazy<LightSensor?> _instance = new Lazy<LightSensor?>(() => TryCreateInstance());

		private readonly StartStopTypedEventWrapper<LightSensor, LightSensorReadingChangedEventArgs> _readingChangedWrapper;

		private LightSensor()
		{
			_readingChangedWrapper = new StartStopTypedEventWrapper<LightSensor, LightSensorReadingChangedEventArgs>(
				StartReading, StopReading);
		}

		/// <summary>
		/// Returns the default ambient-light sensor.
		/// </summary>
		/// <returns>
		/// The default ambient-light sensor or null
		/// if no integrated light sensors are found.
		/// </returns>
		public static LightSensor? GetDefault() => _instance.Value;

		/// <summary>
		/// Occurs each time the ambient-light sensor reports a new sensor reading.
		/// </summary>
		public event TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}

		internal static void OnReadingChanged(LightSensorReading reading)
		{
			var eventArgs = new LightSensorReadingChangedEventArgs(reading);

			if (_instance.Value is { } sensor)
			{
				sensor._readingChangedWrapper.Event?.Invoke(sensor, eventArgs);
			}
		}
	}
}
#endif
