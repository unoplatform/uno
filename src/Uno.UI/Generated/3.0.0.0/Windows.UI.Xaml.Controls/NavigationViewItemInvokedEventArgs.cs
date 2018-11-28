#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationViewItemInvokedEventArgs 
	{
		// Skipping already declared property InvokedItem
		// Skipping already declared property IsSettingsInvoked
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.NavigationViewItemBase InvokedItemContainer
		{
			get
			{
				throw new global::System.NotImplementedException("The member NavigationViewItemBase NavigationViewItemInvokedEventArgs.InvokedItemContainer is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo RecommendedNavigationTransitionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member NavigationTransitionInfo NavigationViewItemInvokedEventArgs.RecommendedNavigationTransitionInfo is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs.NavigationViewItemInvokedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs.NavigationViewItemInvokedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs.InvokedItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs.IsSettingsInvoked.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs.InvokedItemContainer.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs.RecommendedNavigationTransitionInfo.get
	}
}
