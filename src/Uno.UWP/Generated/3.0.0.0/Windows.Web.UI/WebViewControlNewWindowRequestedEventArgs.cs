#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewControlNewWindowRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Uri Referrer
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebViewControlNewWindowRequestedEventArgs.Referrer is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebViewControlNewWindowRequestedEventArgs.Uri is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs.Uri.get
		// Forced skipping of method Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs.Referrer.get
		// Forced skipping of method Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs.Handled.get
		// Forced skipping of method Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs.Handled.set
	}
}
