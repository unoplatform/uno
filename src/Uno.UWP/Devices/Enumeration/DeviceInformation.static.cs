using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uno.Devices.Enumeration.Internal;
using Windows.Foundation;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceInformation
	{
#if !__ANDROID__ && !__WASM__ && !__IOS__ && !__MACOS__
		private static readonly Dictionary<Guid, Func<IDeviceClassProvider>> _deviceClassProviders = new Dictionary<Guid, Func<IDeviceClassProvider>>();
#endif

		public static DeviceWatcher CreateWatcher(string aqsFilter)
		{
			var providers = GetMatchingProviders(aqsFilter).Where(w => w.CanWatch).ToArray();

			if (providers.Length == 0)
			{
				throw new NotSupportedException("This AQS device filter is not yet supported in Uno Platform.");
			}

			return new DeviceWatcher(providers);
		}

		public static IAsyncOperation<DeviceInformationCollection> FindAllAsync(string aqsFilter) =>
			FindAllInternalAsync(aqsFilter).AsAsyncOperation();
			

		private static async Task<DeviceInformationCollection> FindAllInternalAsync(string aqsFilter)
		{
			var providers = GetMatchingProviders(aqsFilter).ToArray();

			if (providers.Length == 0)
			{
				throw new NotSupportedException("This AQS device filter is not yet supported in Uno Platform.");
			}

			var allDevices = new List<DeviceInformation>();
			foreach (var provider in providers)
			{
				var devices = await provider.FindAllAsync();
				allDevices.AddRange(devices);
			}
			return new DeviceInformationCollection(allDevices);
		}

		private static IEnumerable<IDeviceClassProvider> GetMatchingProviders(string aqsFilter)
		{
			foreach (var provider in _deviceClassProviders)
			{
				if (aqsFilter.IndexOf(provider.Key.ToString(), StringComparison.InvariantCultureIgnoreCase) >= 0)
				{
					yield return provider.Value();
				}
			}
		}
	}
}
