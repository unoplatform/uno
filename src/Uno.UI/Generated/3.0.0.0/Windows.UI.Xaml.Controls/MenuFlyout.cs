#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class MenuFlyout 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style MenuFlyoutPresenterStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(MenuFlyoutPresenterStyleProperty);
			}
			set
			{
				this.SetValue(MenuFlyoutPresenterStyleProperty, value);
			}
		}
		#endif
		// Skipping already declared property Items
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MenuFlyoutPresenterStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuFlyoutPresenterStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.MenuFlyout), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.MenuFlyout.MenuFlyout()
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyout.MenuFlyout()
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyout.Items.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyout.MenuFlyoutPresenterStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyout.MenuFlyoutPresenterStyle.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void ShowAt( global::Windows.UI.Xaml.UIElement targetElement,  global::Windows.Foundation.Point point)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.MenuFlyout", "void MenuFlyout.ShowAt(UIElement targetElement, Point point)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyout.MenuFlyoutPresenterStyleProperty.get
	}
}
