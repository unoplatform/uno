#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2DevToolsProtocolEventReceiver 
	{
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DevToolsProtocolEventReceiver.DevToolsProtocolEventReceived.add
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DevToolsProtocolEventReceiver.DevToolsProtocolEventReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Microsoft.Web.WebView2.Core.CoreWebView2, global::Microsoft.Web.WebView2.Core.CoreWebView2DevToolsProtocolEventReceivedEventArgs> DevToolsProtocolEventReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2DevToolsProtocolEventReceiver", "event TypedEventHandler<CoreWebView2, CoreWebView2DevToolsProtocolEventReceivedEventArgs> CoreWebView2DevToolsProtocolEventReceiver.DevToolsProtocolEventReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2DevToolsProtocolEventReceiver", "event TypedEventHandler<CoreWebView2, CoreWebView2DevToolsProtocolEventReceivedEventArgs> CoreWebView2DevToolsProtocolEventReceiver.DevToolsProtocolEventReceived");
			}
		}
		#endif
	}
}
