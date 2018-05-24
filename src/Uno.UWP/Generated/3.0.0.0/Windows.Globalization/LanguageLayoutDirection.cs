#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LanguageLayoutDirection 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ltr,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rtl,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TtbLtr,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TtbRtl,
		#endif
	}
	#endif
}
