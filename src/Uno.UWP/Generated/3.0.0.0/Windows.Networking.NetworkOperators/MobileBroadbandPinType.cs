#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MobileBroadbandPinType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pin1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pin2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SimPin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FirstSimPin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkPin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkSubsetPin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServiceProviderPin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CorporatePin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SubsidyLock,
		#endif
	}
	#endif
}
