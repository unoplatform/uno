#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorageFolder : global::Windows.Storage.IStorageFolder,global::Windows.Storage.IStorageItem,global::Windows.Storage.Search.IStorageFolderQueryOperations,global::Windows.Storage.IStorageItemProperties,global::Windows.Storage.IStorageItemProperties2,global::Windows.Storage.IStorageItem2,global::Windows.Storage.IStorageFolder2,global::Windows.Storage.IStorageItemPropertiesWithProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.FileAttributes Attributes
		{
			get
			{
				throw new global::System.NotImplementedException("The member FileAttributes StorageFolder.Attributes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset DateCreated
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset StorageFolder.DateCreated is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Name
		// Skipping already declared property Path
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StorageFolder.DisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DisplayType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StorageFolder.DisplayType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string FolderRelativeId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StorageFolder.FolderRelativeId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.FileProperties.StorageItemContentProperties Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageItemContentProperties StorageFolder.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.StorageProvider Provider
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageProvider StorageFolder.Provider is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CreateFileAsync( string desiredName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageFolder.CreateFileAsync(string desiredName) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Storage.StorageFolder.CreateFileAsync(string, Windows.Storage.CreationCollisionOption)
		// Skipping already declared method Windows.Storage.StorageFolder.CreateFolderAsync(string)
		// Skipping already declared method Windows.Storage.StorageFolder.CreateFolderAsync(string, Windows.Storage.CreationCollisionOption)
		// Skipping already declared method Windows.Storage.StorageFolder.GetFileAsync(string)
		// Skipping already declared method Windows.Storage.StorageFolder.GetFolderAsync(string)
		// Skipping already declared method Windows.Storage.StorageFolder.GetItemAsync(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFile>> GetFilesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StorageFile>> StorageFolder.GetFilesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFolder>> GetFoldersAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StorageFolder>> StorageFolder.GetFoldersAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.IStorageItem>> GetItemsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<IStorageItem>> StorageFolder.GetItemsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction RenameAsync( string desiredName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFolder.RenameAsync(string desiredName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction RenameAsync( string desiredName,  global::Windows.Storage.NameCollisionOption option)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFolder.RenameAsync(string desiredName, NameCollisionOption option) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Storage.StorageFolder.DeleteAsync()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction DeleteAsync( global::Windows.Storage.StorageDeleteOption option)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFolder.DeleteAsync(StorageDeleteOption option) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.BasicProperties> GetBasicPropertiesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<BasicProperties> StorageFolder.GetBasicPropertiesAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.StorageFolder.Name.get
		// Forced skipping of method Windows.Storage.StorageFolder.Path.get
		// Forced skipping of method Windows.Storage.StorageFolder.Attributes.get
		// Forced skipping of method Windows.Storage.StorageFolder.DateCreated.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsOfType( global::Windows.Storage.StorageItemTypes type)
		{
			throw new global::System.NotImplementedException("The member bool StorageFolder.IsOfType(StorageItemTypes type) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Search.IndexedState> GetIndexedStateAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IndexedState> StorageFolder.GetIndexedStateAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Search.StorageFileQueryResult CreateFileQuery()
		{
			throw new global::System.NotImplementedException("The member StorageFileQueryResult StorageFolder.CreateFileQuery() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Search.StorageFileQueryResult CreateFileQuery( global::Windows.Storage.Search.CommonFileQuery query)
		{
			throw new global::System.NotImplementedException("The member StorageFileQueryResult StorageFolder.CreateFileQuery(CommonFileQuery query) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Search.StorageFileQueryResult CreateFileQueryWithOptions( global::Windows.Storage.Search.QueryOptions queryOptions)
		{
			throw new global::System.NotImplementedException("The member StorageFileQueryResult StorageFolder.CreateFileQueryWithOptions(QueryOptions queryOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Search.StorageFolderQueryResult CreateFolderQuery()
		{
			throw new global::System.NotImplementedException("The member StorageFolderQueryResult StorageFolder.CreateFolderQuery() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Search.StorageFolderQueryResult CreateFolderQuery( global::Windows.Storage.Search.CommonFolderQuery query)
		{
			throw new global::System.NotImplementedException("The member StorageFolderQueryResult StorageFolder.CreateFolderQuery(CommonFolderQuery query) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Search.StorageFolderQueryResult CreateFolderQueryWithOptions( global::Windows.Storage.Search.QueryOptions queryOptions)
		{
			throw new global::System.NotImplementedException("The member StorageFolderQueryResult StorageFolder.CreateFolderQueryWithOptions(QueryOptions queryOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Search.StorageItemQueryResult CreateItemQuery()
		{
			throw new global::System.NotImplementedException("The member StorageItemQueryResult StorageFolder.CreateItemQuery() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Search.StorageItemQueryResult CreateItemQueryWithOptions( global::Windows.Storage.Search.QueryOptions queryOptions)
		{
			throw new global::System.NotImplementedException("The member StorageItemQueryResult StorageFolder.CreateItemQueryWithOptions(QueryOptions queryOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFile>> GetFilesAsync( global::Windows.Storage.Search.CommonFileQuery query,  uint startIndex,  uint maxItemsToRetrieve)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StorageFile>> StorageFolder.GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFile>> GetFilesAsync( global::Windows.Storage.Search.CommonFileQuery query)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StorageFile>> StorageFolder.GetFilesAsync(CommonFileQuery query) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFolder>> GetFoldersAsync( global::Windows.Storage.Search.CommonFolderQuery query,  uint startIndex,  uint maxItemsToRetrieve)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StorageFolder>> StorageFolder.GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFolder>> GetFoldersAsync( global::Windows.Storage.Search.CommonFolderQuery query)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StorageFolder>> StorageFolder.GetFoldersAsync(CommonFolderQuery query) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.IStorageItem>> GetItemsAsync( uint startIndex,  uint maxItemsToRetrieve)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<IStorageItem>> StorageFolder.GetItemsAsync(uint startIndex, uint maxItemsToRetrieve) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool AreQueryOptionsSupported( global::Windows.Storage.Search.QueryOptions queryOptions)
		{
			throw new global::System.NotImplementedException("The member bool StorageFolder.AreQueryOptionsSupported(QueryOptions queryOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsCommonFolderQuerySupported( global::Windows.Storage.Search.CommonFolderQuery query)
		{
			throw new global::System.NotImplementedException("The member bool StorageFolder.IsCommonFolderQuerySupported(CommonFolderQuery query) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsCommonFileQuerySupported( global::Windows.Storage.Search.CommonFileQuery query)
		{
			throw new global::System.NotImplementedException("The member bool StorageFolder.IsCommonFileQuerySupported(CommonFileQuery query) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFolder.GetThumbnailAsync(ThumbnailMode mode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFolder.GetThumbnailAsync(ThumbnailMode mode, uint requestedSize) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize,  global::Windows.Storage.FileProperties.ThumbnailOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFolder.GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.StorageFolder.DisplayName.get
		// Forced skipping of method Windows.Storage.StorageFolder.DisplayType.get
		// Forced skipping of method Windows.Storage.StorageFolder.FolderRelativeId.get
		// Forced skipping of method Windows.Storage.StorageFolder.Properties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetScaledImageAsThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFolder.GetScaledImageAsThumbnailAsync(ThumbnailMode mode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetScaledImageAsThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFolder.GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetScaledImageAsThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize,  global::Windows.Storage.FileProperties.ThumbnailOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFolder.GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetParentAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> StorageFolder.GetParentAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsEqual( global::Windows.Storage.IStorageItem item)
		{
			throw new global::System.NotImplementedException("The member bool StorageFolder.IsEqual(IStorageItem item) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Storage.StorageFolder.TryGetItemAsync(string)
		// Forced skipping of method Windows.Storage.StorageFolder.Provider.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.StorageLibraryChangeTracker TryGetChangeTracker()
		{
			throw new global::System.NotImplementedException("The member StorageLibraryChangeTracker StorageFolder.TryGetChangeTracker() is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Storage.StorageFolder.GetFolderFromPathAsync(string)
		// Processing: Windows.Storage.IStorageFolder
		// Processing: Windows.Storage.IStorageItem
		// Processing: Windows.Storage.Search.IStorageFolderQueryOperations
		// Processing: Windows.Storage.IStorageItemProperties
		// Processing: Windows.Storage.IStorageItemProperties2
		// Processing: Windows.Storage.IStorageItem2
		// Processing: Windows.Storage.IStorageFolder2
		// Processing: Windows.Storage.IStorageItemPropertiesWithProvider
	}
}
