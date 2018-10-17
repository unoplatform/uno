#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationView : global::Windows.UI.Xaml.Controls.ContentControl
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object SelectedItem
		{
			get
			{
				return (object)this.GetValue(SelectedItemProperty);
			}
			set
			{
				this.SetValue(SelectedItemProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style PaneToggleButtonStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(PaneToggleButtonStyleProperty);
			}
			set
			{
				this.SetValue(PaneToggleButtonStyleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement PaneFooter
		{
			get
			{
				return (global::Windows.UI.Xaml.UIElement)this.GetValue(PaneFooterProperty);
			}
			set
			{
				this.SetValue(PaneFooterProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double OpenPaneLength
		{
			get
			{
				return (double)this.GetValue(OpenPaneLengthProperty);
			}
			set
			{
				this.SetValue(OpenPaneLengthProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object MenuItemsSource
		{
			get
			{
				return (object)this.GetValue(MenuItemsSourceProperty);
			}
			set
			{
				this.SetValue(MenuItemsSourceProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.DataTemplateSelector MenuItemTemplateSelector
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.DataTemplateSelector)this.GetValue(MenuItemTemplateSelectorProperty);
			}
			set
			{
				this.SetValue(MenuItemTemplateSelectorProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate MenuItemTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(MenuItemTemplateProperty);
			}
			set
			{
				this.SetValue(MenuItemTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.StyleSelector MenuItemContainerStyleSelector
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.StyleSelector)this.GetValue(MenuItemContainerStyleSelectorProperty);
			}
			set
			{
				this.SetValue(MenuItemContainerStyleSelectorProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style MenuItemContainerStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(MenuItemContainerStyleProperty);
			}
			set
			{
				this.SetValue(MenuItemContainerStyleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsSettingsVisible
		{
			get
			{
				return (bool)this.GetValue(IsSettingsVisibleProperty);
			}
			set
			{
				this.SetValue(IsSettingsVisibleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsPaneToggleButtonVisible
		{
			get
			{
				return (bool)this.GetValue(IsPaneToggleButtonVisibleProperty);
			}
			set
			{
				this.SetValue(IsPaneToggleButtonVisibleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsPaneOpen
		{
			get
			{
				return (bool)this.GetValue(IsPaneOpenProperty);
			}
			set
			{
				this.SetValue(IsPaneOpenProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate HeaderTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(HeaderTemplateProperty);
			}
			set
			{
				this.SetValue(HeaderTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object Header
		{
			get
			{
				return (object)this.GetValue(HeaderProperty);
			}
			set
			{
				this.SetValue(HeaderProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double ExpandedModeThresholdWidth
		{
			get
			{
				return (double)this.GetValue(ExpandedModeThresholdWidthProperty);
			}
			set
			{
				this.SetValue(ExpandedModeThresholdWidthProperty, value);
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
			set
			{
				this.SetValue(CompactPaneLengthProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double CompactModeThresholdWidth
		{
			get
			{
				return (double)this.GetValue(CompactModeThresholdWidthProperty);
			}
			set
			{
				this.SetValue(CompactModeThresholdWidthProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.AutoSuggestBox AutoSuggestBox
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.AutoSuggestBox)this.GetValue(AutoSuggestBoxProperty);
			}
			set
			{
				this.SetValue(AutoSuggestBoxProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool AlwaysShowHeader
		{
			get
			{
				return (bool)this.GetValue(AlwaysShowHeaderProperty);
			}
			set
			{
				this.SetValue(AlwaysShowHeaderProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.NavigationViewDisplayMode DisplayMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.NavigationViewDisplayMode)this.GetValue(DisplayModeProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<object> MenuItems
		{
			get
			{
				return (global::System.Collections.Generic.IList<object>)this.GetValue(MenuItemsProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object SettingsItem
		{
			get
			{
				return (object)this.GetValue(SettingsItemProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string PaneTitle
		{
			get
			{
				return (string)this.GetValue(PaneTitleProperty);
			}
			set
			{
				this.SetValue(PaneTitleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsBackEnabled
		{
			get
			{
				return (bool)this.GetValue(IsBackEnabledProperty);
			}
			set
			{
				this.SetValue(IsBackEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.NavigationViewBackButtonVisible IsBackButtonVisible
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.NavigationViewBackButtonVisible)this.GetValue(IsBackButtonVisibleProperty);
			}
			set
			{
				this.SetValue(IsBackButtonVisibleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlwaysShowHeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AlwaysShowHeader", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AutoSuggestBoxProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AutoSuggestBox", typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.AutoSuggestBox)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CompactModeThresholdWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CompactModeThresholdWidth", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CompactPaneLengthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CompactPaneLength", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DisplayModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DisplayMode", typeof(global::Windows.UI.Xaml.Controls.NavigationViewDisplayMode), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.NavigationViewDisplayMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ExpandedModeThresholdWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ExpandedModeThresholdWidth", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsPaneOpenProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPaneOpen", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsPaneToggleButtonVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPaneToggleButtonVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsSettingsVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsSettingsVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MenuItemContainerStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemContainerStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MenuItemContainerStyleSelectorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemContainerStyleSelector", typeof(global::Windows.UI.Xaml.Controls.StyleSelector), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.StyleSelector)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MenuItemTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MenuItemTemplateSelectorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemTemplateSelector", typeof(global::Windows.UI.Xaml.Controls.DataTemplateSelector), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.DataTemplateSelector)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MenuItemsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItems", typeof(global::System.Collections.Generic.IList<object>), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<object>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MenuItemsSourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemsSource", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OpenPaneLengthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"OpenPaneLength", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PaneFooterProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneFooter", typeof(global::Windows.UI.Xaml.UIElement), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.UIElement)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PaneToggleButtonStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneToggleButtonStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectedItemProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedItem", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SettingsItemProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SettingsItem", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsBackButtonVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsBackButtonVisible", typeof(global::Windows.UI.Xaml.Controls.NavigationViewBackButtonVisible), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.NavigationViewBackButtonVisible)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsBackEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsBackEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PaneTitleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneTitle", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationView), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public NavigationView() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "NavigationView.NavigationView()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.NavigationView()
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsPaneOpen.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsPaneOpen.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.CompactModeThresholdWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.CompactModeThresholdWidth.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.ExpandedModeThresholdWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.ExpandedModeThresholdWidth.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneFooter.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneFooter.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.DisplayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsSettingsVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsSettingsVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsPaneToggleButtonVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsPaneToggleButtonVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.AlwaysShowHeader.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.AlwaysShowHeader.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.CompactPaneLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.CompactPaneLength.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.OpenPaneLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.OpenPaneLength.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneToggleButtonStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneToggleButtonStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.SelectedItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.SelectedItem.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItems.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemsSource.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemsSource.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.SettingsItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.AutoSuggestBox.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.AutoSuggestBox.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemTemplateSelector.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemTemplateSelector.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemContainerStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemContainerStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemContainerStyleSelector.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemContainerStyleSelector.set
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object MenuItemFromContainer( global::Windows.UI.Xaml.DependencyObject container)
		{
			throw new global::System.NotImplementedException("The member object NavigationView.MenuItemFromContainer(DependencyObject container) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DependencyObject ContainerFromMenuItem( object item)
		{
			throw new global::System.NotImplementedException("The member DependencyObject NavigationView.ContainerFromMenuItem(object item) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.SelectionChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.SelectionChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.ItemInvoked.add
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.ItemInvoked.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.DisplayModeChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.DisplayModeChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsBackButtonVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsBackButtonVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsBackEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsBackEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneTitle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneTitle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.BackRequested.add
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.BackRequested.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneClosed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneClosed.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneClosing.add
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneClosing.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneOpened.add
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneOpened.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneOpening.add
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneOpening.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsBackButtonVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsBackEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneTitleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsPaneOpenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.CompactModeThresholdWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.ExpandedModeThresholdWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneFooterProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.DisplayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsSettingsVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.IsPaneToggleButtonVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.AlwaysShowHeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.CompactPaneLengthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.OpenPaneLengthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.PaneToggleButtonStyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemsSourceProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.SelectedItemProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.SettingsItemProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.AutoSuggestBoxProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemTemplateSelectorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemContainerStyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationView.MenuItemContainerStyleSelectorProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.NavigationView, global::Windows.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs> DisplayModeChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, NavigationViewDisplayModeChangedEventArgs> NavigationView.DisplayModeChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, NavigationViewDisplayModeChangedEventArgs> NavigationView.DisplayModeChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.NavigationView, global::Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs> ItemInvoked
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, NavigationViewItemInvokedEventArgs> NavigationView.ItemInvoked");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, NavigationViewItemInvokedEventArgs> NavigationView.ItemInvoked");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.NavigationView, global::Windows.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs> SelectionChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, NavigationViewSelectionChangedEventArgs> NavigationView.SelectionChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, NavigationViewSelectionChangedEventArgs> NavigationView.SelectionChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.NavigationView, global::Windows.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs> BackRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, NavigationViewBackRequestedEventArgs> NavigationView.BackRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, NavigationViewBackRequestedEventArgs> NavigationView.BackRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.NavigationView, object> PaneClosed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, object> NavigationView.PaneClosed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, object> NavigationView.PaneClosed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.NavigationView, global::Windows.UI.Xaml.Controls.NavigationViewPaneClosingEventArgs> PaneClosing
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, NavigationViewPaneClosingEventArgs> NavigationView.PaneClosing");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, NavigationViewPaneClosingEventArgs> NavigationView.PaneClosing");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.NavigationView, object> PaneOpened
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, object> NavigationView.PaneOpened");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, object> NavigationView.PaneOpened");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.NavigationView, object> PaneOpening
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, object> NavigationView.PaneOpening");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationView", "event TypedEventHandler<NavigationView, object> NavigationView.PaneOpening");
			}
		}
		#endif
	}
}
