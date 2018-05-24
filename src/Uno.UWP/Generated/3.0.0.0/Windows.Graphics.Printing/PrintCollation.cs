#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PrintCollation 
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
		Collated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Uncollated,
		#endif
	}
	#endif
}
