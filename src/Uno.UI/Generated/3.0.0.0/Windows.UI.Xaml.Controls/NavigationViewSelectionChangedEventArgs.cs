#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationViewSelectionChangedEventArgs 
	{
		// Skipping already declared property IsSettingsSelected
		// Skipping already declared property SelectedItem
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo RecommendedNavigationTransitionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member NavigationTransitionInfo NavigationViewSelectionChangedEventArgs.RecommendedNavigationTransitionInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.NavigationViewItemBase SelectedItemContainer
		{
			get
			{
				throw new global::System.NotImplementedException("The member NavigationViewItemBase NavigationViewSelectionChangedEventArgs.SelectedItemContainer is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs.SelectedItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs.IsSettingsSelected.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs.SelectedItemContainer.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs.RecommendedNavigationTransitionInfo.get
	}
}
