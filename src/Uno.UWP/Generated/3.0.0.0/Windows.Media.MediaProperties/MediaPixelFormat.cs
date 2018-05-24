#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.MediaProperties
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaPixelFormat 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Nv12,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bgra8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		P010,
		#endif
	}
	#endif
}
