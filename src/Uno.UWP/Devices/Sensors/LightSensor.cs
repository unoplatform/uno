#if __WASM__ || __ANDROID__
#nullable enable

using System;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class LightSensor
	{
		private readonly static Lazy<LightSensor?> _instance = new Lazy<LightSensor?>(() => TryCreateInstance());

		private readonly StartStopEventWrapper<TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>> _readingChangedWrapper;

		private LightSensor()
		{
			_readingChangedWrapper = new StartStopEventWrapper<TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>>(
				StartReading, StopReading);
		}

		public static LightSensor? GetDefault() => _instance.Value;

		public event TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}
	}
}
#endif
