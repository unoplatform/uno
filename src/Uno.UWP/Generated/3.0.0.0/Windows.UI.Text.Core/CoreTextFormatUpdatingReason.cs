#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CoreTextFormatUpdatingReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CompositionUnconverted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CompositionConverted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CompositionTargetUnconverted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CompositionTargetConverted,
		#endif
	}
	#endif
}
