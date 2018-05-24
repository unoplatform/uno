#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BluetoothMajorClass 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Miscellaneous,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Computer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Phone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkAccessPoint,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Peripheral,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Imaging,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wearable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Toy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Health,
		#endif
	}
	#endif
}
