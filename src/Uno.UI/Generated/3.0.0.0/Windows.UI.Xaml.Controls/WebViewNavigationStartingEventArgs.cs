#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewNavigationStartingEventArgs 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Cancel
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebViewNavigationStartingEventArgs.Cancel is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebViewNavigationStartingEventArgs", "bool WebViewNavigationStartingEventArgs.Cancel");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebViewNavigationStartingEventArgs.Uri is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.WebViewNavigationStartingEventArgs.Uri.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebViewNavigationStartingEventArgs.Cancel.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebViewNavigationStartingEventArgs.Cancel.set
	}
}
