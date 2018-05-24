#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Preview.Injection
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InjectedInputMouseOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Move,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MiddleDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MiddleUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		XDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		XUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wheel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HWheel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MoveNoCoalesce,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VirtualDesk,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Absolute,
		#endif
	}
	#endif
}
