#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ESimAuthenticationPreference 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnEntry,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnAction,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Never,
		#endif
	}
	#endif
}
