#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FrameFlashMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Enable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Global,
		#endif
	}
	#endif
}
