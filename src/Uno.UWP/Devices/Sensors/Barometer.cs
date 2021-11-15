#if __ANDROID__ || __IOS__

using Uno.Extensions;
using Uno.Helpers;
using Uno.Logging;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private static readonly object _syncLock = new object();
		private static bool _initializationAttempted = false;
		private static Barometer _instance = null;	

		private readonly StartStopTypedEventWrapper<Barometer, BarometerReadingChangedEventArgs> _readingChangedWrapper;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private Barometer()
		{
			_readingChangedWrapper = new StartStopTypedEventWrapper<Barometer, BarometerReadingChangedEventArgs>(
				() => StartReading(),
				() => StopReading(),
				_syncLock);
		}

		public static Barometer GetDefault()
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

		public event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}


		private void OnReadingChanged(BarometerReading reading)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Barometer reading received " +
					$"StationPressureInHectopascals:{reading.StationPressureInHectopascals}, " +
					$"Timestamp:{reading.Timestamp}");
			}
			_readingChangedWrapper.Invoke(this, new GyrometerReadingChangedEventArgs(reading));
		}
	}
}
#endif
