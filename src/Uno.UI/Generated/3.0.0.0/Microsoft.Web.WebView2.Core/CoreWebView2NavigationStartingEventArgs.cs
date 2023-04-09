#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2NavigationStartingEventArgs 
	{
		// Skipping already declared property Cancel
		// Skipping already declared property IsRedirected
		// Skipping already declared property IsUserInitiated
		// Skipping already declared property NavigationId
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2HttpRequestHeaders RequestHeaders
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2HttpRequestHeaders CoreWebView2NavigationStartingEventArgs.RequestHeaders is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2HttpRequestHeaders%20CoreWebView2NavigationStartingEventArgs.RequestHeaders");
			}
		}
		#endif
		// Skipping already declared property Uri
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs.Uri.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs.IsUserInitiated.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs.IsRedirected.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs.RequestHeaders.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs.Cancel.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs.Cancel.set
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs.NavigationId.get
	}
}
