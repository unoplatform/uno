#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.BulkAccess
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStorageItemInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.FileProperties.BasicProperties BasicProperties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.FileProperties.DocumentProperties DocumentProperties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.FileProperties.ImageProperties ImageProperties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.FileProperties.MusicProperties MusicProperties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.FileProperties.StorageItemThumbnail Thumbnail
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.FileProperties.VideoProperties VideoProperties
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Storage.BulkAccess.IStorageItemInformation.MusicProperties.get
		// Forced skipping of method Windows.Storage.BulkAccess.IStorageItemInformation.VideoProperties.get
		// Forced skipping of method Windows.Storage.BulkAccess.IStorageItemInformation.ImageProperties.get
		// Forced skipping of method Windows.Storage.BulkAccess.IStorageItemInformation.DocumentProperties.get
		// Forced skipping of method Windows.Storage.BulkAccess.IStorageItemInformation.BasicProperties.get
		// Forced skipping of method Windows.Storage.BulkAccess.IStorageItemInformation.Thumbnail.get
		// Forced skipping of method Windows.Storage.BulkAccess.IStorageItemInformation.ThumbnailUpdated.add
		// Forced skipping of method Windows.Storage.BulkAccess.IStorageItemInformation.ThumbnailUpdated.remove
		// Forced skipping of method Windows.Storage.BulkAccess.IStorageItemInformation.PropertiesUpdated.add
		// Forced skipping of method Windows.Storage.BulkAccess.IStorageItemInformation.PropertiesUpdated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Storage.BulkAccess.IStorageItemInformation, object> PropertiesUpdated;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Storage.BulkAccess.IStorageItemInformation, object> ThumbnailUpdated;
		#endif
	}
}
