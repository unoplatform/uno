#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MenuFlyoutSubItem : global::Windows.UI.Xaml.Controls.MenuFlyoutItemBase
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Text
		{
			get
			{
				return (string)this.GetValue(TextProperty);
			}
			set
			{
				this.SetValue(TextProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.MenuFlyoutItemBase> Items
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<MenuFlyoutItemBase> MenuFlyoutSubItem.Items is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Text", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.MenuFlyoutSubItem), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IconProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Icon", typeof(global::Windows.UI.Xaml.Controls.IconElement), 
			typeof(global::Windows.UI.Xaml.Controls.MenuFlyoutSubItem), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.IconElement)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public MenuFlyoutSubItem() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.MenuFlyoutSubItem", "MenuFlyoutSubItem.MenuFlyoutSubItem()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutSubItem.MenuFlyoutSubItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutSubItem.Items.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutSubItem.Text.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutSubItem.Text.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutSubItem.Icon.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutSubItem.Icon.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutSubItem.IconProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuFlyoutSubItem.TextProperty.get
	}
}
