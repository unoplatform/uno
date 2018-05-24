#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.AccessCache
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AccessCacheOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisallowUserInput,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FastLocationsOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseReadOnlyCachedCopy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SuppressAccessTimeUpdate,
		#endif
	}
	#endif
}
