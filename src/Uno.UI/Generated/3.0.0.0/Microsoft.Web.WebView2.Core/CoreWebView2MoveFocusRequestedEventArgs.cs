#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2MoveFocusRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWebView2MoveFocusRequestedEventArgs.Handled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreWebView2MoveFocusRequestedEventArgs.Handled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2MoveFocusRequestedEventArgs", "bool CoreWebView2MoveFocusRequestedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2MoveFocusReason Reason
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2MoveFocusReason CoreWebView2MoveFocusRequestedEventArgs.Reason is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2MoveFocusReason%20CoreWebView2MoveFocusRequestedEventArgs.Reason");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2MoveFocusRequestedEventArgs.Reason.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2MoveFocusRequestedEventArgs.Handled.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2MoveFocusRequestedEventArgs.Handled.set
	}
}
