#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TextLineBounds 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Full,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TrimToCapHeight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TrimToBaseline,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tight,
		#endif
	}
	#endif
}
