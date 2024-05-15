namespace Windows.Storage.AccessCache;

/// <summary>
/// Describes the extent of the visibility of a storage item added to the most recently used (MRU) list.
/// </summary>
public enum RecentStorageItemVisibility
{
	/// <summary>
	/// The storage item is visible in the most recently used (MRU) list for the app only.
	/// </summary>
	AppOnly = 0,

	/// <summary>
	/// The storage item is visible in the most recently used (MRU) list for the app and the system.
	/// </summary>
	AppAndSystem = 1,
}
