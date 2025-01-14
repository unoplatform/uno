#if __IOS__ || __ANDROID__
using System;
using System.Threading.Tasks;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents a pedometer sensor.
	/// This sensor returns the number of steps taken with the device.
	/// </summary>
	public partial class Pedometer
	{
		private readonly static object _syncLock = new();

		private static bool _initializationAttempted;
		private static Task<Pedometer> _instanceTask;

		private readonly StartStopTypedEventWrapper<Pedometer, PedometerReadingChangedEventArgs> _readingChangedWrapper;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private Pedometer()
		{
			_readingChangedWrapper = new StartStopTypedEventWrapper<Pedometer, PedometerReadingChangedEventArgs>(
				() => StartReading(),
				() => StopReading(),
				_syncLock);
		}

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
			_readingChangedWrapper.Invoke(this, new PedometerReadingChangedEventArgs(reading));
		}
	}
}
#endif
