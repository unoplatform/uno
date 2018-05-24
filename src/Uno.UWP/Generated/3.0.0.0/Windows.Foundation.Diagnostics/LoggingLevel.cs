#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LoggingLevel 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Verbose,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Information,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Warning,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Error,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Critical,
		#endif
	}
	#endif
}
