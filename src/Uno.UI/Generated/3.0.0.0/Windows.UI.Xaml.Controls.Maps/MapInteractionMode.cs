#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Maps
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MapInteractionMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Auto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GestureOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PointerAndKeyboard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ControlOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GestureAndControl,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PointerKeyboardAndControl,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PointerOnly,
		#endif
	}
	#endif
}
