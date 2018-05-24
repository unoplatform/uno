#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.NumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RoundingAlgorithm 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoundDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoundUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoundTowardsZero,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoundAwayFromZero,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoundHalfDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoundHalfUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoundHalfTowardsZero,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoundHalfAwayFromZero,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoundHalfToEven,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoundHalfToOdd,
		#endif
	}
	#endif
}
