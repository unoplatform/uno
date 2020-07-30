#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Search
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStorageFolderQueryOperations 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Search.IndexedState> GetIndexedStateAsync();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Search.StorageFileQueryResult CreateFileQuery();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Search.StorageFileQueryResult CreateFileQuery( global::Windows.Storage.Search.CommonFileQuery query);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Search.StorageFileQueryResult CreateFileQueryWithOptions( global::Windows.Storage.Search.QueryOptions queryOptions);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Search.StorageFolderQueryResult CreateFolderQuery();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Search.StorageFolderQueryResult CreateFolderQuery( global::Windows.Storage.Search.CommonFolderQuery query);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Search.StorageFolderQueryResult CreateFolderQueryWithOptions( global::Windows.Storage.Search.QueryOptions queryOptions);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Search.StorageItemQueryResult CreateItemQuery();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Search.StorageItemQueryResult CreateItemQueryWithOptions( global::Windows.Storage.Search.QueryOptions queryOptions);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFile>> GetFilesAsync( global::Windows.Storage.Search.CommonFileQuery query,  uint startIndex,  uint maxItemsToRetrieve);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFile>> GetFilesAsync( global::Windows.Storage.Search.CommonFileQuery query);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFolder>> GetFoldersAsync( global::Windows.Storage.Search.CommonFolderQuery query,  uint startIndex,  uint maxItemsToRetrieve);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFolder>> GetFoldersAsync( global::Windows.Storage.Search.CommonFolderQuery query);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.IStorageItem>> GetItemsAsync( uint startIndex,  uint maxItemsToRetrieve);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool AreQueryOptionsSupported( global::Windows.Storage.Search.QueryOptions queryOptions);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsCommonFolderQuerySupported( global::Windows.Storage.Search.CommonFolderQuery query);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsCommonFileQuerySupported( global::Windows.Storage.Search.CommonFileQuery query);
		#endif
	}
}
