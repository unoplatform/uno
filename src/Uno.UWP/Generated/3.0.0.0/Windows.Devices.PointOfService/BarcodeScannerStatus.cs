#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BarcodeScannerStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Online,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Off,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Offline,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OffOrOffline,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Extended,
		#endif
	}
	#endif
}
