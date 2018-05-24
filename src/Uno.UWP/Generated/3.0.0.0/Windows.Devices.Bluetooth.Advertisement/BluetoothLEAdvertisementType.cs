#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Advertisement
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BluetoothLEAdvertisementType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectableUndirected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectableDirected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ScannableUndirected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NonConnectableUndirected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ScanResponse,
		#endif
	}
	#endif
}
