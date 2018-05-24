#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Radios
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RadioKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WiFi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MobileBroadband,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bluetooth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FM,
		#endif
	}
	#endif
}
