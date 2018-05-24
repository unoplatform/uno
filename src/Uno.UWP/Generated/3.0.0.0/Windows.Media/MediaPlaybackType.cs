#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaPlaybackType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Music,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Video,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Image,
		#endif
	}
	#endif
}
