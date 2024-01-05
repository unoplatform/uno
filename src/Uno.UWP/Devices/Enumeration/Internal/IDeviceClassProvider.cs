using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace Uno.Devices.Enumeration.Internal
{
	internal interface IDeviceClassProvider
	{
		Task<DeviceInformation[]> FindAllAsync();

		void WatchStart();

		void WatchStop();

		bool CanWatch { get; }

		event EventHandler<DeviceInformation>? WatchAdded;

		event EventHandler<DeviceInformation>? WatchEnumerationCompleted;

		event EventHandler<DeviceInformationUpdate>? WatchRemoved;

		event EventHandler<object>? WatchStopped;

		event EventHandler<DeviceInformationUpdate>? WatchUpdated;
	}
}
