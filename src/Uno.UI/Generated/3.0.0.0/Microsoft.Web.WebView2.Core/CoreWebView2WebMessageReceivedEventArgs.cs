#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2WebMessageReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2WebMessageReceivedEventArgs.Source is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2WebMessageReceivedEventArgs.Source");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string WebMessageAsJson
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2WebMessageReceivedEventArgs.WebMessageAsJson is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2WebMessageReceivedEventArgs.WebMessageAsJson");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs.Source.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs.WebMessageAsJson.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string TryGetWebMessageAsString()
		{
			throw new global::System.NotImplementedException("The member string CoreWebView2WebMessageReceivedEventArgs.TryGetWebMessageAsString() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2WebMessageReceivedEventArgs.TryGetWebMessageAsString%28%29");
		}
		#endif
	}
}
