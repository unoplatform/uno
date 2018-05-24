#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ArcadeStickButtons 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StickUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StickDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StickLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StickRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Action1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Action2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Action3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Action4,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Action5,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Action6,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Special1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Special2,
		#endif
	}
	#endif
}
