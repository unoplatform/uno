#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationViewItem : global::Windows.UI.Xaml.Controls.NavigationViewItemBase
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.IconElement Icon
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.IconElement)this.GetValue(IconProperty);
			}
			set
			{
				this.SetValue(IconProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double CompactPaneLength
		{
			get
			{
				return (double)this.GetValue(CompactPaneLengthProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CompactPaneLengthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CompactPaneLength", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewItem), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IconProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Icon", typeof(global::Windows.UI.Xaml.Controls.IconElement), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewItem), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.IconElement)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public NavigationViewItem() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationViewItem", "NavigationViewItem.NavigationViewItem()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.NavigationViewItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.Icon.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.Icon.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.CompactPaneLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.IconProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewItem.CompactPaneLengthProperty.get
	}
}
