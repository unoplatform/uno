#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PointerUpdateKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftButtonPressed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftButtonReleased,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightButtonPressed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightButtonReleased,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MiddleButtonPressed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MiddleButtonReleased,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		XButton1Pressed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		XButton1Released,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		XButton2Pressed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		XButton2Released,
		#endif
	}
	#endif
}
