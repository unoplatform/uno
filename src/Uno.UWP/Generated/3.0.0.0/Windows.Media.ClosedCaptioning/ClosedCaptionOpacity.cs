#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.ClosedCaptioning
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ClosedCaptionOpacity 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OneHundredPercent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SeventyFivePercent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TwentyFivePercent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ZeroPercent,
		#endif
	}
	#endif
}
