using System;
using Uno.Devices.Enumeration.Internal;
using Windows.Foundation;

namespace Windows.Devices.Enumeration
{
	/// <summary>
	/// Enumerates devices dynamically, so that the app
	/// receives notifications if devices are added, removed,
	/// or changed after the initial enumeration is complete.
	/// </summary>
	public partial class DeviceWatcher
	{
		private readonly IDeviceClassProvider[] _providers;
		private int _stopCounter;
		private int _enumerationCounter;

		internal DeviceWatcher(IDeviceClassProvider[] providers)
		{
			_providers = providers ?? throw new ArgumentNullException(nameof(providers));

			if (_providers.Length == 0)
			{
				throw new ArgumentOutOfRangeException(nameof(_providers), "At least one provider is required");
			}
		}

		/// <summary>
		/// The status of the DeviceWatcher.
		/// </summary>
		public DeviceWatcherStatus Status { get; private set; } = DeviceWatcherStatus.Created;

		/// <summary>
		/// Starts a search for devices, and subscribes to device enumeration events.
		/// </summary>
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

		/// <summary>
		/// Stop raising the events that add, update and remove enumeration results.
		/// </summary>
		public void Stop()
		{
			if (Status != DeviceWatcherStatus.Stopping &&
				Status != DeviceWatcherStatus.Stopped)
			{
				_stopCounter = 0;
				Status = DeviceWatcherStatus.Stopping;
				foreach (var provider in _providers)
				{
					provider.WatchStop();
				}
			}
		}

		/// <summary>
		/// Event that is raised when a device is added to the collection
		/// enumerated by the DeviceWatcher.
		/// </summary>
		public event TypedEventHandler<DeviceWatcher, DeviceInformation>? Added;

		/// <summary>
		/// Event that is raised when a device is updated in the collection of enumerated devices.
		/// </summary>
		public event TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>? Updated;

		/// <summary>
		/// Event that is raised when a device is removed from the collection of enumerated devices.
		/// </summary>
		public event TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>? Removed;

		/// <summary>
		/// Event that is raised when the enumeration of devices completes.
		/// </summary>
		public event TypedEventHandler<DeviceWatcher, object?>? EnumerationCompleted;

		/// <summary>
		/// Event that is raised when the enumeration operation has been stopped.
		/// </summary>
		public event TypedEventHandler<DeviceWatcher, object?>? Stopped;

		private void Provider_WatchAdded(object? sender, DeviceInformation e) =>
			Added?.Invoke(this, e);

		private void Provider_WatchUpdated(object? sender, DeviceInformationUpdate e) =>
			Updated?.Invoke(this, e);

		private void Provider_WatchRemoved(object? sender, DeviceInformationUpdate e) =>
			Removed?.Invoke(this, e);

		private void Provider_WatchEnumerationCompleted(object? sender, DeviceInformation? e)
		{
			_enumerationCounter++;
			if (_enumerationCounter == _providers.Length)
			{
				Status = DeviceWatcherStatus.EnumerationCompleted;
				EnumerationCompleted?.Invoke(this, e);
			}
		}

		private void Provider_WatchStopped(object? sender, object? e)
		{
			_stopCounter++;
			if (_stopCounter == _providers.Length)
			{
				Status = DeviceWatcherStatus.Stopped;
				Stopped?.Invoke(this, e);
			}
		}
	}
}
