#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.PhoneNumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhoneNumberFormat 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		E164,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		International,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		National,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rfc3966,
		#endif
	}
	#endif
}
