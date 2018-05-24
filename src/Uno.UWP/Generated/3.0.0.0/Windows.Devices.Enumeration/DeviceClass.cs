#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DeviceClass 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		All,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioCapture,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioRender,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PortableStorageDevice,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VideoCapture,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ImageScanner,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Location,
		#endif
	}
	#endif
}
