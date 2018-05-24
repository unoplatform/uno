#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SmsEncoding 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Optimal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SevenBitAscii,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unicode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GsmSevenBit,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EightBit,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Latin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Korean,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IA5,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ShiftJis,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LatinHebrew,
		#endif
	}
	#endif
}
