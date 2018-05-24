#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FocusInputDeviceKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mouse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Touch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pen,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Keyboard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GameController,
		#endif
	}
	#endif
}
