#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NetworkAuthenticationType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Open80211,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SharedKey80211,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wpa,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WpaPsk,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WpaNone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rsna,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RsnaPsk,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ihv,
		#endif
	}
	#endif
}
