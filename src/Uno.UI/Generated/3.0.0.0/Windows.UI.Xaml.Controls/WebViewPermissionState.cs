#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WebViewPermissionState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Defer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Allow,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Deny,
		#endif
	}
	#endif
}
