#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MobileBroadbandModemStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Busy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoDeviceSupport,
		#endif
	}
	#endif
}
