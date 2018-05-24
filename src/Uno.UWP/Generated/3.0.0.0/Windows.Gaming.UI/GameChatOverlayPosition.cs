#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.UI
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GameChatOverlayPosition 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BottomCenter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BottomLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BottomRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MiddleRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MiddleLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TopCenter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TopLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TopRight,
		#endif
	}
	#endif
}
