#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2CookieManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Microsoft.Web.WebView2.Core.CoreWebView2Cookie>> GetCookiesAsync( string uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<CoreWebView2Cookie>> CoreWebView2CookieManager.GetCookiesAsync(string uri) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIReadOnlyList%3CCoreWebView2Cookie%3E%3E%20CoreWebView2CookieManager.GetCookiesAsync%28string%20uri%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2Cookie CreateCookie( string name,  string value,  string Domain,  string Path)
		{
			throw new global::System.NotImplementedException("The member CoreWebView2Cookie CoreWebView2CookieManager.CreateCookie(string name, string value, string Domain, string Path) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2Cookie%20CoreWebView2CookieManager.CreateCookie%28string%20name%2C%20string%20value%2C%20string%20Domain%2C%20string%20Path%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2Cookie CopyCookie( global::Microsoft.Web.WebView2.Core.CoreWebView2Cookie cookieParam)
		{
			throw new global::System.NotImplementedException("The member CoreWebView2Cookie CoreWebView2CookieManager.CopyCookie(CoreWebView2Cookie cookieParam) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2Cookie%20CoreWebView2CookieManager.CopyCookie%28CoreWebView2Cookie%20cookieParam%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddOrUpdateCookie( global::Microsoft.Web.WebView2.Core.CoreWebView2Cookie cookie)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2CookieManager", "void CoreWebView2CookieManager.AddOrUpdateCookie(CoreWebView2Cookie cookie)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DeleteCookie( global::Microsoft.Web.WebView2.Core.CoreWebView2Cookie cookie)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2CookieManager", "void CoreWebView2CookieManager.DeleteCookie(CoreWebView2Cookie cookie)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DeleteCookies( string name,  string uri)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2CookieManager", "void CoreWebView2CookieManager.DeleteCookies(string name, string uri)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DeleteCookiesWithDomainAndPath( string name,  string Domain,  string Path)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2CookieManager", "void CoreWebView2CookieManager.DeleteCookiesWithDomainAndPath(string name, string Domain, string Path)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DeleteAllCookies()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2CookieManager", "void CoreWebView2CookieManager.DeleteAllCookies()");
		}
		#endif
	}
}
