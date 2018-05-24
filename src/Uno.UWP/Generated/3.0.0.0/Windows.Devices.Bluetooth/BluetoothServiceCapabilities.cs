#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BluetoothServiceCapabilities 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LimitedDiscoverableMode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PositioningService,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkingService,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RenderingService,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CapturingService,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ObjectTransferService,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioService,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TelephoneService,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InformationService,
		#endif
	}
	#endif
}
