#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaPlaybackStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Closed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Changing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stopped,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Playing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paused,
		#endif
	}
	#endif
}
