#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FontVariants 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Normal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Superscript,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Subscript,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ordinal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Inferior,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ruby,
		#endif
	}
	#endif
}
