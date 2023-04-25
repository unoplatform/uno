#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2ContentLoadingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsErrorPage
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWebView2ContentLoadingEventArgs.IsErrorPage is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreWebView2ContentLoadingEventArgs.IsErrorPage");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong NavigationId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong CoreWebView2ContentLoadingEventArgs.NavigationId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20CoreWebView2ContentLoadingEventArgs.NavigationId");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2ContentLoadingEventArgs.IsErrorPage.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2ContentLoadingEventArgs.NavigationId.get
	}
}
