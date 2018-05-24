#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DisplayBrightnessScenario 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DefaultBrightness,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IdleBrightness,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BarcodeReadingBrightness,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FullBrightness,
		#endif
	}
	#endif
}
