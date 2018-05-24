#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Spatial
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpatialGestureSettings 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tap,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DoubleTap,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hold,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationTranslate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NavigationX,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NavigationY,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NavigationZ,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NavigationRailsX,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NavigationRailsY,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NavigationRailsZ,
		#endif
	}
	#endif
}
