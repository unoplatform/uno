#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Render
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AudioRenderCategory 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ForegroundOnlyMedia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BackgroundCapableMedia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Communications,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Alerts,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SoundEffects,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GameEffects,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GameMedia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GameChat,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Speech,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Movie,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Media,
		#endif
	}
	#endif
}
