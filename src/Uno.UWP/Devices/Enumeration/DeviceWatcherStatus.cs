namespace Windows.Devices.Enumeration
{
	/// <summary>
	/// Describes the state of a DeviceWatcher object.
	/// </summary>
	public enum DeviceWatcherStatus
	{
		/// <summary>
		/// This is the initial state of a Watcher object. During this state clients can register event handlers.
		/// </summary>
		Created,

		/// <summary>
		/// The watcher transitions to the Started state once Start is called. The watcher is enumerating the initial collection.
		/// Note that during this enumeration phase it is possible to receive Updated and Removed notifications but only to items that have already been Added.
		/// </summary>
		Started,

		/// <summary>
		/// The watcher has completed enumerating the initial collection. Items can still be added, updated or removed from the collection.
		/// </summary>
		EnumerationCompleted,

		/// <summary>
		/// The client has called Stop and the watcher is still in the process of stopping. Events may still be raised.
		/// </summary>
		Stopping,

		/// <summary>
		/// The client has called Stop and the watcher has completed all outstanding events. No further events will be raised.
		/// </summary>
		Stopped,

		/// <summary>
		/// The watcher has aborted operation. No subsequent events will be raised.
		/// </summary>
		Aborted,
	}
}
