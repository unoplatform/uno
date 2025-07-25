// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationView.properties.cpp, commit 65718e2813

using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Math = System.Math;

namespace Microsoft.UI.Xaml.Controls;

public partial class NavigationView
{
	/// <summary>
	/// Gets or sets a value that indicates whether the header is always visible.
	/// </summary>
	public bool AlwaysShowHeader
	{
		get => (bool)GetValue(AlwaysShowHeaderProperty);
		set => SetValue(AlwaysShowHeaderProperty, value);
	}

	/// <summary>
	/// Identifies the AlwaysShowHeader dependency property.
	/// </summary>
	public static DependencyProperty AlwaysShowHeaderProperty { get; } =
		DependencyProperty.Register(nameof(AlwaysShowHeader), typeof(bool), typeof(NavigationView), new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>
	/// Gets or sets an AutoSuggestBox to be displayed in the NavigationView.
	/// </summary>
	public AutoSuggestBox AutoSuggestBox
	{
		get => (AutoSuggestBox)GetValue(AutoSuggestBoxProperty);
		set => SetValue(AutoSuggestBoxProperty, value);
	}

	/// <summary>
	/// Identifies the AutoSuggestBox dependency property.
	/// </summary>
	public static DependencyProperty AutoSuggestBoxProperty { get; } =
		DependencyProperty.Register(nameof(AutoSuggestBox), typeof(AutoSuggestBox), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the minimum window width at which the NavigationView enters Compact display mode.
	/// </summary>
	public double CompactModeThresholdWidth
	{
		get => (double)GetValue(CompactModeThresholdWidthProperty);
		set
		{
			var coercedValue = value;
			CoerceToGreaterThanZero(ref coercedValue);
			SetValue(CompactModeThresholdWidthProperty, coercedValue);
		}
	}

	/// <summary>
	/// Identifies the CompactModeThresholdWidth dependency property.
	/// </summary>
	public static DependencyProperty CompactModeThresholdWidthProperty { get; } =
		DependencyProperty.Register(nameof(CompactModeThresholdWidth), typeof(double), typeof(NavigationView), new FrameworkPropertyMetadata(641.0, OnCompactModeThresholdWidthPropertyChanged));

	/// <summary>
	/// Gets or sets the width of the NavigationView pane in its compact display mode.
	/// </summary>
	public double CompactPaneLength
	{
		get => (double)GetValue(CompactPaneLengthProperty);
		set
		{
			var coercedValue = value;
			CoerceToGreaterThanZero(ref coercedValue);
			SetValue(CompactPaneLengthProperty, coercedValue);
		}
	}

	/// <summary>
	/// Identifies the CompactPaneLength dependency property.
	/// </summary>
	public static DependencyProperty CompactPaneLengthProperty { get; } =
		DependencyProperty.Register(nameof(CompactPaneLength), typeof(double), typeof(NavigationView), new FrameworkPropertyMetadata(48.0, OnCompactPaneLengthPropertyChanged));

	/// <summary>
	/// Gets or sets a UI element that is shown at the top of the control, below the pane if PaneDisplayMode is Top.
	/// </summary>
	public UIElement ContentOverlay
	{
		get => (UIElement)GetValue(ContentOverlayProperty);
		set => SetValue(ContentOverlayProperty, value);
	}

	/// <summary>
	/// Identifies the ContentOverlay dependency property.
	/// </summary>
	public static DependencyProperty ContentOverlayProperty { get; } =
		DependencyProperty.Register(nameof(ContentOverlay), typeof(UIElement), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets a value that specifies how the pane and content areas of a NavigationView are being shown.
	/// </summary>
	public NavigationViewDisplayMode DisplayMode
	{
		get => (NavigationViewDisplayMode)GetValue(DisplayModeProperty);
		private set => SetValue(DisplayModeProperty, value);
	}

	/// <summary>
	/// Identifies the DisplayMode dependency property.
	/// </summary>
	public static DependencyProperty DisplayModeProperty { get; } =
		DependencyProperty.Register(nameof(DisplayMode), typeof(NavigationViewDisplayMode), typeof(NavigationView), new FrameworkPropertyMetadata(NavigationViewDisplayMode.Minimal, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the minimum window width at which the NavigationView enters Expanded display mode.
	/// </summary>
	public double ExpandedModeThresholdWidth
	{
		get => (double)GetValue(ExpandedModeThresholdWidthProperty);
		set => SetValue(ExpandedModeThresholdWidthProperty, value);
	}

	/// <summary>
	/// Identifies the ExpandedModeThresholdWidth dependency property.
	/// </summary>
	public static DependencyProperty ExpandedModeThresholdWidthProperty { get; } =
		DependencyProperty.Register(nameof(ExpandedModeThresholdWidth), typeof(double), typeof(NavigationView), new FrameworkPropertyMetadata(1008.0, OnExpandedModeThresholdWidthPropertyChanged));

	/// <summary>
	/// Gets the footer menu items.
	/// </summary>
	public IList<object> FooterMenuItems
	{
		get => (IList<object>)GetValue(FooterMenuItemsProperty);
		private set => SetValue(FooterMenuItemsProperty, value);
	}

	/// <summary>
	/// Identifies the FooterMenuItems dependency property.
	/// </summary>
	public static DependencyProperty FooterMenuItemsProperty { get; } =
		DependencyProperty.Register(nameof(FooterMenuItems), typeof(IList<object>), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the footer menu items data source.
	/// </summary>
	public object FooterMenuItemsSource
	{
		get => (object)GetValue(FooterMenuItemsSourceProperty);
		set => SetValue(FooterMenuItemsSourceProperty, value);
	}

	/// <summary>
	/// Identifies the FooterMenuItemsSource dependency property.
	/// </summary>
	public static DependencyProperty FooterMenuItemsSourceProperty { get; } =
		DependencyProperty.Register(nameof(FooterMenuItemsSource), typeof(object), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the header content.
	/// </summary>
	public object Header
	{
		get => (object)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Identifies the Header dependency property.
	/// </summary>
	public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(nameof(Header), typeof(object), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the DataTemplate used to display the control's header.
	/// </summary>
	public DataTemplate HeaderTemplate
	{
		get => (DataTemplate)GetValue(HeaderTemplateProperty);
		set => SetValue(HeaderTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderTemplate dependency property.
	/// </summary>
	public static DependencyProperty HeaderTemplateProperty { get; } =
		DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(NavigationView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the back button is enabled or disabled.
	/// </summary>
	public NavigationViewBackButtonVisible IsBackButtonVisible
	{
		get => (NavigationViewBackButtonVisible)GetValue(IsBackButtonVisibleProperty);
		set => SetValue(IsBackButtonVisibleProperty, value);
	}

	/// <summary>
	/// Identifies the IsBackButtonVisible dependency property.
	/// </summary>
	public static DependencyProperty IsBackButtonVisibleProperty { get; } =
		DependencyProperty.Register(nameof(IsBackButtonVisible), typeof(NavigationViewBackButtonVisible), typeof(NavigationView), new FrameworkPropertyMetadata(NavigationViewBackButtonVisible.Auto, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the back button is enabled or disabled.
	/// </summary>
	public bool IsBackEnabled
	{
		get => (bool)GetValue(IsBackEnabledProperty);
		set => SetValue(IsBackEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsBackEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsBackEnabledProperty { get; } =
		DependencyProperty.Register(nameof(IsBackEnabled), typeof(bool), typeof(NavigationView), new FrameworkPropertyMetadata(false, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that specifies whether the NavigationView pane is expanded to its full width.
	/// </summary>
	public bool IsPaneOpen
	{
		get => (bool)GetValue(IsPaneOpenProperty);
		set => SetValue(IsPaneOpenProperty, value);
	}

	/// <summary>
	/// Identifies the IsPaneOpen dependency property.
	/// </summary>
	public static DependencyProperty IsPaneOpenProperty { get; } =
		DependencyProperty.Register(nameof(IsPaneOpen), typeof(bool), typeof(NavigationView), new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the menu toggle button is shown.
	/// </summary>
	public bool IsPaneToggleButtonVisible
	{
		get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
		set => SetValue(IsPaneToggleButtonVisibleProperty, value);
	}

	/// <summary>
	/// Identifies the IsPaneToggleButtonVisible dependency property.
	/// </summary>
	public static DependencyProperty IsPaneToggleButtonVisibleProperty { get; } =
		DependencyProperty.Register(nameof(IsPaneToggleButtonVisible), typeof(bool), typeof(NavigationView), new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that determines whether the pane is shown.
	/// </summary>
	public bool IsPaneVisible
	{
		get => (bool)GetValue(IsPaneVisibleProperty);
		set => SetValue(IsPaneVisibleProperty, value);
	}

	/// <summary>
	/// Identifies the IsPaneVisible dependency property.
	/// </summary>
	public static DependencyProperty IsPaneVisibleProperty { get; } =
		DependencyProperty.Register(nameof(IsPaneVisible), typeof(bool), typeof(NavigationView), new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the settings button is shown.
	/// </summary>
	public bool IsSettingsVisible
	{
		get => (bool)GetValue(IsSettingsVisibleProperty);
		set => SetValue(IsSettingsVisibleProperty, value);
	}

	/// <summary>
	/// Identifies the IsSettingsVisible dependency property.
	/// </summary>
	public static DependencyProperty IsSettingsVisibleProperty { get; } =
		DependencyProperty.Register(nameof(IsSettingsVisible), typeof(bool), typeof(NavigationView), new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether top padding is added to the navigation view's header when used with a custom title bar.
	/// </summary>
	public bool IsTitleBarAutoPaddingEnabled
	{
		get => (bool)GetValue(IsTitleBarAutoPaddingEnabledProperty);
		set => SetValue(IsTitleBarAutoPaddingEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsTitleBarAutoPaddingEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsTitleBarAutoPaddingEnabledProperty { get; } =
		DependencyProperty.Register(nameof(IsTitleBarAutoPaddingEnabled), typeof(bool), typeof(NavigationView), new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the style that is used when rendering the menu item containers.
	/// </summary>
	public Style MenuItemContainerStyle
	{
		get => (Style)GetValue(MenuItemContainerStyleProperty);
		set => SetValue(MenuItemContainerStyleProperty, value);
	}

	/// <summary>
	/// Identifies the MenuItemContainerStyle dependency property.
	/// </summary>
	public static DependencyProperty MenuItemContainerStyleProperty { get; } =
		DependencyProperty.Register(nameof(MenuItemContainerStyle), typeof(Style), typeof(NavigationView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a reference to a custom StyleSelector logic class. The StyleSelector returns different Style values
	/// to use for the item container based on characteristics of the object being displayed.
	/// </summary>
	public StyleSelector MenuItemContainerStyleSelector
	{
		get => (StyleSelector)GetValue(MenuItemContainerStyleSelectorProperty);
		set => SetValue(MenuItemContainerStyleSelectorProperty, value);
	}

	/// <summary>
	/// Identifies the MenuItemContainerStyleSelector dependency property.
	/// </summary>
	public static DependencyProperty MenuItemContainerStyleSelectorProperty { get; } =
		DependencyProperty.Register(nameof(MenuItemContainerStyleSelector), typeof(StyleSelector), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets the collection of menu items displayed in the NavigationView.
	/// </summary>
	public IList<object> MenuItems
	{
		get => (IList<object>)GetValue(MenuItemsProperty);
		private set => SetValue(MenuItemsProperty, value);
	}

	/// <summary>
	/// Identifies the MenuItems dependency property.
	/// </summary>
	public static DependencyProperty MenuItemsProperty { get; } =
		DependencyProperty.Register(nameof(MenuItems), typeof(IList<object>), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets an object source used to generate the content of the NavigationView menu.
	/// </summary>
	public object MenuItemsSource
	{
		get => (object)GetValue(MenuItemsSourceProperty);
		set => SetValue(MenuItemsSourceProperty, value);
	}

	/// <summary>
	/// Identifies the MenuItemsSource dependency property.
	/// </summary>
	public static DependencyProperty MenuItemsSourceProperty { get; } =
		DependencyProperty.Register(nameof(MenuItemsSource), typeof(object), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the DataTemplate used to display each menu item.
	/// </summary>
	public DataTemplate MenuItemTemplate
	{
		get => (DataTemplate)GetValue(MenuItemTemplateProperty);
		set => SetValue(MenuItemTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the MenuItemTemplate dependency property.
	/// </summary>
	public static DependencyProperty MenuItemTemplateProperty { get; } =
		DependencyProperty.Register(nameof(MenuItemTemplate), typeof(DataTemplate), typeof(NavigationView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a reference to a custom DataTemplateSelector logic class. The DataTemplateSelector referenced by this property returns
	/// a template to apply to items.
	/// </summary>
	public DataTemplateSelector MenuItemTemplateSelector
	{
		get => (DataTemplateSelector)GetValue(MenuItemTemplateSelectorProperty);
		set => SetValue(MenuItemTemplateSelectorProperty, value);
	}

	/// <summary>
	/// Identifies the MenuItemTemplateSelector dependency property.
	/// </summary>
	public static DependencyProperty MenuItemTemplateSelectorProperty { get; } =
		DependencyProperty.Register(nameof(MenuItemTemplateSelector), typeof(DataTemplateSelector), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the width of the NavigationView pane when it's fully expanded.
	/// </summary>
	public double OpenPaneLength
	{
		get => (double)GetValue(OpenPaneLengthProperty);
		set
		{
			var coercedValue = value;
			CoerceToGreaterThanZero(ref coercedValue);
			SetValue(OpenPaneLengthProperty, coercedValue);
		}
	}

	/// <summary>
	/// Identifies the OpenPaneLength dependency property.
	/// </summary>
	public static DependencyProperty OpenPaneLengthProperty { get; } =
		DependencyProperty.Register(nameof(OpenPaneLength), typeof(double), typeof(NavigationView), new FrameworkPropertyMetadata(320.0, OnOpenPaneLengthPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates what text label is shown for the overflow menu.
	/// </summary>
	public NavigationViewOverflowLabelMode OverflowLabelMode
	{
		get => (NavigationViewOverflowLabelMode)GetValue(OverflowLabelModeProperty);
		set => SetValue(OverflowLabelModeProperty, value);
	}

	/// <summary>
	/// Identifies the OverflowLabelMode dependency property.
	/// </summary>
	public static DependencyProperty OverflowLabelModeProperty { get; } =
		DependencyProperty.Register(nameof(OverflowLabelMode), typeof(NavigationViewOverflowLabelMode), typeof(NavigationView), new FrameworkPropertyMetadata(NavigationViewOverflowLabelMode.MoreLabel, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a UI element that is shown in the NavigationView pane.
	/// </summary>
	public UIElement PaneCustomContent
	{
		get => (UIElement)GetValue(PaneCustomContentProperty);
		set => SetValue(PaneCustomContentProperty, value);
	}

	/// <summary>
	/// Identifies the PaneCustomContent dependency property.
	/// </summary>
	public static DependencyProperty PaneCustomContentProperty { get; } =
		DependencyProperty.Register(nameof(PaneCustomContent), typeof(UIElement), typeof(NavigationView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a value that indicates how and where the NavigationView pane is shown.
	/// </summary>
	public NavigationViewPaneDisplayMode PaneDisplayMode
	{
		get => (NavigationViewPaneDisplayMode)GetValue(PaneDisplayModeProperty);
		set => SetValue(PaneDisplayModeProperty, value);
	}

	/// <summary>
	/// Identifies the PaneDisplayMode dependency property.
	/// </summary>
	public static DependencyProperty PaneDisplayModeProperty { get; } =
		DependencyProperty.Register(nameof(PaneDisplayMode), typeof(NavigationViewPaneDisplayMode), typeof(NavigationView), new FrameworkPropertyMetadata(NavigationViewPaneDisplayMode.Auto, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the content for the pane footer.
	/// </summary>
	public UIElement PaneFooter
	{
		get => (UIElement)GetValue(PaneFooterProperty);
		set => SetValue(PaneFooterProperty, value);
	}

	/// <summary>
	/// Identifies the PaneFooter dependency property.
	/// </summary>
	public static DependencyProperty PaneFooterProperty { get; } =
		DependencyProperty.Register(nameof(PaneFooter), typeof(UIElement), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the content for the pane header.
	/// </summary>
	public UIElement PaneHeader
	{
		get => (UIElement)GetValue(PaneHeaderProperty);
		set => SetValue(PaneHeaderProperty, value);
	}

	/// <summary>
	/// Identifies the PaneHeader dependency property.
	/// </summary>
	public static DependencyProperty PaneHeaderProperty { get; } =
		DependencyProperty.Register(nameof(PaneHeader), typeof(UIElement), typeof(NavigationView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the label adjacent to the menu icon when the NavigationView pane is open.
	/// </summary>
	public string PaneTitle
	{
		get => (string)GetValue(PaneTitleProperty);
		set => SetValue(PaneTitleProperty, value);
	}

	/// <summary>
	/// Identifies the PaneTitle dependency property.
	/// </summary>
	public static DependencyProperty PaneTitleProperty { get; } =
		DependencyProperty.Register(nameof(PaneTitle), typeof(string), typeof(NavigationView), new FrameworkPropertyMetadata(string.Empty, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the Style that defines the look of the menu toggle button.
	/// </summary>
	public Style PaneToggleButtonStyle
	{
		get => (Style)GetValue(PaneToggleButtonStyleProperty);
		set => SetValue(PaneToggleButtonStyleProperty, value);
	}

	/// <summary>
	/// Identifies the PaneToggleButtonStyle dependency property.
	/// </summary>
	public static DependencyProperty PaneToggleButtonStyleProperty { get; } =
		DependencyProperty.Register(nameof(PaneToggleButtonStyle), typeof(Style), typeof(NavigationView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the selected item.
	/// </summary>
	public object SelectedItem
	{
		get => (object)GetValue(SelectedItemProperty);
		set => SetValue(SelectedItemProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedItem dependency property.
	/// </summary>
	public static DependencyProperty SelectedItemProperty { get; } =
		DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether item selection changes when keyboard focus changes.
	/// </summary>
	public NavigationViewSelectionFollowsFocus SelectionFollowsFocus
	{
		get => (NavigationViewSelectionFollowsFocus)GetValue(SelectionFollowsFocusProperty);
		set => SetValue(SelectionFollowsFocusProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionFollowsFocus dependency property.
	/// </summary>
	public static DependencyProperty SelectionFollowsFocusProperty { get; } =
		DependencyProperty.Register(nameof(SelectionFollowsFocus), typeof(NavigationViewSelectionFollowsFocus), typeof(NavigationView), new FrameworkPropertyMetadata(NavigationViewSelectionFollowsFocus.Disabled, OnPropertyChanged));

	/// <summary>
	/// Gets the navigation item that represents the entry point to app settings.
	/// </summary>
	public object SettingsItem
	{
		get => (object)GetValue(SettingsItemProperty);
		set => SetValue(SettingsItemProperty, value);
	}

	/// <summary>
	/// Identifies the SettingsItem dependency property.
	/// </summary>
	public static DependencyProperty SettingsItemProperty { get; } =
		DependencyProperty.Register(nameof(SettingsItem), typeof(object), typeof(NavigationView), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates when gamepad bumpers can be used to navigate the top-level navigation items in a NavigationView.
	/// </summary>
	public NavigationViewShoulderNavigationEnabled ShoulderNavigationEnabled
	{
		get => (NavigationViewShoulderNavigationEnabled)GetValue(ShoulderNavigationEnabledProperty);
		set => SetValue(ShoulderNavigationEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the ShoulderNavigationEnabled dependency property.
	/// </summary>
	public static DependencyProperty ShoulderNavigationEnabledProperty { get; } =
		DependencyProperty.Register(nameof(ShoulderNavigationEnabled), typeof(NavigationViewShoulderNavigationEnabled), typeof(NavigationView), new FrameworkPropertyMetadata(NavigationViewShoulderNavigationEnabled.Never, OnPropertyChanged));

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as TemplateBinding sources when defining templates for a NavigationView control.
	/// </summary>
	public NavigationViewTemplateSettings TemplateSettings
	{
		get => (NavigationViewTemplateSettings)GetValue(TemplateSettingsProperty);
		set => SetValue(TemplateSettingsProperty, value);
	}

	/// <summary>
	/// Identifies the TemplateSettings dependency property.
	/// </summary>
	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TemplateSettings), typeof(NavigationViewTemplateSettings), typeof(NavigationView), new FrameworkPropertyMetadata(null));

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (NavigationView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnCompactModeThresholdWidthPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (NavigationView)sender;

		var value = (double)args.NewValue;
		var coercedValue = value;
		owner.CoerceToGreaterThanZero(ref coercedValue);
		if (Math.Abs(coercedValue - value) > 0.1)
		{
			sender.SetValue(args.Property, coercedValue);
			return;
		}

		owner.OnPropertyChanged(args);
	}

	private static void OnCompactPaneLengthPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (NavigationView)sender;

		var value = (double)args.NewValue;
		var coercedValue = value;
		owner.CoerceToGreaterThanZero(ref coercedValue);
		if (Math.Abs(coercedValue - value) > 0.1)
		{
			sender.SetValue(args.Property, coercedValue);
			return;
		}

		owner.OnPropertyChanged(args);
	}

	private static void OnExpandedModeThresholdWidthPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (NavigationView)sender;

		var value = (double)args.NewValue;
		var coercedValue = value;
		owner.CoerceToGreaterThanZero(ref coercedValue);
		if (Math.Abs(coercedValue - value) > 0.1)
		{
			sender.SetValue(args.Property, coercedValue);
			return;
		}

		owner.OnPropertyChanged(args);
	}

	private static void OnOpenPaneLengthPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (NavigationView)sender;

		var value = (double)args.NewValue;
		var coercedValue = value;
		owner.CoerceToGreaterThanZero(ref coercedValue);
		if (Math.Abs(coercedValue - value) > 0.1)
		{
			sender.SetValue(args.Property, coercedValue);
			return;
		}

		owner.OnPropertyChanged(args);
	}

	// Events

	/// <summary>
	/// Occurs when the back button receives an interaction such as a click or tap.
	/// </summary>
	public event TypedEventHandler<NavigationView, NavigationViewBackRequestedEventArgs> BackRequested;

	/// <summary>
	/// Occurs when a node in the tree is collapsed.
	/// </summary>
	public event TypedEventHandler<NavigationView, NavigationViewItemCollapsedEventArgs> Collapsed;

	/// <summary>
	/// Occurs when the DisplayMode property changes.
	/// </summary>
	public event TypedEventHandler<NavigationView, NavigationViewDisplayModeChangedEventArgs> DisplayModeChanged;

	/// <summary>
	/// Occurs when a node in the tree starts to expand.
	/// </summary>
	public event TypedEventHandler<NavigationView, NavigationViewItemExpandingEventArgs> Expanding;

	/// <summary>
	/// Occurs when an item in the menu receives an interaction such a a click or tap.
	/// </summary>
	public event TypedEventHandler<NavigationView, NavigationViewItemInvokedEventArgs> ItemInvoked;

	/// <summary>
	/// Occurs when the NavigationView pane is closed.
	/// </summary>
	public event TypedEventHandler<NavigationView, object> PaneClosed;

	/// <summary>
	/// Occurs when the NavigationView pane is closing.
	/// </summary>
	public event TypedEventHandler<NavigationView, NavigationViewPaneClosingEventArgs> PaneClosing;

	/// <summary>
	/// Occurs when the NavigationView pane is opened.
	/// </summary>
	public event TypedEventHandler<NavigationView, object> PaneOpened;

	/// <summary>
	/// Occurs when the NavigationView pane is opening.
	/// </summary>
	public event TypedEventHandler<NavigationView, object> PaneOpening;

	/// <summary>
	/// Occurs when the currently selected item changes.
	/// </summary>
	public event TypedEventHandler<NavigationView, NavigationViewSelectionChangedEventArgs> SelectionChanged;
}
