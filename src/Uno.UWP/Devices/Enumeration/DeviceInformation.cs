#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceInformation
	{
		private static readonly Dictionary<string, Func<IDeviceClassProvider>> _deviceClassProviders = new Dictionary<string, Func<IDeviceClassProvider>>()
		{
			{ DeviceClassGuids.MidiIn, () => new MidiInDeviceClassProvider() }
		};

		public static DeviceWatcher CreateWatcher(string aqsFilter)
		{
			var providers = GetMatchingProviders(aqsFilter).ToArray();
			return new DeviceWatcher(providers);
		}

		public static Foundation.IAsyncOperation<DeviceInformation> CreateFromIdAsync(string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceInformation> DeviceInformation.CreateFromIdAsync(string deviceId) is not implemented in Uno.");
		}

		public static Foundation.IAsyncOperation<DeviceInformationCollection> FindAllAsync(string aqsFilter) =>
			FindAllInternalAsync(aqsFilter).AsAsyncOperation();

		private static IEnumerable<IDeviceClassProvider> GetMatchingProviders(string aqsFilter)
		{
			foreach (var provider in _deviceClassProviders)
			{
				if (aqsFilter.IndexOf(provider.Key, StringComparison.InvariantCultureIgnoreCase) >= 0)
				{
					yield return provider.Value();
				}
			}
		}

		private static async Task<DeviceInformationCollection> FindAllInternalAsync(string aqsFilter)
		{
			var providers = GetMatchingProviders(aqsFilter).ToArray();
			var allDevices = new List<DeviceInformation>();
			foreach (var provider in providers)
			{
				var devices = await provider.FindAllAsync();
				allDevices.AddRange(devices);
			}
			return new DeviceInformationCollection(allDevices);
		}
	}
}
#endif
