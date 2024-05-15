namespace Windows.Storage.AccessCache;


/// <summary>
/// Represents a list entry that contains the identifier and metadata 
/// for a StorageFile or StorageFolder object in a list.
/// </summary>
public partial struct AccessListEntry
{
	/// <summary>
	/// The identifier of the StorageFile or StorageFolder in the list.
	/// </summary>
	public string Token;

	/// <summary>
	/// Optional app-specified metadata associated with the StorageFile or StorageFolder in the list.
	/// </summary>
	public string Metadata;
}
