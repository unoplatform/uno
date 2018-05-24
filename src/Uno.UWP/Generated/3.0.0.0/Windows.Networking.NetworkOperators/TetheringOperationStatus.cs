#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TetheringOperationStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MobileBroadbandDeviceOff,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WiFiDeviceOff,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EntitlementCheckTimeout,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EntitlementCheckFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OperationInProgress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BluetoothDeviceOff,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkLimitedConnectivity,
		#endif
	}
	#endif
}
