#if __IOS__ || __ANDROID__ || __WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents a magnetic sensor.
	/// </summary>
	public partial class Magnetometer
	{
		private readonly static object _syncLock = new();

		private static Magnetometer _instance;
		private static bool _initializationAttempted;

		private readonly StartStopTypedEventWrapper<Magnetometer, MagnetometerReadingChangedEventArgs> _readingChangedWrapper;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private Magnetometer()
		{
			_readingChangedWrapper = new StartStopTypedEventWrapper<Magnetometer, MagnetometerReadingChangedEventArgs>(
				() => StartReading(),
				() => StopReading(),
				_syncLock);
		}

		/// <summary>
		/// Returns the default magnetometer.
		/// </summary>
		/// <returns>The default magnetometer.</returns>
		public static Magnetometer GetDefault()
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

		/// <summary>
		/// Occurs each time the compass reports a new sensor reading.
		/// </summary>
		public event TypedEventHandler<Magnetometer, MagnetometerReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChangedWrapper.AddHandler(value);
			remove => _readingChangedWrapper.RemoveHandler(value);
		}

		private void OnReadingChanged(MagnetometerReading reading)
		{
			_readingChangedWrapper.Invoke(this, new MagnetometerReadingChangedEventArgs(reading));
		}
	}
}
#endif
