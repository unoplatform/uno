namespace Windows.Storage.AccessCache;

/// <summary>
/// Provides data about an ItemRemoved event.
/// </summary>
public partial class ItemRemovedEventArgs
{
	internal ItemRemovedEventArgs(AccessListEntry removedEntry)
	{
		RemovedEntry = removedEntry;
	}

	/// <summary>
	/// Gets information about the StorageFile or StorageFolder that 
	/// was removed from the StorageItemMostRecentlyUsedList.
	/// </summary>
	public AccessListEntry RemovedEntry { get; }
}
