#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2NewWindowRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2 NewWindow
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2 CoreWebView2NewWindowRequestedEventArgs.NewWindow is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2%20CoreWebView2NewWindowRequestedEventArgs.NewWindow");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs", "CoreWebView2 CoreWebView2NewWindowRequestedEventArgs.NewWindow");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWebView2NewWindowRequestedEventArgs.Handled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreWebView2NewWindowRequestedEventArgs.Handled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs", "bool CoreWebView2NewWindowRequestedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsUserInitiated
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWebView2NewWindowRequestedEventArgs.IsUserInitiated is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreWebView2NewWindowRequestedEventArgs.IsUserInitiated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2NewWindowRequestedEventArgs.Uri is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2NewWindowRequestedEventArgs.Uri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2WindowFeatures WindowFeatures
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2WindowFeatures CoreWebView2NewWindowRequestedEventArgs.WindowFeatures is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2WindowFeatures%20CoreWebView2NewWindowRequestedEventArgs.WindowFeatures");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2NewWindowRequestedEventArgs.Name is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2NewWindowRequestedEventArgs.Name");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs.Name.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs.Uri.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs.NewWindow.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs.NewWindow.set
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs.Handled.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs.Handled.set
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs.IsUserInitiated.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs.WindowFeatures.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral CoreWebView2NewWindowRequestedEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20CoreWebView2NewWindowRequestedEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
