#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
#endif
	public partial class CoreWebView2WebResourceRequestedEventArgs
	{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponse Response
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2WebResourceResponse CoreWebView2WebResourceRequestedEventArgs.Response is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs", "CoreWebView2WebResourceResponse CoreWebView2WebResourceRequestedEventArgs.Response");
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequest Request
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2WebResourceRequest CoreWebView2WebResourceRequestedEventArgs.Request is not implemented in Uno.");
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext ResourceContext
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2WebResourceContext CoreWebView2WebResourceRequestedEventArgs.ResourceContext is not implemented in Uno.");
			}
		}
#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs.Request.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs.Response.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs.Response.set
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs.ResourceContext.get
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral CoreWebView2WebResourceRequestedEventArgs.GetDeferral() is not implemented in Uno.");
		}
#endif
	}
}
