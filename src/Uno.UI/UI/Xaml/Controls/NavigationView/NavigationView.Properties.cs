// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationView.cpp file from WinUI controls.
//

using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationView : ContentControl
	{
		public object SelectedItem
		{
			get => (object)GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		public Style PaneToggleButtonStyle
		{
			get => (Style)GetValue(PaneToggleButtonStyleProperty);
			set => SetValue(PaneToggleButtonStyleProperty, value);
		}

		public UIElement PaneFooter
		{
			get => (UIElement)GetValue(PaneFooterProperty);
			set => SetValue(PaneFooterProperty, value);
		}

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

		public DataTemplateSelector MenuItemTemplateSelector
		{
			get => (DataTemplateSelector)GetValue(MenuItemTemplateSelectorProperty);
			set => SetValue(MenuItemTemplateSelectorProperty, value);
		}

		public DataTemplate MenuItemTemplate
		{
			get => (DataTemplate)GetValue(MenuItemTemplateProperty);
			set => SetValue(MenuItemTemplateProperty, value);
		}

		public StyleSelector MenuItemContainerStyleSelector
		{
			get => (StyleSelector)GetValue(MenuItemContainerStyleSelectorProperty);
			set => SetValue(MenuItemContainerStyleSelectorProperty, value);
		}

		public Style MenuItemContainerStyle
		{
			get => (Style)GetValue(MenuItemContainerStyleProperty);
			set => SetValue(MenuItemContainerStyleProperty, value);
		}

		public bool IsSettingsVisible
		{
			get => (bool)GetValue(IsSettingsVisibleProperty);
			set => SetValue(IsSettingsVisibleProperty, value);
		}

		public bool IsPaneToggleButtonVisible
		{
			get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
			set => SetValue(IsPaneToggleButtonVisibleProperty, value);
		}

		public bool IsPaneOpen
		{
			get => (bool)GetValue(IsPaneOpenProperty);
			set => SetValue(IsPaneOpenProperty, value);
		}

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public double ExpandedModeThresholdWidth
		{
			get => (double)GetValue(ExpandedModeThresholdWidthProperty);
			set => SetValue(ExpandedModeThresholdWidthProperty, value);
		}

		public double CompactPaneLength
		{
			get => (double)GetValue(CompactPaneLengthProperty);
			set => SetValue(CompactPaneLengthProperty, value);
		}

		public double CompactModeThresholdWidth
		{
			get => (double)GetValue(CompactModeThresholdWidthProperty);
			set => SetValue(CompactModeThresholdWidthProperty, value);
		}

		public AutoSuggestBox AutoSuggestBox
		{
			get => (AutoSuggestBox)GetValue(AutoSuggestBoxProperty);
			set => SetValue(AutoSuggestBoxProperty, value);
		}

		public bool AlwaysShowHeader
		{
			get => (bool)GetValue(AlwaysShowHeaderProperty);
			set => SetValue(AlwaysShowHeaderProperty, value);
		}

		public NavigationViewDisplayMode DisplayMode
		{
			get => (NavigationViewDisplayMode)GetValue(DisplayModeProperty);
			internal set => SetValue(DisplayModeProperty, value);
		}

		public IList<object> MenuItems => (IList<object>)GetValue(MenuItemsProperty);

		public object SettingsItem => (object)GetValue(SettingsItemProperty);

		public string PaneTitle
		{
			get => (string)GetValue(PaneTitleProperty);
			set => SetValue(PaneTitleProperty, value);
		}

		public bool IsBackEnabled
		{
			get => (bool)GetValue(IsBackEnabledProperty);
			set => SetValue(IsBackEnabledProperty, value);
		}

		public NavigationViewBackButtonVisible IsBackButtonVisible
		{
			get => (NavigationViewBackButtonVisible)GetValue(IsBackButtonVisibleProperty);
			set => SetValue(IsBackButtonVisibleProperty, value);
		}

		public NavigationViewShoulderNavigationEnabled ShoulderNavigationEnabled
		{
			get => (NavigationViewShoulderNavigationEnabled)GetValue(ShoulderNavigationEnabledProperty);
			set => SetValue(ShoulderNavigationEnabledProperty, value);
		}

		public NavigationViewSelectionFollowsFocus SelectionFollowsFocus
		{
			get => (NavigationViewSelectionFollowsFocus)GetValue(SelectionFollowsFocusProperty);
			set => SetValue(SelectionFollowsFocusProperty, value);
		}

		public UIElement PaneHeader
		{
			get => (UIElement)GetValue(PaneHeaderProperty);
			set => SetValue(PaneHeaderProperty, value);
		}

		public NavigationViewPaneDisplayMode PaneDisplayMode
		{
			get => (NavigationViewPaneDisplayMode)GetValue(PaneDisplayModeProperty);
			set => SetValue(PaneDisplayModeProperty, value);
		}

		public UIElement PaneCustomContent
		{
			get => (UIElement)GetValue(PaneCustomContentProperty);
			set => SetValue(PaneCustomContentProperty, value);
		}

		public NavigationViewOverflowLabelMode OverflowLabelMode
		{
			get => (NavigationViewOverflowLabelMode)GetValue(OverflowLabelModeProperty);
			set => SetValue(OverflowLabelModeProperty, value);
		}

		public bool IsPaneVisible
		{
			get => (bool)GetValue(IsPaneVisibleProperty);
			set => SetValue(IsPaneVisibleProperty, value);
		}

		public UIElement ContentOverlay
		{
			get => (UIElement)GetValue(ContentOverlayProperty);
			set => SetValue(ContentOverlayProperty, value);
		}

		public NavigationViewTemplateSettings TemplateSettings
			=> (NavigationViewTemplateSettings)GetValue(TemplateSettingsProperty);

		public static DependencyProperty IsPaneVisibleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPaneVisible",
			typeof(bool),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(
				true,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty OverflowLabelModeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"OverflowLabelMode",
			typeof(NavigationViewOverflowLabelMode),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(
				NavigationViewOverflowLabelMode.MoreLabel,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty PaneCustomContentProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneCustomContent",
			typeof(UIElement),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(
				null,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty PaneDisplayModeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneDisplayMode",
			typeof(NavigationViewPaneDisplayMode),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(
				NavigationViewPaneDisplayMode.Auto,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty PaneHeaderProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneHeader",
			typeof(UIElement),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(
				null,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty SelectionFollowsFocusProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectionFollowsFocus",
			typeof(NavigationViewSelectionFollowsFocus),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(NavigationViewSelectionFollowsFocus.Disabled,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty ShoulderNavigationEnabledProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"ShoulderNavigationEnabled",
			typeof(NavigationViewShoulderNavigationEnabled),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(NavigationViewShoulderNavigationEnabled.Never,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty TemplateSettingsProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"TemplateSettings",
			typeof(NavigationViewTemplateSettings),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(null)
		);

		public static DependencyProperty ContentOverlayProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"ContentOverlay",
			typeof(UIElement),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(null)
		);

		public static DependencyProperty AlwaysShowHeaderProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"AlwaysShowHeader",
			typeof(bool),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(
				defaultValue: true,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty AutoSuggestBoxProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"AutoSuggestBox",
			typeof(AutoSuggestBox),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(
				default(AutoSuggestBox),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty CompactModeThresholdWidthProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(CompactModeThresholdWidth),
			propertyType: typeof(double),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: 641.0,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged_CoerceToGreaterThanZero(e)
			)
		);

		public static DependencyProperty CompactPaneLengthProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(CompactPaneLength),
			propertyType: typeof(double),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(
				defaultValue: 48.0,
				(s, e) => (s as NavigationView)?.OnPropertyChanged_CoerceToGreaterThanZero(e)
			)
		);

		public static DependencyProperty DisplayModeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(DisplayMode),
			propertyType: typeof(NavigationViewDisplayMode),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(
				NavigationViewDisplayMode.Minimal,
				(s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty ExpandedModeThresholdWidthProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(ExpandedModeThresholdWidth),
			propertyType: typeof(double),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(1008.0,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged_CoerceToGreaterThanZero(e))
		);

		public static DependencyProperty HeaderProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header",
			typeof(object),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(
				default(object),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			));

		public static DependencyProperty HeaderTemplateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate",
			typeof(DataTemplate),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(DataTemplate),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty IsPaneOpenProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPaneOpen",
			typeof(bool),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(true,
				(s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty IsPaneToggleButtonVisibleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPaneToggleButtonVisible",
			typeof(bool),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(true,
				(s, e) => (s as NavigationView)?.OnPropertyChanged(e)
				)
			);

		public static DependencyProperty IsSettingsVisibleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: "IsSettingsVisible",
			propertyType: typeof(bool),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(
				defaultValue: true,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty MenuItemContainerStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemContainerStyle",
			typeof(Style),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(Style),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty MenuItemContainerStyleSelectorProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemContainerStyleSelector",
			typeof(StyleSelector),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(StyleSelector),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty MenuItemTemplateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemTemplate",
			typeof(DataTemplate),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(DataTemplate),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty MenuItemTemplateSelectorProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemTemplateSelector",
			typeof(DataTemplateSelector),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(DataTemplateSelector),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty MenuItemsProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(MenuItems),
			propertyType: typeof(IList<object>),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(null,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e))
		);

		public static DependencyProperty MenuItemsSourceProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuItemsSource",
			typeof(object),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(object),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty OpenPaneLengthProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(OpenPaneLength),
			propertyType: typeof(double),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: 320.0,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged_CoerceToGreaterThanZero(e))
		);

		public static DependencyProperty PaneFooterProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneFooter",
			typeof(UIElement),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(UIElement),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty PaneToggleButtonStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneToggleButtonStyle",
			typeof(Style),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(null,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e))
		);

		public static DependencyProperty SelectedItemProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(SelectedItem),
			propertyType: typeof(object),
			ownerType: typeof(NavigationView),
			typeMetadata: new FrameworkPropertyMetadata(
				null,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)
			)
		);

		public static DependencyProperty SettingsItemProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"SettingsItem",
			typeof(object),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(object),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty IsBackButtonVisibleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsBackButtonVisible",
			typeof(NavigationViewBackButtonVisible),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(NavigationViewBackButtonVisible.Auto,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty IsBackEnabledProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsBackEnabled",
			typeof(bool),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(
				false,
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));

		public static DependencyProperty PaneTitleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneTitle",
			typeof(string),
			typeof(NavigationView),
			new FrameworkPropertyMetadata(default(string),
				propertyChangedCallback: (s, e) => (s as NavigationView)?.OnPropertyChanged(e)));
	}
}
