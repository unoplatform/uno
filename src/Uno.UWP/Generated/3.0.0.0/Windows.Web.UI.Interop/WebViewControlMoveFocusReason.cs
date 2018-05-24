#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI.Interop
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WebViewControlMoveFocusReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Programmatic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Next,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Previous,
		#endif
	}
	#endif
}
