#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum OptionalUINavigationButtons 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Context1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Context2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Context3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Context4,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PageUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PageDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PageLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PageRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ScrollUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ScrollDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ScrollLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ScrollRight,
		#endif
	}
	#endif
}
