#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MenuBar : global::Windows.UI.Xaml.Controls.Control
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.MenuBarItem> Items
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.MenuBarItem>)this.GetValue(ItemsProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Items", typeof(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.MenuBarItem>), 
			typeof(global::Windows.UI.Xaml.Controls.MenuBar), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.MenuBarItem>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public MenuBar() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.MenuBar", "MenuBar.MenuBar()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuBar.MenuBar()
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuBar.Items.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MenuBar.ItemsProperty.get
	}
}
