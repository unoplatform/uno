#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFi
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WiFiPhyKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Fhss,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dsss,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IRBaseband,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ofdm,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hrdsss,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Erp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HT,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Vht,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dmg,
		#endif
	}
	#endif
}
