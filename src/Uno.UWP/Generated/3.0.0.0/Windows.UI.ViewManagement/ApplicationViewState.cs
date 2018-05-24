#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ApplicationViewState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FullScreenLandscape,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Filled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Snapped,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FullScreenPortrait,
		#endif
	}
	#endif
}
