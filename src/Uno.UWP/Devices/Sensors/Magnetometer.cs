#nullable disable

#if __IOS__ || __ANDROID__ || __WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Magnetometer
	{
		private readonly static object _syncLock = new object();

		private static Magnetometer _instance;
		private static bool _initializationAttempted;

		private TypedEventHandler<Magnetometer, MagnetometerReadingChangedEventArgs> _readingChanged;

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

		public event TypedEventHandler<Magnetometer, MagnetometerReadingChangedEventArgs> ReadingChanged
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

		private void OnReadingChanged(MagnetometerReading reading)
		{
			_readingChanged?.Invoke(this, new MagnetometerReadingChangedEventArgs(reading));
		}
	}
}
#endif
