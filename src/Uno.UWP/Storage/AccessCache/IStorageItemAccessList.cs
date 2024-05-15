using Windows.Foundation;

namespace Windows.Storage.AccessCache;

/// <summary>
/// Represents a list of storage items that the app has stored for efficient future access.
/// </summary>
public partial interface IStorageItemAccessList
{
	/// <summary>
	/// Gets an object for retrieving storage items from the access list.
	/// </summary>
	AccessListEntryView Entries { get; }

	/// <summary>
	/// Gets the maximum number of storage items that the access list can contain.
	/// </summary>
	uint MaximumItemsAllowed { get; }

	/// <summary>
	/// Adds a new storage item to the access list.
	/// </summary>
	/// <param name="file">The storage item to add.</param>
	/// <returns>A token that the app can use later to retrieve the storage item.</returns>
	string Add(IStorageItem file);

	/// <summary>
	/// Adds a new storage item and accompanying metadata to the access list.
	/// </summary>
	/// <param name="file">The storage item to add.</param>
	/// <param name="metadata">Optional metadata to associate with the storage item.</param>
	/// <returns>A token that the app can use later to retrieve the storage item.</returns>
	string Add(IStorageItem file, string metadata);

	/// <summary>
	/// Adds a new storage item to the access list, or replaces the specified item if it already exists in the list.
	/// </summary>
	/// <param name="token">
	/// The token associated with the new storage item. If the access list already contains a storage item 
	/// that has this token, the new item replaces the existing one.</param>
	/// <param name="file">The storage item to add or replace.</param>
	void AddOrReplace(string token, IStorageItem file);

	/// <summary>
	/// Adds a new storage item and accompanying metadata to the access list, 
	/// or replaces the specified item if it already exists in the list.
	/// </summary>
	/// <param name="token">
	/// The token associated with the new storage item. If the access list already contains a storage item 
	/// that has this token, the new item replaces the existing one.</param>
	/// <param name="file">The storage item to add or replace.</param>
	/// <param name="metadata">Optional metadata to associate with the storage item.</param>
	void AddOrReplace(string token, IStorageItem file, string metadata);

	/// <summary>
	/// Determines whether the app has access to the specified storage item in the access list.
	/// </summary>
	/// <param name="file"></param>
	/// <returns></returns>
	bool CheckAccess(IStorageItem file);

	/// <summary>
	/// Removes all storage items from the access list.
	/// </summary>
	void Clear();

	/// <summary>
	/// Determines whether the access list contains the specified storage item.
	/// </summary>
	/// <param name="token"></param>
	/// <returns></returns>
	bool ContainsItem(string token);

	/// <summary>
	/// Retrieves the specified StorageFile from the list.
	/// </summary>
	/// <param name="token">The token of the StorageFile to retrieve.</param>
	/// <returns>
	/// When this method completes successfully, it returns the StorageFile that
	/// is associated with the specified token.
	/// </returns>
	IAsyncOperation<StorageFile> GetFileAsync(string token);

	/// <summary>
	/// Retrieves the specified StorageFile from the list using the specified options.
	/// </summary>
	/// <param name="token">The token of the StorageFile to retrieve.</param>
	/// <param name="options">The enum value that describes the behavior to use when the app accesses the item.</param>
	/// <returns>
	/// When this method completes successfully, it returns the StorageFile that
	/// is associated with the specified token.
	/// </returns>
	IAsyncOperation<StorageFile> GetFileAsync(string token, AccessCacheOptions options);

	/// <summary>
	/// Retrieves the specified StorageFolder from the list.
	/// </summary>
	/// <param name="token">The token of the StorageFolder to retrieve.</param>
	/// <param name="options">The enum value that describes the behavior to use when the app accesses the item.</param>
	/// <returns>
	/// When this method completes successfully, it returns the StorageFolder 
	/// that is associated with the specified token.
	/// </returns>
	IAsyncOperation<StorageFolder> GetFolderAsync(string token);

	/// <summary>
	/// Retrieves the specified StorageFolder from the list using the specified options.
	/// </summary>
	/// <param name="token">The token of the StorageFolder to retrieve.</param>
	/// <param name="options">The enum value that describes the behavior to use when the app accesses the item.</param>
	/// <returns>
	/// When this method completes successfully, it returns the StorageFolder 
	/// that is associated with the specified token.
	/// </returns>
	IAsyncOperation<StorageFolder> GetFolderAsync(string token, AccessCacheOptions options);

	/// <summary>
	/// Determines whether the access list contains the specified storage item.
	/// </summary>
	/// <param name="token">The token of the storage item to look for.</param>
	/// <returns>True if the access list contains the specified storage item; false otherwise.</returns>
	IAsyncOperation<IStorageItem> GetItemAsync(string token);

	/// <summary>
	/// Retrieves the specified item (like a file or folder) from the list.
	/// </summary>
	/// <param name="token">The token of the item to retrieve.</param>
	/// <param name="options"></param>
	/// <returns>
	/// When this method completes successfully, it returns the item 
	/// (type IStorageItem) that is associated with the specified token.
	/// </returns>
	IAsyncOperation<IStorageItem> GetItemAsync(string token, AccessCacheOptions options);

	/// <summary>
	/// Removes the specified storage item from the access list.
	/// </summary>
	/// <param name="token"></param>
	void Remove(string token);
}
