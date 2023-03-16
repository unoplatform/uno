#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
#endif
	public partial class CoreWebView2WebResourceResponseView
	{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Microsoft.Web.WebView2.Core.CoreWebView2HttpResponseHeaders Headers
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2HttpResponseHeaders CoreWebView2WebResourceResponseView.Headers is not implemented in Uno.");
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public string ReasonPhrase
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2WebResourceResponseView.ReasonPhrase is not implemented in Uno.");
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public int StatusCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member int CoreWebView2WebResourceResponseView.StatusCode is not implemented in Uno.");
			}
		}
#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponseView.Headers.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponseView.StatusCode.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponseView.ReasonPhrase.get
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> GetContentAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> CoreWebView2WebResourceResponseView.GetContentAsync() is not implemented in Uno.");
		}
#endif
	}
}
