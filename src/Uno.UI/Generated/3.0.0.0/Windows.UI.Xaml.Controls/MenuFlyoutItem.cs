#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class MenuFlyoutItem : global::Windows.UI.Xaml.Controls.MenuFlyoutItemBase
	{
		// Skipping already declared property Text
		// Skipping already declared property CommandParameter
		// Skipping already declared property Command
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
		public  string KeyboardAcceleratorTextOverride
		{
			get
			{
				return (string)this.GetValue(KeyboardAcceleratorTextOverrideProperty);
			}
			set
			{
				this.SetValue(KeyboardAcceleratorTextOverrideProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.MenuFlyoutItemTemplateSettings TemplateSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member MenuFlyoutItemTemplateSettings MenuFlyoutItem.TemplateSettings is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property CommandParameterProperty
		// Skipping already declared property CommandProperty
		// Skipping already declared property TextProperty
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IconProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Icon", typeof(global::Windows.UI.Xaml.Controls.IconElement), 
			typeof(global::Windows.UI.Xaml.Controls.MenuFlyoutItem), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.IconElement)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty KeyboardAcceleratorTextOverrideProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"KeyboardAcceleratorTextOverride", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.MenuFlyoutItem), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.MenuFlyoutItem.MenuFlyoutItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.MenuFlyoutItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.Text.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.Text.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.Command.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.Command.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.CommandParameter.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.CommandParameter.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.Click.add
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.Click.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.Icon.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.Icon.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.KeyboardAcceleratorTextOverride.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.KeyboardAcceleratorTextOverride.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.TemplateSettings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.KeyboardAcceleratorTextOverrideProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.IconProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.TextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.CommandProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutItem.CommandParameterProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.RoutedEventHandler Click
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.MenuFlyoutItem", "event RoutedEventHandler MenuFlyoutItem.Click");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.MenuFlyoutItem", "event RoutedEventHandler MenuFlyoutItem.Click");
			}
		}
		#endif
	}
}
