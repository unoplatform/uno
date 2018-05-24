#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.MediaProperties
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VideoEncodingQuality 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Auto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HD1080p,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HD720p,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wvga,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ntsc,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Vga,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Qvga,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Uhd2160p,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Uhd4320p,
		#endif
	}
	#endif
}
