#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Preview.Injection
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InjectedInputPenButtons 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Barrel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Inverted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Eraser,
		#endif
	}
	#endif
}
