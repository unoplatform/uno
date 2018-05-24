#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AvailableAudioRoutingEndpoints 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
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
	}
	#endif
}
