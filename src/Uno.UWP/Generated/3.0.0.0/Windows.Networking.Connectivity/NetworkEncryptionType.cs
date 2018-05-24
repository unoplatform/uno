#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NetworkEncryptionType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wep,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wep40,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wep104,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tkip,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ccmp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WpaUseGroup,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RsnUseGroup,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ihv,
		#endif
	}
	#endif
}
