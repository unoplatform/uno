#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ProfileMediaType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wlan,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wwan,
		#endif
	}
	#endif
}
