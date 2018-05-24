#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Preview.Injection
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InjectedInputKeyOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ExtendedKey,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KeyUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ScanCode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unicode,
		#endif
	}
	#endif
}
