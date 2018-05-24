#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.PhoneNumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PredictedPhoneNumberKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FixedLine,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mobile,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FixedLineOrMobile,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TollFree,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PremiumRate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SharedCost,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Voip,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PersonalNumber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pager,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UniversalAccountNumber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Voicemail,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
	}
	#endif
}
