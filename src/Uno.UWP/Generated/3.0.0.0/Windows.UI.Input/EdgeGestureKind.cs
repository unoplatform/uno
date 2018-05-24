#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EdgeGestureKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Touch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Keyboard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mouse,
		#endif
	}
	#endif
}
