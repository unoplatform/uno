#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Advertisement
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BluetoothLEAdvertisementFlags 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LimitedDiscoverableMode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GeneralDiscoverableMode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ClassicNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DualModeControllerCapable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DualModeHostCapable,
		#endif
	}
	#endif
}
