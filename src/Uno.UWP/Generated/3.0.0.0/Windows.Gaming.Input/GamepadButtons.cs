#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GamepadButtons 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Menu,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		View,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		A,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		B,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		X,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Y,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DPadUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DPadDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DPadLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DPadRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftShoulder,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightShoulder,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftThumbstick,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightThumbstick,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paddle1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paddle2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paddle3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paddle4,
		#endif
	}
	#endif
}
