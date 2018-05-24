#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToggleMenuFlyoutItem : global::Windows.UI.Xaml.Controls.MenuFlyoutItem
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsChecked
		{
			get
			{
				return (bool)this.GetValue(IsCheckedProperty);
			}
			set
			{
				this.SetValue(IsCheckedProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsCheckedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsChecked", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ToggleMenuFlyoutItem() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem", "ToggleMenuFlyoutItem.ToggleMenuFlyoutItem()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem.ToggleMenuFlyoutItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem.IsChecked.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem.IsChecked.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem.IsCheckedProperty.get
	}
}
