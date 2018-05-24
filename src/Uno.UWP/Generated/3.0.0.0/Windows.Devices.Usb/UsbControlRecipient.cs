#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UsbControlRecipient 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Device,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SpecifiedInterface,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Endpoint,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DefaultInterface,
		#endif
	}
	#endif
}
