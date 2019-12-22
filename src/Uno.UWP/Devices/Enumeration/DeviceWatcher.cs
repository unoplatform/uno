using System;
using Uno.Devices.Enumeration.Internal;
using Windows.Foundation;

namespace Windows.Devices.Enumeration
{
	public partial class DeviceWatcher
	{
		private readonly IDeviceClassProvider[] _providers;
		private int _stopCounter = 0;
		private int _enumerationCounter = 0;

		internal DeviceWatcher(IDeviceClassProvider[] providers)
		{
			_providers = providers ?? throw new ArgumentNullException(nameof(providers));

			if (_providers.Length == 0)
			{
				throw new ArgumentOutOfRangeException(nameof(_providers), "At least one provider is required");
			}
		}

		public DeviceWatcherStatus Status { get; private set; } = DeviceWatcherStatus.Created;

		public void Start()
		{
			foreach (var provider in _providers)
			{
				provider.WatchAdded += Provider_WatchAdded;
				provider.WatchEnumerationCompleted += Provider_WatchEnumerationCompleted;
				provider.WatchRemoved += Provider_WatchRemoved;
				provider.WatchStopped += Provider_WatchStopped;
				provider.WatchUpdated += Provider_WatchUpdated;
				provider.WatchStart();
			}
			Status = DeviceWatcherStatus.Started;
		}

		private void Provider_WatchUpdated(object sender, DeviceInformationUpdate e) => Updated?.Invoke(this, e);

		private void Provider_WatchStopped(object sender, object e)
		{
			_stopCounter++;
			if (_stopCounter == _providers.Length)
			{
				Status = DeviceWatcherStatus.Stopped;
				Stopped?.Invoke(this, e);
			}
		}

		private void Provider_WatchRemoved(object sender, DeviceInformationUpdate e) => Removed?.Invoke(this, e);

		private void Provider_WatchEnumerationCompleted(object sender, DeviceInformation e)
		{
			_enumerationCounter++;
			if (_enumerationCounter == _providers.Length)
			{
				Status = DeviceWatcherStatus.EnumerationCompleted;
				EnumerationCompleted?.Invoke(this, e);
			}
		}

		private void Provider_WatchAdded(object sender, DeviceInformation e) => Added?.Invoke(this, e);

		public void Stop()
		{
			_stopCounter = 0;
			Status = DeviceWatcherStatus.Stopping;
			foreach (var provider in _providers)
			{
				provider.WatchStop();
			}
		}

		public event TypedEventHandler<DeviceWatcher, DeviceInformation> Added;

		public event TypedEventHandler<DeviceWatcher, object> EnumerationCompleted;

		public event TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> Removed;

		public event TypedEventHandler<DeviceWatcher, object> Stopped;

		public event TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> Updated;
	}
}
