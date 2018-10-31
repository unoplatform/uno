#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationViewItemInvokedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object InvokedItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member object NavigationViewItemInvokedEventArgs.InvokedItem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsSettingsInvoked
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool NavigationViewItemInvokedEventArgs.IsSettingsInvoked is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public NavigationViewItemInvokedEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs", "NavigationViewItemInvokedEventArgs.NavigationViewItemInvokedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs.NavigationViewItemInvokedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs.InvokedItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs.IsSettingsInvoked.get
	}
}
