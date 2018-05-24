#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ViewSizePreference 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseLess,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseHalf,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseMore,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseMinimum,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseNone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
	}
	#endif
}
