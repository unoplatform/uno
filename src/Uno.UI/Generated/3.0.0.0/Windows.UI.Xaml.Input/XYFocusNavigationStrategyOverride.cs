#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum XYFocusNavigationStrategyOverride 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Auto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Projection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NavigationDirectionDistance,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RectilinearDistance,
		#endif
	}
	#endif
}
