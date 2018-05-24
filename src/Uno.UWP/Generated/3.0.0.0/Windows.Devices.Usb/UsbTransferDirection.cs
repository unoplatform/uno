#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UsbTransferDirection 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Out,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		In,
		#endif
	}
	#endif
}
