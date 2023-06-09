#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2DOMContentLoadedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong NavigationId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong CoreWebView2DOMContentLoadedEventArgs.NavigationId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20CoreWebView2DOMContentLoadedEventArgs.NavigationId");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs.NavigationId.get
	}
}
