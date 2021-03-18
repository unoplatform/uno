#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewWebResourceRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.HttpResponseMessage Response
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpResponseMessage WebViewWebResourceRequestedEventArgs.Response is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebViewWebResourceRequestedEventArgs", "HttpResponseMessage WebViewWebResourceRequestedEventArgs.Response");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.HttpRequestMessage Request
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpRequestMessage WebViewWebResourceRequestedEventArgs.Request is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.WebViewWebResourceRequestedEventArgs.Request.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebViewWebResourceRequestedEventArgs.Response.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebViewWebResourceRequestedEventArgs.Response.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral WebViewWebResourceRequestedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
