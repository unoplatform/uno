#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ApplicationDataLocality 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Local,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roaming,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Temporary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LocalCache,
		#endif
	}
	#endif
}
