#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MenuBarItem : global::Windows.UI.Xaml.Controls.Control
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Title
		{
			get
			{
				return (string)this.GetValue(TitleProperty);
			}
			set
			{
				this.SetValue(TitleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.MenuFlyoutItemBase> Items
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.MenuFlyoutItemBase>)this.GetValue(ItemsProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Items", typeof(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.MenuFlyoutItemBase>), 
			typeof(global::Windows.UI.Xaml.Controls.MenuBarItem), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.MenuFlyoutItemBase>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TitleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Title", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.MenuBarItem), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public MenuBarItem() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.MenuBarItem", "MenuBarItem.MenuBarItem()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuBarItem.MenuBarItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuBarItem.Title.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuBarItem.Title.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuBarItem.Items.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuBarItem.TitleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuBarItem.ItemsProperty.get
	}
}
