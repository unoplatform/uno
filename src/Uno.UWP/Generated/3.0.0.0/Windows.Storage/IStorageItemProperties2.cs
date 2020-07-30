#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStorageItemProperties2 : global::Windows.Storage.IStorageItemProperties
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetScaledImageAsThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetScaledImageAsThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetScaledImageAsThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize,  global::Windows.Storage.FileProperties.ThumbnailOptions options);
		#endif
	}
}
