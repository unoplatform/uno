using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace Uno.Devices.Enumeration.Internal
{
	internal interface IDeviceClassProvider
	{
		void WatchStart();

		void WatchStop();

		Task<DeviceInformation[]> FindAllAsync();

		event EventHandler<DeviceInformation> WatchAdded;

		event EventHandler<DeviceInformation> WatchEnumerationCompleted;

		event EventHandler<DeviceInformationUpdate> WatchRemoved;

		event EventHandler<object> WatchStopped;

		event EventHandler<DeviceInformationUpdate> WatchUpdated;		
	}
}
