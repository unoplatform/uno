#if __IOS__ || __ANDROID__
using System;
using System.Threading.Tasks;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Pedometer
	{
		private readonly static object _syncLock = new object();
		
		private static bool _initializationAttempted;
		private static Task<Pedometer> _instanceTask;

		private StartStopEventWrapper<TypedEventHandler<Pedometer, PedometerReadingChangedEventArgs>> _readingChangedWrapper;

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
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}

		private void OnReadingChanged(PedometerReading reading)
		{
			_readingChangedWrapper.Event?.Invoke(this, new PedometerReadingChangedEventArgs(reading));
		}
	}
}
#endif
