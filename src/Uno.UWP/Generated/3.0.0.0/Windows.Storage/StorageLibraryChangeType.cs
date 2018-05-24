#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum StorageLibraryChangeType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Created,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Deleted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MovedOrRenamed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ContentsChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MovedOutOfLibrary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MovedIntoLibrary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ContentsReplaced,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IndexingStatusChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EncryptionChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChangeTrackingLost,
		#endif
	}
	#endif
}
