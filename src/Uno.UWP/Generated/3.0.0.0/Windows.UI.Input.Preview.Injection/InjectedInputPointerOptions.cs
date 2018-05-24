#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Preview.Injection
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InjectedInputPointerOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		New,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InRange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InContact,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FirstButton,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SecondButton,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Primary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Confidence,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Canceled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PointerDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Update,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PointerUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CaptureChanged,
		#endif
	}
	#endif
}
