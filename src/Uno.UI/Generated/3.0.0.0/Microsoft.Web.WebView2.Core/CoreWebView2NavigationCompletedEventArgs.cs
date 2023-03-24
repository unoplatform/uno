#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2NavigationCompletedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSuccess
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWebView2NavigationCompletedEventArgs.IsSuccess is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreWebView2NavigationCompletedEventArgs.IsSuccess");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong NavigationId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong CoreWebView2NavigationCompletedEventArgs.NavigationId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20CoreWebView2NavigationCompletedEventArgs.NavigationId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2WebErrorStatus WebErrorStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2WebErrorStatus CoreWebView2NavigationCompletedEventArgs.WebErrorStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2WebErrorStatus%20CoreWebView2NavigationCompletedEventArgs.WebErrorStatus");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs.IsSuccess.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs.WebErrorStatus.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs.NavigationId.get
	}
}
