#if __IOS__ || __ANDROID__ || __WASM__
#define IS_IMPLEMENTED
#endif

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides data for the accelerometer reading– changed event.
	/// </summary>
	public partial class AccelerometerReadingChangedEventArgs
	{
		internal AccelerometerReadingChangedEventArgs(AccelerometerReading reading)
		{
#if IS_IMPLEMENTED
			Reading = reading;
#endif
		}

#if IS_IMPLEMENTED

		/// <summary>
		/// Gets the most recent accelerometer reading.
		/// </summary>
		public AccelerometerReading Reading { get; }
#endif
	}
}
