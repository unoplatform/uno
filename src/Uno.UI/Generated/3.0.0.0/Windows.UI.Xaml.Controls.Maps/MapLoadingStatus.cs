#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Maps
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MapLoadingStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Loading,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Loaded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DataUnavailable,
		#endif
	}
	#endif
}
