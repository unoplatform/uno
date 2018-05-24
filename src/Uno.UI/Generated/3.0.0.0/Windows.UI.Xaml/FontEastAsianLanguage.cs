#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FontEastAsianLanguage 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Normal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HojoKanji,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Jis04,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Jis78,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Jis83,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Jis90,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NlcKanji,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Simplified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Traditional,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TraditionalNames,
		#endif
	}
	#endif
}
