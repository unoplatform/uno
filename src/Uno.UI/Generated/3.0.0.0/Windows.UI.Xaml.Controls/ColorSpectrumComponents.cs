#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ColorSpectrumComponents 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HueValue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ValueHue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HueSaturation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SaturationHue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SaturationValue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ValueSaturation,
		#endif
	}
	#endif
}
