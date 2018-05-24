#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFi
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WiFiConnectionStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnspecifiedFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccessRevoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidCredential,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkNotAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Timeout,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsupportedAuthenticationProtocol,
		#endif
	}
	#endif
}
