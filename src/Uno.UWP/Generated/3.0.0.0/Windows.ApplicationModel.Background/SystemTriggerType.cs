#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SystemTriggerType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Invalid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SmsReceived,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserPresent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserAway,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkStateChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ControlChannelReset,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InternetAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SessionConnected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServicingComplete,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LockScreenApplicationAdded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LockScreenApplicationRemoved,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TimeZoneChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnlineIdConnectedStateChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BackgroundWorkCostChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PowerStateChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DefaultSignInAccountChange,
		#endif
	}
	#endif
}
