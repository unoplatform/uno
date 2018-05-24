#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Provider
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CachedFileTarget 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Local,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Remote,
		#endif
	}
	#endif
}
