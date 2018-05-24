#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.PhoneNumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhoneNumberParseResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Valid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotANumber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidCountryCode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooShort,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooLong,
		#endif
	}
	#endif
}
