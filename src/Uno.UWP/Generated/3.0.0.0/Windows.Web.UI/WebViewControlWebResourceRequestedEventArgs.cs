#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewControlWebResourceRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.HttpResponseMessage Response
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpResponseMessage WebViewControlWebResourceRequestedEventArgs.Response is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.WebViewControlWebResourceRequestedEventArgs", "HttpResponseMessage WebViewControlWebResourceRequestedEventArgs.Response");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.HttpRequestMessage Request
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpRequestMessage WebViewControlWebResourceRequestedEventArgs.Request is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral WebViewControlWebResourceRequestedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Web.UI.WebViewControlWebResourceRequestedEventArgs.Request.get
		// Forced skipping of method Windows.Web.UI.WebViewControlWebResourceRequestedEventArgs.Response.set
		// Forced skipping of method Windows.Web.UI.WebViewControlWebResourceRequestedEventArgs.Response.get
	}
}
