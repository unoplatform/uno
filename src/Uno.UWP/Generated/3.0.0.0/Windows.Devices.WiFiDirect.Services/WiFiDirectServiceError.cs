#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect.Services
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WiFiDirectServiceError 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RadioNotAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResourceInUse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsupportedHardware,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoHardware,
		#endif
	}
	#endif
}
