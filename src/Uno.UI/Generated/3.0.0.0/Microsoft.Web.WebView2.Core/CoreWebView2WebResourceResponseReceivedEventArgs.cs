#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2WebResourceResponseReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequest Request
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2WebResourceRequest CoreWebView2WebResourceResponseReceivedEventArgs.Request is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2WebResourceRequest%20CoreWebView2WebResourceResponseReceivedEventArgs.Request");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponseView Response
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2WebResourceResponseView CoreWebView2WebResourceResponseReceivedEventArgs.Response is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2WebResourceResponseView%20CoreWebView2WebResourceResponseReceivedEventArgs.Response");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponseReceivedEventArgs.Request.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponseReceivedEventArgs.Response.get
	}
}
