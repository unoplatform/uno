#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewControlUnsupportedUriSchemeIdentifiedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebViewControlUnsupportedUriSchemeIdentifiedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.WebViewControlUnsupportedUriSchemeIdentifiedEventArgs", "bool WebViewControlUnsupportedUriSchemeIdentifiedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebViewControlUnsupportedUriSchemeIdentifiedEventArgs.Uri is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Web.UI.WebViewControlUnsupportedUriSchemeIdentifiedEventArgs.Uri.get
		// Forced skipping of method Windows.Web.UI.WebViewControlUnsupportedUriSchemeIdentifiedEventArgs.Handled.get
		// Forced skipping of method Windows.Web.UI.WebViewControlUnsupportedUriSchemeIdentifiedEventArgs.Handled.set
	}
}
