#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NDCertificatePlatformID 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Windows,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OSX,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WindowsOnARM,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WindowsMobile7,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		iOSOnARM,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		XBoxOnPPC,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WindowsPhone8OnARM,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WindowsPhone8OnX86,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		XboxOne,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AndroidOnARM,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WindowsPhone81OnARM,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WindowsPhone81OnX86,
		#endif
	}
	#endif
}
