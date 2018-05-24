#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TextGetOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AdjustCrlf,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseCrlf,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseObjectText,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllowFinalEop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoHidden,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IncludeNumbering,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FormatRtf,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseLf,
		#endif
	}
	#endif
}
