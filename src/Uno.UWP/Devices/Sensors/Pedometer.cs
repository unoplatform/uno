using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Pedometer
	{
		private readonly static object _syncLock = new object();
		
		private static Pedometer _instance;
		private static bool _initializationAttempted;

		private TypedEventHandler<Pedometer, PedometerReadingChangedEventArgs> _readingChanged;
		
		public static IAsyncOperation<Pedometer> GetDefaultAsync()
		{
			if (_initializationAttempted)
			{
				return Task.FromResult(_instance).AsAsyncOperation();
			}
			lock (_syncLock)
			{
				if (!_initializationAttempted)
				{
					_instance = TryCreateInstance();
					_initializationAttempted = true;
				}
				return Task.FromResult(_instance).AsAsyncOperation();
			}
		}

		public event TypedEventHandler<Pedometer, PedometerReadingChangedEventArgs> ReadingChanged
		{
			add
			{
				lock (_syncLock)
				{
					var isFirstSubscriber = _readingChanged == null;
					_readingChanged += value;
					if (isFirstSubscriber)
					{
						StartReading();
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
						StopReading();
					}
				}
			}
		}

		private void OnReadingChanged(PedometerReading reading)
		{
			_readingChanged?.Invoke(this, new PedometerReadingChangedEventArgs(reading));
		}
	}
}
