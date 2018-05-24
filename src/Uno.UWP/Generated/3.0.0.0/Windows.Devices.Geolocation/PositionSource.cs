#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PositionSource 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cellular,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Satellite,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WiFi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IPAddress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Obfuscated,
		#endif
	}
	#endif
}
