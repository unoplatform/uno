#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WiFiDirectAdvertisementPublisherStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Created,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Started,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stopped,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aborted,
		#endif
	}
	#endif
}
