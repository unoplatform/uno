#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Background
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BluetoothEventTriggeringMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Serial,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Batch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KeepLatest,
		#endif
	}
	#endif
}
