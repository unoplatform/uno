#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationViewItem : global::Windows.UI.Xaml.Controls.NavigationViewItemBase
	{
		// Skipping already declared property Icon
		// Skipping already declared property CompactPaneLength
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool SelectsOnInvoked
		{
			get
			{
				return (bool)this.GetValue(SelectsOnInvokedProperty);
			}
			set
			{
				this.SetValue(SelectsOnInvokedProperty, value);
			}
		}
		#endif
		// Skipping already declared property CompactPaneLengthProperty
		// Skipping already declared property IconProperty
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectsOnInvokedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectsOnInvoked", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewItem), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.NavigationViewItem.NavigationViewItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.NavigationViewItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.Icon.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.Icon.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.CompactPaneLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.SelectsOnInvoked.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.SelectsOnInvoked.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.SelectsOnInvokedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.IconProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.CompactPaneLengthProperty.get
	}
}
