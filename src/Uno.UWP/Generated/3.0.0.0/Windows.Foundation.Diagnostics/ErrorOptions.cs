#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ErrorOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SuppressExceptions,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ForceExceptions,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseSetErrorInfo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SuppressSetErrorInfo,
		#endif
	}
	#endif
}
