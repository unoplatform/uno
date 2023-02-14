#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2DownloadStartingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ResultFilePath
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2DownloadStartingEventArgs.ResultFilePath is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2DownloadStartingEventArgs.ResultFilePath");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs", "string CoreWebView2DownloadStartingEventArgs.ResultFilePath");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWebView2DownloadStartingEventArgs.Handled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreWebView2DownloadStartingEventArgs.Handled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs", "bool CoreWebView2DownloadStartingEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Cancel
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWebView2DownloadStartingEventArgs.Cancel is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreWebView2DownloadStartingEventArgs.Cancel");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs", "bool CoreWebView2DownloadStartingEventArgs.Cancel");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2DownloadOperation DownloadOperation
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2DownloadOperation CoreWebView2DownloadStartingEventArgs.DownloadOperation is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2DownloadOperation%20CoreWebView2DownloadStartingEventArgs.DownloadOperation");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs.DownloadOperation.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs.Cancel.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs.Cancel.set
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs.ResultFilePath.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs.ResultFilePath.set
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs.Handled.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs.Handled.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral CoreWebView2DownloadStartingEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20CoreWebView2DownloadStartingEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
