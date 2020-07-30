#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewControlNavigationCompletedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSuccess
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebViewControlNavigationCompletedEventArgs.IsSuccess is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebViewControlNavigationCompletedEventArgs.Uri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.WebErrorStatus WebErrorStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebErrorStatus WebViewControlNavigationCompletedEventArgs.WebErrorStatus is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Web.UI.WebViewControlNavigationCompletedEventArgs.Uri.get
		// Forced skipping of method Windows.Web.UI.WebViewControlNavigationCompletedEventArgs.IsSuccess.get
		// Forced skipping of method Windows.Web.UI.WebViewControlNavigationCompletedEventArgs.WebErrorStatus.get
	}
}
