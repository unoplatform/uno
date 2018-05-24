#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CompositionColorSpace 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Auto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hsl,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rgb,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HslLinear,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RgbLinear,
		#endif
	}
	#endif
}
