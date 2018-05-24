#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Preview.Injection
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InjectedInputTouchParameters 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Contact,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Orientation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pressure,
		#endif
	}
	#endif
}
