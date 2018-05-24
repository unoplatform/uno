#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaCaptureFocusState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Uninitialized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Lost,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Searching,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Focused,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Failed,
		#endif
	}
	#endif
}
