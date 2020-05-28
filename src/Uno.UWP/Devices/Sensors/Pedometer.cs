#if __IOS__ || __ANDROID__
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Pedometer
	{
		private readonly static object _syncLock = new object();
		
		private static bool _initializationAttempted;
		private static Task<Pedometer> _instanceTask;

		private TypedEventHandler<Pedometer, PedometerReadingChangedEventArgs> _readingChanged;

		public static IAsyncOperation<Pedometer> GetDefaultAsync() => GetDefaultImplAsync().AsAsyncOperation();

		private static async Task<Pedometer> GetDefaultImplAsync()
		{
			if (_initializationAttempted)
			{
				return await _instanceTask;
			}
			lock (_syncLock)
			{
				if (!_initializationAttempted)
				{
					_instanceTask = Task.Run(() => TryCreateInstance());
					_initializationAttempted = true;
				}				
			}
			return await _instanceTask;
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
#endif
