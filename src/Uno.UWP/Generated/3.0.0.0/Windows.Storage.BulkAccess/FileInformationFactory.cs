#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.BulkAccess
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FileInformationFactory 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public FileInformationFactory( global::Windows.Storage.Search.IStorageQueryResultBase queryResult,  global::Windows.Storage.FileProperties.ThumbnailMode mode) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.BulkAccess.FileInformationFactory", "FileInformationFactory.FileInformationFactory(IStorageQueryResultBase queryResult, ThumbnailMode mode)");
		}
		#endif
		// Forced skipping of method Windows.Storage.BulkAccess.FileInformationFactory.FileInformationFactory(Windows.Storage.Search.IStorageQueryResultBase, Windows.Storage.FileProperties.ThumbnailMode)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public FileInformationFactory( global::Windows.Storage.Search.IStorageQueryResultBase queryResult,  global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedThumbnailSize) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.BulkAccess.FileInformationFactory", "FileInformationFactory.FileInformationFactory(IStorageQueryResultBase queryResult, ThumbnailMode mode, uint requestedThumbnailSize)");
		}
		#endif
		// Forced skipping of method Windows.Storage.BulkAccess.FileInformationFactory.FileInformationFactory(Windows.Storage.Search.IStorageQueryResultBase, Windows.Storage.FileProperties.ThumbnailMode, uint)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public FileInformationFactory( global::Windows.Storage.Search.IStorageQueryResultBase queryResult,  global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedThumbnailSize,  global::Windows.Storage.FileProperties.ThumbnailOptions thumbnailOptions) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.BulkAccess.FileInformationFactory", "FileInformationFactory.FileInformationFactory(IStorageQueryResultBase queryResult, ThumbnailMode mode, uint requestedThumbnailSize, ThumbnailOptions thumbnailOptions)");
		}
		#endif
		// Forced skipping of method Windows.Storage.BulkAccess.FileInformationFactory.FileInformationFactory(Windows.Storage.Search.IStorageQueryResultBase, Windows.Storage.FileProperties.ThumbnailMode, uint, Windows.Storage.FileProperties.ThumbnailOptions)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public FileInformationFactory( global::Windows.Storage.Search.IStorageQueryResultBase queryResult,  global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedThumbnailSize,  global::Windows.Storage.FileProperties.ThumbnailOptions thumbnailOptions,  bool delayLoad) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.BulkAccess.FileInformationFactory", "FileInformationFactory.FileInformationFactory(IStorageQueryResultBase queryResult, ThumbnailMode mode, uint requestedThumbnailSize, ThumbnailOptions thumbnailOptions, bool delayLoad)");
		}
		#endif
		// Forced skipping of method Windows.Storage.BulkAccess.FileInformationFactory.FileInformationFactory(Windows.Storage.Search.IStorageQueryResultBase, Windows.Storage.FileProperties.ThumbnailMode, uint, Windows.Storage.FileProperties.ThumbnailOptions, bool)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.BulkAccess.IStorageItemInformation>> GetItemsAsync( uint startIndex,  uint maxItemsToRetrieve)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<IStorageItemInformation>> FileInformationFactory.GetItemsAsync(uint startIndex, uint maxItemsToRetrieve) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.BulkAccess.IStorageItemInformation>> GetItemsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<IStorageItemInformation>> FileInformationFactory.GetItemsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.BulkAccess.FileInformation>> GetFilesAsync( uint startIndex,  uint maxItemsToRetrieve)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<FileInformation>> FileInformationFactory.GetFilesAsync(uint startIndex, uint maxItemsToRetrieve) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.BulkAccess.FileInformation>> GetFilesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<FileInformation>> FileInformationFactory.GetFilesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.BulkAccess.FolderInformation>> GetFoldersAsync( uint startIndex,  uint maxItemsToRetrieve)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<FolderInformation>> FileInformationFactory.GetFoldersAsync(uint startIndex, uint maxItemsToRetrieve) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.BulkAccess.FolderInformation>> GetFoldersAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<FolderInformation>> FileInformationFactory.GetFoldersAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object GetVirtualizedItemsVector()
		{
			throw new global::System.NotImplementedException("The member object FileInformationFactory.GetVirtualizedItemsVector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object GetVirtualizedFilesVector()
		{
			throw new global::System.NotImplementedException("The member object FileInformationFactory.GetVirtualizedFilesVector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object GetVirtualizedFoldersVector()
		{
			throw new global::System.NotImplementedException("The member object FileInformationFactory.GetVirtualizedFoldersVector() is not implemented in Uno.");
		}
		#endif
	}
}
