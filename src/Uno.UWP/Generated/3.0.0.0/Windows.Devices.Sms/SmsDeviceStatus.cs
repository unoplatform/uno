#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SmsDeviceStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Off,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ready,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SimNotInserted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BadSim,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SubscriptionNotActivated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceLocked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceBlocked,
		#endif
	}
	#endif
}
