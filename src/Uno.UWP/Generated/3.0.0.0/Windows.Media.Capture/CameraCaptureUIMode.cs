#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CameraCaptureUIMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhotoOrVideo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Photo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Video,
		#endif
	}
	#endif
}
