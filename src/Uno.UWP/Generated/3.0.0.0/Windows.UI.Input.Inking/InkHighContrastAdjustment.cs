#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InkHighContrastAdjustment 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseSystemColorsWhenNecessary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseSystemColors,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseOriginalColors,
		#endif
	}
	#endif
}
