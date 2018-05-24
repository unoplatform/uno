#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BluetoothError 
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
		DeviceNotConnected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByUser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConsentRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TransportNotSupported,
		#endif
	}
	#endif
}
