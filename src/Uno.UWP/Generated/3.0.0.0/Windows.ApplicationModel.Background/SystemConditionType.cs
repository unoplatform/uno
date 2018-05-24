#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SystemConditionType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Invalid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserPresent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserNotPresent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InternetAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InternetNotAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SessionConnected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SessionDisconnected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FreeNetworkAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BackgroundWorkCostNotHigh,
		#endif
	}
	#endif
}
