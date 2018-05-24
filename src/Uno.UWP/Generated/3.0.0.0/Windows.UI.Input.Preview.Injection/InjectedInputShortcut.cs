#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Preview.Injection
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InjectedInputShortcut 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Back,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Start,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Search,
		#endif
	}
	#endif
}
