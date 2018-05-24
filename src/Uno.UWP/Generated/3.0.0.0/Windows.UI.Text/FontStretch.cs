#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FontStretch 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Undefined,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UltraCondensed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ExtraCondensed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Condensed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SemiCondensed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Normal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SemiExpanded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Expanded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ExtraExpanded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UltraExpanded,
		#endif
	}
	#endif
}
