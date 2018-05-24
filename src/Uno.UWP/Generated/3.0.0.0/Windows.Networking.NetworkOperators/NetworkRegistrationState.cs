#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NetworkRegistrationState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Deregistered,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Searching,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Home,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roaming,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Partner,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Denied,
		#endif
	}
	#endif
}
