#if __ANDROID__ || __IOS__
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Devices.Lights
{
	public partial class Lamp : IDisposable
	{
		private static readonly object _syncLock = new object();

		private static Lamp? _instance;
		private static bool _initializationAttempted;

		public static IAsyncOperation<Lamp?> GetDefaultAsync()
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
	}
}
#endif
