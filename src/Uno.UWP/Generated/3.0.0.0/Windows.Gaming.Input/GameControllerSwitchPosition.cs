#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GameControllerSwitchPosition 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Center,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Up,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UpRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Right,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DownRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Down,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DownLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Left,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UpLeft,
		#endif
	}
	#endif
}
