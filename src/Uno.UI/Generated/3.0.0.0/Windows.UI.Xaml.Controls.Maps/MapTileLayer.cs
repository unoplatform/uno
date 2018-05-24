#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Maps
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MapTileLayer 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LabelOverlay,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoadOverlay,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AreaOverlay,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BackgroundOverlay,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BackgroundReplacement,
		#endif
	}
	#endif
}
