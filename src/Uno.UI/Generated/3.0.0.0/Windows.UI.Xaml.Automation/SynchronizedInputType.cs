#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SynchronizedInputType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KeyUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KeyDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftMouseUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftMouseDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightMouseUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightMouseDown,
		#endif
	}
	#endif
}
