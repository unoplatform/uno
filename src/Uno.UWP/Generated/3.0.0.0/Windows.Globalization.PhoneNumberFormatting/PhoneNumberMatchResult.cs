#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.PhoneNumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhoneNumberMatchResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoMatch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ShortNationalSignificantNumberMatch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NationalSignificantNumberMatch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ExactMatch,
		#endif
	}
	#endif
}
