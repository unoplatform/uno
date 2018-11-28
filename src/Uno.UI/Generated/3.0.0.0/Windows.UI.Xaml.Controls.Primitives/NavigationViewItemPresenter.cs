#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationViewItemPresenter : global::Windows.UI.Xaml.Controls.ContentControl
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
		public static global::Windows.UI.Xaml.DependencyProperty IconProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Icon", typeof(global::Windows.UI.Xaml.Controls.IconElement), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.IconElement)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public NavigationViewItemPresenter() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter", "NavigationViewItemPresenter.NavigationViewItemPresenter()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter.NavigationViewItemPresenter()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter.Icon.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter.Icon.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter.IconProperty.get
	}
}
