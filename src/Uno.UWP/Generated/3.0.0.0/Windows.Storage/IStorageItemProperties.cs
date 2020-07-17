#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStorageItemProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string DisplayName
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string DisplayType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string FolderRelativeId
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.FileProperties.StorageItemContentProperties Properties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize,  global::Windows.Storage.FileProperties.ThumbnailOptions options);
		#endif
		// Forced skipping of method Windows.Storage.IStorageItemProperties.DisplayName.get
		// Forced skipping of method Windows.Storage.IStorageItemProperties.DisplayType.get
		// Forced skipping of method Windows.Storage.IStorageItemProperties.FolderRelativeId.get
		// Forced skipping of method Windows.Storage.IStorageItemProperties.Properties.get
	}
}
