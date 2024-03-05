namespace Microsoft.UI.Windowing;

/// <summary>
/// Defines constants that specify the status of a DisplayAreaWatcher.
/// </summary>
public enum DisplayAreaWatcherStatus
{
	/// <summary>
	/// The watcher has been created.
	/// </summary>
	Created = 0,

	/// <summary>
	/// The watcher has started.
	/// </summary>
	Started = 1,

	/// <summary>
	/// The watcher has finished enumerating the display areas.
	/// </summary>
	EnumerationCompleted = 2,

	/// <summary>
	/// The watcher is stopping.
	/// </summary>
	Stopping = 3,

	/// <summary>
	/// The watcher has stopped.
	/// </summary>
	Stopped = 4,

	/// <summary>
	/// The watcher has stopped before completing enumeration of display areas.
	/// </summary>
	Aborted = 5,
}
