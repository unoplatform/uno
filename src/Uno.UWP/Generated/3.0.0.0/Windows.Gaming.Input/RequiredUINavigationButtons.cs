#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RequiredUINavigationButtons 
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
		Accept,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cancel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Up,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Down,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Left,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Right,
		#endif
	}
	#endif
}
