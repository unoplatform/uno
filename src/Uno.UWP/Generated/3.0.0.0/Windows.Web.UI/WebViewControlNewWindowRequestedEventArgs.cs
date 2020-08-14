#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewControlNewWindowRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebViewControlNewWindowRequestedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs", "bool WebViewControlNewWindowRequestedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri Referrer
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebViewControlNewWindowRequestedEventArgs.Referrer is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebViewControlNewWindowRequestedEventArgs.Uri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.UI.IWebViewControl NewWindow
		{
			get
			{
				throw new global::System.NotImplementedException("The member IWebViewControl WebViewControlNewWindowRequestedEventArgs.NewWindow is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs", "IWebViewControl WebViewControlNewWindowRequestedEventArgs.NewWindow");
			}
		}
		#endif
		// Forced skipping of method Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs.Uri.get
		// Forced skipping of method Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs.Referrer.get
		// Forced skipping of method Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs.Handled.get
		// Forced skipping of method Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs.Handled.set
		// Forced skipping of method Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs.NewWindow.get
		// Forced skipping of method Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs.NewWindow.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral WebViewControlNewWindowRequestedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
