#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BluetoothAddressType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Public,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Random,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unspecified,
		#endif
	}
	#endif
}
