#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2ProcessInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2ProcessKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2ProcessKind CoreWebView2ProcessInfo.Kind is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2ProcessKind%20CoreWebView2ProcessInfo.Kind");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int ProcessId
		{
			get
			{
				throw new global::System.NotImplementedException("The member int CoreWebView2ProcessInfo.ProcessId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20CoreWebView2ProcessInfo.ProcessId");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2ProcessInfo.ProcessId.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2ProcessInfo.Kind.get
	}
}
