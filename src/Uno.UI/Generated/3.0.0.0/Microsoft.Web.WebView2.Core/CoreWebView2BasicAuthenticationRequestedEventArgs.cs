#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2BasicAuthenticationRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Cancel
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWebView2BasicAuthenticationRequestedEventArgs.Cancel is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreWebView2BasicAuthenticationRequestedEventArgs.Cancel");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationRequestedEventArgs", "bool CoreWebView2BasicAuthenticationRequestedEventArgs.Cancel");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Challenge
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2BasicAuthenticationRequestedEventArgs.Challenge is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2BasicAuthenticationRequestedEventArgs.Challenge");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationResponse Response
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2BasicAuthenticationResponse CoreWebView2BasicAuthenticationRequestedEventArgs.Response is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2BasicAuthenticationResponse%20CoreWebView2BasicAuthenticationRequestedEventArgs.Response");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2BasicAuthenticationRequestedEventArgs.Uri is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2BasicAuthenticationRequestedEventArgs.Uri");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationRequestedEventArgs.Uri.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationRequestedEventArgs.Challenge.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationRequestedEventArgs.Response.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationRequestedEventArgs.Cancel.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationRequestedEventArgs.Cancel.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral CoreWebView2BasicAuthenticationRequestedEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20CoreWebView2BasicAuthenticationRequestedEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
