#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Preview.Injection
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InjectedInputButtonChangeKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FirstButtonDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FirstButtonUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SecondButtonDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SecondButtonUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ThirdButtonDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ThirdButtonUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FourthButtonDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FourthButtonUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FifthButtonDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FifthButtonUp,
		#endif
	}
	#endif
}
