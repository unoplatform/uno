#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AudioRoutingEndpoint 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Earpiece,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Speakerphone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bluetooth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WiredHeadset,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WiredHeadsetSpeakerOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BluetoothWithNoiseAndEchoCancellation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BluetoothPreferred,
		#endif
	}
	#endif
}
