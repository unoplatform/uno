#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MarkerStyle 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Undefined,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Parenthesis,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Parentheses,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Period,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Plain,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Minus,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoNumber,
		#endif
	}
	#endif
}
