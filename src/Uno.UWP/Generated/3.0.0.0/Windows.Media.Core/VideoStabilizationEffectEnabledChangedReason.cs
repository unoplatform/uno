#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VideoStabilizationEffectEnabledChangedReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Programmatic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PixelRateTooHigh,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RunningSlowly,
		#endif
	}
	#endif
}
