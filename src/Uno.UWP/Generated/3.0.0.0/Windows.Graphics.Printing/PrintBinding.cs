#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PrintBinding 
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
		Bale,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BindBottom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BindLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BindRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BindTop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Booklet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EdgeStitchBottom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EdgeStitchLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EdgeStitchRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EdgeStitchTop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Fold,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JogOffset,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Trim,
		#endif
	}
	#endif
}
