#if __IOS__ || __ANDROID__ || __WASM__
using Uno.Extensions;
using Uno.Helpers;
using Uno.Logging;
using Windows.Foundation;
using Windows.UI.WebUI;

namespace Windows.Devices.Sensors
{
	public partial class Gyrometer
	{
		private readonly static object _syncLock = new object();

		private static Gyrometer _instance;
		private static bool _initializationAttempted;

		private StartStopEventWrapper<TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs>> _readingChangedWrapper;

		public Gyrometer()
		{
			_readingChangedWrapper = new StartStopEventWrapper<TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs>>(
				() => StartReading(),
				() => StopReading(),
				_syncLock);
		}

		public static Gyrometer GetDefault()
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

		public event TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}

		private void OnReadingChanged(GyrometerReading reading)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat($"Gyrometer reading received " +
					$"X:{reading.AngularVelocityX}, Y:{reading.AngularVelocityY}, Z:{reading.AngularVelocityZ}");
			}
			_readingChangedWrapper.Event?.Invoke(this, new GyrometerReadingChangedEventArgs(reading));
		}
	}
}
#endif
