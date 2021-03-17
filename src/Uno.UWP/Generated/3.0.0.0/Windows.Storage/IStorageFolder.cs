#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStorageFolder : global::Windows.Storage.IStorageItem
	{
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CreateFileAsync( string desiredName);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CreateFileAsync( string desiredName,  global::Windows.Storage.CreationCollisionOption options);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> CreateFolderAsync( string desiredName);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> CreateFolderAsync( string desiredName,  global::Windows.Storage.CreationCollisionOption options);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> GetFileAsync( string name);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetFolderAsync( string name);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.IStorageItem> GetItemAsync( string name);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFile>> GetFilesAsync();
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFolder>> GetFoldersAsync();
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.IStorageItem>> GetItemsAsync();
		#endif
	}
}
