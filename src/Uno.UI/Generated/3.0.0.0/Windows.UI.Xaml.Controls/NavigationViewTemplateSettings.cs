#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationViewTemplateSettings : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Visibility BackButtonVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)this.GetValue(BackButtonVisibilityProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Visibility LeftPaneVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)this.GetValue(LeftPaneVisibilityProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Visibility OverflowButtonVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)this.GetValue(OverflowButtonVisibilityProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Visibility PaneToggleButtonVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)this.GetValue(PaneToggleButtonVisibilityProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool SingleSelectionFollowsFocus
		{
			get
			{
				return (bool)this.GetValue(SingleSelectionFollowsFocusProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double TopPadding
		{
			get
			{
				return (double)this.GetValue(TopPaddingProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Visibility TopPaneVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)this.GetValue(TopPaneVisibilityProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BackButtonVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BackButtonVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Visibility)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LeftPaneVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LeftPaneVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Visibility)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OverflowButtonVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"OverflowButtonVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Visibility)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PaneToggleButtonVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneToggleButtonVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Visibility)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SingleSelectionFollowsFocusProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SingleSelectionFollowsFocus", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TopPaddingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TopPadding", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TopPaneVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TopPaneVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Visibility)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.NavigationViewTemplateSettings()
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.NavigationViewTemplateSettings()
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.TopPadding.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.OverflowButtonVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.PaneToggleButtonVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.BackButtonVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.TopPaneVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.LeftPaneVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.SingleSelectionFollowsFocus.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.TopPaddingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.OverflowButtonVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.PaneToggleButtonVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.BackButtonVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.TopPaneVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.LeftPaneVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewTemplateSettings.SingleSelectionFollowsFocusProperty.get
	}
}
