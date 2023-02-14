#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2BasicAuthenticationResponse 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string UserName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2BasicAuthenticationResponse.UserName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2BasicAuthenticationResponse.UserName");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationResponse", "string CoreWebView2BasicAuthenticationResponse.UserName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Password
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2BasicAuthenticationResponse.Password is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2BasicAuthenticationResponse.Password");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationResponse", "string CoreWebView2BasicAuthenticationResponse.Password");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationResponse.UserName.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationResponse.UserName.set
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationResponse.Password.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2BasicAuthenticationResponse.Password.set
	}
}
