#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFi
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WiFiNetworkKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Any,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Infrastructure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Adhoc,
		#endif
	}
	#endif
}
