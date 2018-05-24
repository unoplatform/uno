#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CaptureSceneMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Auto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Manual,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Macro,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Portrait,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sport,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Snow,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Night,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Beach,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sunset,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Candlelight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Landscape,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NightPortrait,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Backlit,
		#endif
	}
	#endif
}
