#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NetworkDeviceStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceNotReady,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceReady,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SimNotInserted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BadSim,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceHardwareFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccountNotActivated,
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
