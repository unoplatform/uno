#if __IOS__ || __ANDROID__
#nullable enable

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
		private readonly static Lazy<Task<Pedometer?>> _instance = new Lazy<Task<Pedometer?>>(() => Task.Run(() => TryCreateInstance()));

		private readonly StartStopTypedEventWrapper<Pedometer, PedometerReadingChangedEventArgs> _readingChangedWrapper;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private Pedometer()
		{
			_readingChangedWrapper = new StartStopTypedEventWrapper<Pedometer, PedometerReadingChangedEventArgs>(
				() => StartReading(),
				() => StopReading());
		}

		public static IAsyncOperation<Pedometer?> GetDefaultAsync() => GetDefaultImplAsync().AsAsyncOperation();

		private static async Task<Pedometer?> GetDefaultImplAsync() => await _instance.Value;

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
