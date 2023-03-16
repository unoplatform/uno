#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
#endif
	public partial class CoreWebView2DevToolsProtocolEventReceivedEventArgs
	{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public string ParameterObjectAsJson
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2DevToolsProtocolEventReceivedEventArgs.ParameterObjectAsJson is not implemented in Uno.");
			}
		}
#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DevToolsProtocolEventReceivedEventArgs.ParameterObjectAsJson.get
	}
}
