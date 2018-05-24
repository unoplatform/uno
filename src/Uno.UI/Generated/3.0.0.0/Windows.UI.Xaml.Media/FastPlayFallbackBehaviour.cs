#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FastPlayFallbackBehaviour 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Skip,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hide,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disable,
		#endif
	}
	#endif
}
