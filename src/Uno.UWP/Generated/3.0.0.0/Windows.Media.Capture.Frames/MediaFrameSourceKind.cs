#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Frames
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaFrameSourceKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Color,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Infrared,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Depth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Audio,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Image,
		#endif
	}
	#endif
}
