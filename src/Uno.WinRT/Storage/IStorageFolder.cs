using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.Storage
{
	public partial interface IStorageFolder : IStorageItem
	{
		IAsyncOperation<StorageFile> CreateFileAsync(string desiredName);

		IAsyncOperation<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options);

		IAsyncOperation<StorageFolder> CreateFolderAsync(string desiredName);

		IAsyncOperation<StorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options);

		IAsyncOperation<StorageFile> GetFileAsync(string name);

		IAsyncOperation<StorageFolder> GetFolderAsync(string name);

		IAsyncOperation<IStorageItem> GetItemAsync(string name);

		IAsyncOperation<IReadOnlyList<StorageFile>> GetFilesAsync();

		IAsyncOperation<IReadOnlyList<StorageFolder>> GetFoldersAsync();

		IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync();
	}
}
