#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaElementState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Closed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Opening,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Buffering,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Playing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paused,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stopped,
		#endif
	}
	#endif
}
