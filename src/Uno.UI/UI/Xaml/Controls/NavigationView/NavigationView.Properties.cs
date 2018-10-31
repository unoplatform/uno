using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationView : ContentControl
	{
		[Uno.NotImplemented]
		public object SelectedItem
		{
			get => (object)GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		[Uno.NotImplemented]
		public Style PaneToggleButtonStyle
		{
			get => (Style)GetValue(PaneToggleButtonStyleProperty);
			set => SetValue(PaneToggleButtonStyleProperty, value);
		}

		[Uno.NotImplemented]
		public UIElement PaneFooter
		{
			get => (UIElement)GetValue(PaneFooterProperty);
			set => SetValue(PaneFooterProperty, value);
		}

		[Uno.NotImplemented]
		public double OpenPaneLength
		{
			get => (double)GetValue(OpenPaneLengthProperty);
			set => SetValue(OpenPaneLengthProperty, value);
		}

		public object MenuItemsSource
		{
			get => (object)GetValue(MenuItemsSourceProperty);
			set => SetValue(MenuItemsSourceProperty, value);
		}

		[Uno.NotImplemented]
		public DataTemplateSelector MenuItemTemplateSelector
		{
			get => (DataTemplateSelector)GetValue(MenuItemTemplateSelectorProperty);
			set => SetValue(MenuItemTemplateSelectorProperty, value);
		}

		[Uno.NotImplemented]
		public DataTemplate MenuItemTemplate
		{
			get => (DataTemplate)GetValue(MenuItemTemplateProperty);
			set => SetValue(MenuItemTemplateProperty, value);
		}

		[Uno.NotImplemented]
		public StyleSelector MenuItemContainerStyleSelector
		{
			get => (StyleSelector)GetValue(MenuItemContainerStyleSelectorProperty);
			set => SetValue(MenuItemContainerStyleSelectorProperty, value);
		}

		[Uno.NotImplemented]
		public Style MenuItemContainerStyle
		{
			get => (Style)GetValue(MenuItemContainerStyleProperty);
			set => SetValue(MenuItemContainerStyleProperty, value);
		}

		[Uno.NotImplemented]
		public bool IsSettingsVisible
		{
			get => (bool)GetValue(IsSettingsVisibleProperty);
			set => SetValue(IsSettingsVisibleProperty, value);
		}

		[Uno.NotImplemented]
		public bool IsPaneToggleButtonVisible
		{
			get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
			set => SetValue(IsPaneToggleButtonVisibleProperty, value);
		}

		[Uno.NotImplemented]
		public bool IsPaneOpen
		{
			get => (bool)GetValue(IsPaneOpenProperty);
			set => SetValue(IsPaneOpenProperty, value);
		}

		[Uno.NotImplemented]
		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		[Uno.NotImplemented]
		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		[Uno.NotImplemented]
		public double ExpandedModeThresholdWidth
		{
			get => (double)GetValue(ExpandedModeThresholdWidthProperty);
			set => SetValue(ExpandedModeThresholdWidthProperty, value);
		}

		[Uno.NotImplemented]
		public double CompactPaneLength
		{
			get => (double)GetValue(CompactPaneLengthProperty);
			set => SetValue(CompactPaneLengthProperty, value);
		}

		[Uno.NotImplemented]
		public double CompactModeThresholdWidth
		{
			get => (double)GetValue(CompactModeThresholdWidthProperty);
			set => SetValue(CompactModeThresholdWidthProperty, value);
		}

		[Uno.NotImplemented]
		public AutoSuggestBox AutoSuggestBox
		{
			get => (AutoSuggestBox)GetValue(AutoSuggestBoxProperty);
			set => SetValue(AutoSuggestBoxProperty, value);
		}

		[Uno.NotImplemented]
		public bool AlwaysShowHeader
		{
			get => (bool)GetValue(AlwaysShowHeaderProperty);
			set => SetValue(AlwaysShowHeaderProperty, value);
		}

		[Uno.NotImplemented]
		public NavigationViewDisplayMode DisplayMode => (NavigationViewDisplayMode)GetValue(DisplayModeProperty);

		public IList<object> MenuItems => (IList<object>)GetValue(MenuItemsProperty);

		[Uno.NotImplemented]
		public object SettingsItem => (object)GetValue(SettingsItemProperty);

		[Uno.NotImplemented]
		public string PaneTitle
		{
			get => (string)GetValue(PaneTitleProperty);
			set => SetValue(PaneTitleProperty, value);
		}

		[Uno.NotImplemented]
		public bool IsBackEnabled
		{
			get => (bool)GetValue(IsBackEnabledProperty);
			set => SetValue(IsBackEnabledProperty, value);
		}

		[Uno.NotImplemented]
		public NavigationViewBackButtonVisible IsBackButtonVisible
		{
			get => (NavigationViewBackButtonVisible)GetValue(IsBackButtonVisibleProperty);
			set => SetValue(IsBackButtonVisibleProperty, value);
		}

		[Uno.NotImplemented]
		public static DependencyProperty AlwaysShowHeaderProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"AlwaysShowHeader", typeof(bool),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(bool)));

		[Uno.NotImplemented]
		public static DependencyProperty AutoSuggestBoxProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"AutoSuggestBox", typeof(AutoSuggestBox),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(AutoSuggestBox)));

		[Uno.NotImplemented]
		public static DependencyProperty CompactModeThresholdWidthProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"CompactModeThresholdWidth", typeof(double),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(double)));

		[Uno.NotImplemented]
		public static DependencyProperty CompactPaneLengthProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(CompactPaneLength),
			propertyType: typeof(double),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: 48.0)
		);

		[Uno.NotImplemented]
		public static DependencyProperty DisplayModeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"DisplayMode", typeof(NavigationViewDisplayMode),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(NavigationViewDisplayMode)));

		[Uno.NotImplemented]
		public static DependencyProperty ExpandedModeThresholdWidthProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"ExpandedModeThresholdWidth", typeof(double),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(double)));

		[Uno.NotImplemented]
		public static DependencyProperty HeaderProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(object)));

		[Uno.NotImplemented]
		public static DependencyProperty HeaderTemplateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(DataTemplate),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(DataTemplate)));

		[Uno.NotImplemented]
		public static DependencyProperty IsPaneOpenProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPaneOpen", typeof(bool),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(true));

		[Uno.NotImplemented]
		public static DependencyProperty IsPaneToggleButtonVisibleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPaneToggleButtonVisible", typeof(bool),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(bool)));

		[Uno.NotImplemented]
		public static DependencyProperty IsSettingsVisibleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: "IsSettingsVisible",
			propertyType: typeof(bool),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(
				defaultValue: true, propertyChangedCallback: (s, e) => (s as NavigationView)?.OnIsSettingsVisibleChanged()
			)
		);

		[Uno.NotImplemented]
		public static DependencyProperty MenuItemContainerStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemContainerStyle", typeof(Style),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(Style)));

		[Uno.NotImplemented]
		public static DependencyProperty MenuItemContainerStyleSelectorProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemContainerStyleSelector", typeof(StyleSelector),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(StyleSelector)));

		[Uno.NotImplemented]
		public static DependencyProperty MenuItemTemplateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemTemplate", typeof(DataTemplate),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(DataTemplate)));

		[Uno.NotImplemented]
		public static DependencyProperty MenuItemTemplateSelectorProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemTemplateSelector", typeof(DataTemplateSelector),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(DataTemplateSelector)));

		public static DependencyProperty MenuItemsProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(MenuItems),
			propertyType: typeof(IList<object>),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(null)
		);

		[Uno.NotImplemented]
		public static DependencyProperty MenuItemsSourceProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemsSource", typeof(object),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(object)));

		[Uno.NotImplemented]
		public static DependencyProperty OpenPaneLengthProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(OpenPaneLength),
			propertyType: typeof(double),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: 320.0)
		);

		[Uno.NotImplemented]
		public static DependencyProperty PaneFooterProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneFooter", typeof(UIElement),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(UIElement)));

		[Uno.NotImplemented]
		public static DependencyProperty PaneToggleButtonStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneToggleButtonStyle", typeof(Style),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(Style)));

		[Uno.NotImplemented]
		public static DependencyProperty SelectedItemProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedItem", typeof(object),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(object)));

		[Uno.NotImplemented]
		public static DependencyProperty SettingsItemProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"SettingsItem", typeof(object),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(object)));

		[Uno.NotImplemented]
		public static DependencyProperty IsBackButtonVisibleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsBackButtonVisible", typeof(NavigationViewBackButtonVisible),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(NavigationViewBackButtonVisible)));

		[Uno.NotImplemented]
		public static DependencyProperty IsBackEnabledProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsBackEnabled", typeof(bool),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(bool)));

		[Uno.NotImplemented]
		public static DependencyProperty PaneTitleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneTitle", typeof(string),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(string)));
	}
}
