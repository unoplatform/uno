#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PrintStaple 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PrinterCustom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StapleTopLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StapleTopRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StapleBottomLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StapleBottomRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StapleDualLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StapleDualRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StapleDualTop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StapleDualBottom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SaddleStitch,
		#endif
	}
	#endif
}
