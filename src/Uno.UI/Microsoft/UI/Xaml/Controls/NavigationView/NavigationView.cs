// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationView.cpp, commit 9f7c129

#pragma warning disable 105 // remove when moving to WinUI tree

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Numerics;
using Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls.AnimatedVisuals;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class NavigationView : ContentControl
{
	// General items
	private const string c_togglePaneButtonName = "TogglePaneButton";
	private const string c_paneTitleHolderFrameworkElement = "PaneTitleHolder";
	private const string c_paneTitleFrameworkElement = "PaneTitleTextBlock";
	private const string c_rootSplitViewName = "RootSplitView";
	private const string c_menuItemsHost = "MenuItemsHost";
	private const string c_footerMenuItemsHost = "FooterMenuItemsHost";
	//private const string c_selectionIndicatorName = "SelectionIndicator";
	private const string c_paneContentGridName = "PaneContentGrid";
	private const string c_rootGridName = "RootGrid";
	private const string c_contentGridName = "ContentGrid";
	private const string c_searchButtonName = "PaneAutoSuggestButton";
	//private const string c_paneToggleButtonIconGridColumnName = "PaneToggleButtonIconWidthColumn";
	private const string c_togglePaneTopPadding = "TogglePaneTopPadding";
	private const string c_contentPaneTopPadding = "ContentPaneTopPadding";
	private const string c_contentLeftPadding = "ContentLeftPadding";
	private const string c_navViewBackButton = "NavigationViewBackButton";
	private const string c_navViewBackButtonToolTip = "NavigationViewBackButtonToolTip";
	private const string c_navViewCloseButton = "NavigationViewCloseButton";
	private const string c_navViewCloseButtonToolTip = "NavigationViewCloseButtonToolTip";
	private const string c_paneShadowReceiverCanvas = "PaneShadowReceiver";
	private const string c_flyoutRootGrid = "FlyoutRootGrid";
	private const string c_settingsItemTag = "Settings";

	// DisplayMode Top specific items
	private const string c_topNavMenuItemsHost = "TopNavMenuItemsHost";
	private const string c_topNavFooterMenuItemsHost = "TopFooterMenuItemsHost";
	private const string c_topNavOverflowButton = "TopNavOverflowButton";
	private const string c_topNavMenuItemsOverflowHost = "TopNavMenuItemsOverflowHost";
	private const string c_topNavGrid = "TopNavGrid";
	private const string c_topNavContentOverlayAreaGrid = "TopNavContentOverlayAreaGrid";
	private const string c_leftNavPaneAutoSuggestBoxPresenter = "PaneAutoSuggestBoxPresenter";
	private const string c_topNavPaneAutoSuggestBoxPresenter = "TopPaneAutoSuggestBoxPresenter";
	private const string c_paneTitlePresenter = "PaneTitlePresenter";

	// DisplayMode Left specific items
	private const string c_leftNavFooterContentBorder = "FooterContentBorder";
	private const string c_leftNavPaneHeaderContentBorder = "PaneHeaderContentBorder";
	private const string c_leftNavPaneCustomContentBorder = "PaneCustomContentBorder";

	private const string c_itemsContainer = "ItemsContainerGrid";
	private const string c_itemsContainerRow = "ItemsContainerRow";
	private const string c_menuItemsScrollViewer = "MenuItemsScrollViewer";
	private const string c_footerItemsScrollViewer = "FooterItemsScrollViewer";

	private const string c_paneHeaderOnTopPane = "PaneHeaderOnTopPane";
	private const string c_paneTitleOnTopPane = "PaneTitleOnTopPane";
	private const string c_paneCustomContentOnTopPane = "PaneCustomContentOnTopPane";
	private const string c_paneFooterOnTopPane = "PaneFooterOnTopPane";
	private const string c_paneHeaderCloseButtonColumn = "PaneHeaderCloseButtonColumn";
	private const string c_paneHeaderToggleButtonColumn = "PaneHeaderToggleButtonColumn";
	private const string c_paneHeaderContentBorderRow = "PaneHeaderContentBorderRow";

	private const string c_separatorVisibleStateName = "SeparatorVisible";
	private const string c_separatorCollapsedStateName = "SeparatorCollapsed";

	private const int c_backButtonHeight = 40;
	private const int c_backButtonWidth = 40;
	private const int c_paneToggleButtonHeight = 40;
	private const int c_paneToggleButtonWidth = 40;
	private const int c_toggleButtonHeightWhenShouldPreserveNavigationViewRS3Behavior = 56;
	private const int c_backButtonRowDefinition = 1;
	private const float c_paneElevationTranslationZ = 32;

	private const int c_mainMenuBlockIndex = 0;
	private const int c_footerMenuBlockIndex = 1;

	private const string c_shadowCaster = "ShadowCaster";
	private const string c_shadowCasterEaseOutStoryboard = "ShadowCasterEaseOutStoryboard";

	private int itemNotFound = -1;

	private static Size c_infSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

	~NavigationView()
	{
		UnhookEventsAndClearFields(true);
	}

	// UIElement / UIElementOverridesHelper
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new NavigationViewAutomationPeer(this);
	}

	internal void UnhookEventsAndClearFields(bool isFromDestructor = false)
	{
		m_titleBarMetricsChangedRevoker.Disposable = null;
		m_titleBarIsVisibleChangedRevoker.Disposable = null;
		m_paneToggleButtonClickRevoker.Disposable = null;

		m_settingsItem = null;

		m_paneSearchButtonClickRevoker.Disposable = null;
		m_paneSearchButton = null;

		m_paneHeaderOnTopPane = null;
		m_paneTitleOnTopPane = null;

		m_itemsContainerSizeChangedRevoker.Disposable = null;
		m_paneTitleHolderFrameworkElementSizeChangedRevoker.Disposable = null;
		m_paneTitleHolderFrameworkElement = null;

		m_paneTitleFrameworkElement = null;
		m_paneTitlePresenter = null;

		m_paneHeaderCloseButtonColumn = null;
		m_paneHeaderToggleButtonColumn = null;
		m_paneHeaderContentBorderRow = null;

		m_leftNavItemsRepeaterElementPreparedRevoker.Disposable = null;
		m_leftNavItemsRepeaterElementClearingRevoker.Disposable = null;
		m_leftNavRepeaterLoadedRevoker.Disposable = null;
		m_leftNavRepeaterGettingFocusRevoker.Disposable = null;
		m_leftNavRepeater = null;

		m_topNavItemsRepeaterElementPreparedRevoker.Disposable = null;
		m_topNavItemsRepeaterElementClearingRevoker.Disposable = null;
		m_topNavRepeaterLoadedRevoker.Disposable = null;
		m_topNavRepeaterGettingFocusRevoker.Disposable = null;
		m_topNavRepeater = null;

		m_leftNavFooterMenuItemsRepeaterElementPreparedRevoker.Disposable = null;
		m_leftNavFooterMenuItemsRepeaterElementClearingRevoker.Disposable = null;
		m_leftNavFooterMenuRepeaterLoadedRevoker.Disposable = null;
		m_leftNavFooterMenuRepeaterGettingFocusRevoker.Disposable = null;
		m_leftNavFooterMenuRepeater = null;

		m_topNavFooterMenuItemsRepeaterElementPreparedRevoker.Disposable = null;
		m_topNavFooterMenuItemsRepeaterElementClearingRevoker.Disposable = null;
		m_topNavFooterMenuRepeaterLoadedRevoker.Disposable = null;
		m_topNavFooterMenuRepeaterGettingFocusRevoker.Disposable = null;
		m_topNavFooterMenuRepeater = null;

		m_footerItemsCollectionChangedRevoker.Disposable = null;
		m_menuItemsCollectionChangedRevoker.Disposable = null;

		m_topNavOverflowItemsRepeaterElementPreparedRevoker.Disposable = null;
		m_topNavOverflowItemsRepeaterElementClearingRevoker.Disposable = null;
		m_topNavRepeaterOverflowView = null;

		m_topNavOverflowItemsCollectionChangedRevoker.Disposable = null;

		m_shadowCasterEaseOutStoryboardRevoker.Disposable = null;

#if IS_UNO
		//TODO: Uno specific - remove when #4689 is fixed
		m_leftNavItemsRepeaterUnoBeforeElementPreparedRevoker.Disposable = null;
		m_leftNavFooterMenuItemsRepeaterUnoBeforeElementPreparedRevoker.Disposable = null;
		m_topNavFooterMenuItemsRepeaterUnoBeforeElementPreparedRevoker.Disposable = null;
		m_topNavItemsRepeaterUnoBeforeElementPreparedRevoker.Disposable = null;
		m_topNavOverflowItemsRepeaterUnoBeforeElementPreparedRevoker.Disposable = null;
#endif

		if (isFromDestructor)
		{
			m_selectionChangedRevoker.Disposable = null;
			m_autoSuggestBoxQuerySubmittedRevoker.Disposable = null;
			ClearAllNavigationViewItemRevokers();
		}
	}

	public NavigationView()
	{
#if IS_UNO
		// Uno specific - need to initialize here to be able to use "this"
		m_topDataProvider = new TopNavigationViewDataProvider(this);
#endif
		SetValue(TemplateSettingsProperty, new NavigationViewTemplateSettings());
		DefaultStyleKey = typeof(NavigationView);

		SizeChanged += OnSizeChanged;

		m_selectionModelSource = new ObservableCollection<object>();
		m_selectionModelSource.Add(null);
		m_selectionModelSource.Add(null);

		var items = new ObservableCollection<object>();
		SetValue(MenuItemsProperty, items);

		var footerItems = new ObservableCollection<object>();
		SetValue(FooterMenuItemsProperty, footerItems);

		WeakReference<NavigationView> weakThis = new WeakReference<NavigationView>(this);
		m_topDataProvider.OnRawDataChanged(args =>
		{
			if (weakThis.TryGetTarget(out var target))
			{
				target.OnTopNavDataSourceChanged(args);
			}
		});

		Unloaded += OnUnloaded;
		Loaded += OnLoaded;

		m_selectionModel.SingleSelect = true;
		m_selectionModel.Source = m_selectionModelSource;
		m_selectionModel.SelectionChanged += OnSelectionModelSelectionChanged;
		m_selectionChangedRevoker.Disposable = Disposable.Create(() => m_selectionModel.SelectionChanged -= OnSelectionModelSelectionChanged);
		m_selectionModel.ChildrenRequested += OnSelectionModelChildrenRequested;
		m_childrenRequestedRevoker.Disposable = Disposable.Create(() => m_selectionModel.ChildrenRequested -= OnSelectionModelChildrenRequested);

		m_navigationViewItemsFactory = new NavigationViewItemsFactory();
	}

	private void OnSelectionModelChildrenRequested(SelectionModel selectionModel, SelectionModelChildrenRequestedEventArgs e)
	{
		// this is main menu or footer
		if (e.SourceIndex.GetSize() == 1)
		{
			e.Children = e.Source;
		}
		else if (e.Source is NavigationViewItem nvi)
		{
			e.Children = GetChildren(nvi);
		}
		else
		{
			var children = GetChildrenForItemInIndexPath(e.SourceIndex, true /*forceRealize*/);
			if (children != null)
			{
				e.Children = children;
			}
		}
	}

	private void OnFooterItemsSourceCollectionChanged(object sender, object args)
	{
		UpdateFooterRepeaterItemsSource(false /*sourceCollectionReset*/, true /*sourceCollectionChanged*/);

		// Pane footer items changed. This means we might need to reevaluate the pane layout.
		UpdatePaneLayout();
	}

	private void OnOverflowItemsSourceCollectionChanged(object sender, object args)
	{
		if (m_topNavRepeaterOverflowView.ItemsSourceView.Count == 0)
		{
			SetOverflowButtonVisibility(Visibility.Collapsed);
		}
	}

	private void OnSelectionModelSelectionChanged(SelectionModel selectionModel, SelectionModelSelectionChangedEventArgs e)
	{
		var selectedItem = selectionModel.SelectedItem;

		// Ignore this callback if:
		// 1. the SelectedItem property of NavigationView is already set to the item
		//    being passed in this callback. This is because the item has already been selected
		//    via API and we are just updating the m_selectionModel state to accurately reflect the new selection.
		// 2. Template has not been applied yet. SelectionModel's selectedIndex state will get properly updated
		//    after the repeater finishes loading.
		// TODO: Update SelectedItem comparison to work for the exact same item datasource scenario
		if (m_shouldIgnoreNextSelectionChange || selectedItem == SelectedItem || !m_appliedTemplate)
		{
			return;
		}

		bool setSelectedItem = true;
		var selectedIndex = selectionModel.SelectedIndex;

		if (IsTopNavigationView())
		{
			// If selectedIndex does not exist, means item is being deselected through API
			var isInOverflow = (selectedIndex != null && selectedIndex.GetSize() > 1)
				? selectedIndex.GetAt(0) == c_mainMenuBlockIndex && !m_topDataProvider.IsItemInPrimaryList(selectedIndex.GetAt(1))
				: false;
			if (isInOverflow)
			{
				// We only want to close the overflow flyout and move the item on selection if it is a leaf node
				bool GetItemShouldBeMoved(IndexPath selectedIndex)
				{
					var selectedContainer = GetContainerForIndexPath(selectedIndex);
					if (selectedContainer != null)
					{
						if (selectedContainer is NavigationViewItem selectedNVI)
						{
							if (DoesNavigationViewItemHaveChildren(selectedNVI))
							{
								return false;
							}
						}
					}
					return true;
				}

				var itemShouldBeMoved = GetItemShouldBeMoved(selectedIndex);

				if (itemShouldBeMoved)
				{
					SelectandMoveOverflowItem(selectedItem, selectedIndex, true /*closeFlyout*/);
					setSelectedItem = false;
				}
				else
				{
					m_moveTopNavOverflowItemOnFlyoutClose = true;
				}
			}
		}

		if (setSelectedItem)
		{
			SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(selectedItem);
		}
	}

	private void SelectandMoveOverflowItem(object selectedItem, IndexPath selectedIndex, bool closeFlyout)
	{
		// SelectOverflowItem is moving data in/out of overflow.
		try
		{
			m_selectionChangeFromOverflowMenu = true;

			if (closeFlyout)
			{
				CloseTopNavigationViewFlyout();
			}

			if (!IsSelectionSuppressed(selectedItem))
			{
				SelectOverflowItem(selectedItem, selectedIndex);
			}
		}
		finally
		{
			m_selectionChangeFromOverflowMenu = false;
		}
	}

	// We only need to close the flyout if the selected item is a leaf node
	private void CloseFlyoutIfRequired(NavigationViewItem selectedItem)
	{
		var selectedIndex = m_selectionModel.SelectedIndex;

		bool GetIsInModeWithFlyout()
		{
			var splitView = m_rootSplitView;
			if (splitView != null)
			{
				// Check if the pane is closed and if the splitview is in either compact mode.
				var splitViewDisplayMode = splitView.DisplayMode;
				return (!splitView.IsPaneOpen && (splitViewDisplayMode == SplitViewDisplayMode.CompactOverlay || splitViewDisplayMode == SplitViewDisplayMode.CompactInline)) ||
						PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
			}
			return false;
		}

		bool isInModeWithFlyout = GetIsInModeWithFlyout();

		if (isInModeWithFlyout && selectedIndex != null && !DoesNavigationViewItemHaveChildren(selectedItem))
		{
			// Item selected is a leaf node, find top level parent and close flyout
			var rootItem = GetContainerForIndex(selectedIndex.GetAt(1), selectedIndex.GetAt(0) == c_footerMenuBlockIndex /* inFooter */);
			if (rootItem != null)
			{
				if (rootItem is NavigationViewItem nvi)
				{
					var nviImpl = nvi;
					if (nviImpl.ShouldRepeaterShowInFlyout())
					{
						nvi.IsExpanded = false;
					}
				}
			}
		}
	}

	protected override void OnApplyTemplate()
	{
		// Stop update anything because of PropertyChange during OnApplyTemplate. Update them all together at the end of this function
		m_appliedTemplate = false;
		try
		{
			m_fromOnApplyTemplate = true;

			UnhookEventsAndClearFields();

			//IControlProtected controlProtected = this;

			// Set up the pane toggle button click handler
			var paneToggleButton = GetTemplateChild(c_togglePaneButtonName) as Button;
			if (paneToggleButton != null)
			{
				m_paneToggleButton = paneToggleButton;
				paneToggleButton.Click += OnPaneToggleButtonClick;
				m_paneToggleButtonClickRevoker.Disposable = Disposable.Create(() => paneToggleButton.Click -= OnPaneToggleButtonClick);

				SetPaneToggleButtonAutomationName();

				if (SharedHelpers.IsRS3OrHigher())
				{
					KeyboardAccelerator keyboardAccelerator = new KeyboardAccelerator();
					keyboardAccelerator.Key = VirtualKey.Back;
					keyboardAccelerator.Modifiers = VirtualKeyModifiers.Windows;
					paneToggleButton.KeyboardAccelerators.Add(keyboardAccelerator);
				}
			}

			m_leftNavPaneHeaderContentBorder = (ContentControl)GetTemplateChild(c_leftNavPaneHeaderContentBorder);
			m_leftNavPaneCustomContentBorder = (ContentControl)GetTemplateChild(c_leftNavPaneCustomContentBorder);
			m_leftNavFooterContentBorder = (ContentControl)GetTemplateChild(c_leftNavFooterContentBorder);
			m_paneHeaderOnTopPane = (ContentControl)GetTemplateChild(c_paneHeaderOnTopPane);
			m_paneTitleOnTopPane = (ContentControl)GetTemplateChild(c_paneTitleOnTopPane);
			m_paneCustomContentOnTopPane = (ContentControl)GetTemplateChild(c_paneCustomContentOnTopPane);
			m_paneFooterOnTopPane = (ContentControl)GetTemplateChild(c_paneFooterOnTopPane);

			// Get a pointer to the root SplitView
			var splitView = GetTemplateChild(c_rootSplitViewName) as SplitView;
			if (splitView != null)
			{
				m_rootSplitView = splitView;
				var splitViewIsPaneOpenSubscription = splitView.RegisterPropertyChangedCallback(SplitView.IsPaneOpenProperty, OnSplitViewClosedCompactChanged);
				m_splitViewIsPaneOpenChangedRevoker.Disposable = Disposable.Create(() => splitView.UnregisterPropertyChangedCallback(SplitView.IsPaneOpenProperty, splitViewIsPaneOpenSubscription));

				var splitViewDisplayModeChangedSubscription = splitView.RegisterPropertyChangedCallback(SplitView.DisplayModeProperty, OnSplitViewClosedCompactChanged);
				m_splitViewDisplayModeChangedRevoker.Disposable = Disposable.Create(() => splitView.UnregisterPropertyChangedCallback(SplitView.DisplayModeProperty, splitViewDisplayModeChangedSubscription));

				if (SharedHelpers.IsRS3OrHigher()) // These events are new to RS3/v5 API
				{
					splitView.PaneClosed += OnSplitViewPaneClosed;
					m_splitViewPaneClosedRevoker.Disposable = Disposable.Create(() => splitView.PaneClosed -= OnSplitViewPaneClosed);
					splitView.PaneClosing += OnSplitViewPaneClosing;
					m_splitViewPaneClosingRevoker.Disposable = Disposable.Create(() => splitView.PaneClosing -= OnSplitViewPaneClosing);
					splitView.PaneOpened += OnSplitViewPaneOpened;
					m_splitViewPaneOpenedRevoker.Disposable = Disposable.Create(() => splitView.PaneOpened -= OnSplitViewPaneOpened);
					splitView.PaneOpening += OnSplitViewPaneOpening;
					m_splitViewPaneOpeningRevoker.Disposable = Disposable.Create(() => splitView.PaneOpening -= OnSplitViewPaneOpening);
				}

				UpdateIsClosedCompact();
			}

			m_topNavGrid = (Grid)GetTemplateChild(c_topNavGrid);

			// Change code to NOT do this if we're in top nav mode, to prevent it from being realized:
			var leftNavRepeater = GetTemplateChild(c_menuItemsHost) as ItemsRepeater;
			if (leftNavRepeater != null)
			{
				m_leftNavRepeater = leftNavRepeater;

				// API is currently in preview, so setting this via code.
				// Disabling virtualization for now because of https://github.com/microsoft/microsoft-ui-xaml/issues/2095
				if (leftNavRepeater.Layout is StackLayout stackLayout)
				{
					var stackLayoutImpl = stackLayout;
					stackLayoutImpl.DisableVirtualization = true;
				}

#if IS_UNO
				//TODO: Uno specific - remove when #4689 is fixed
				leftNavRepeater.UnoBeforeElementPrepared += OnRepeaterUnoBeforeElementPrepared;
				m_leftNavItemsRepeaterUnoBeforeElementPreparedRevoker.Disposable = Disposable.Create(() => leftNavRepeater.UnoBeforeElementPrepared -= OnRepeaterUnoBeforeElementPrepared);
#endif

				leftNavRepeater.ElementPrepared += OnRepeaterElementPrepared;
				m_leftNavItemsRepeaterElementPreparedRevoker.Disposable = Disposable.Create(() => leftNavRepeater.ElementPrepared -= OnRepeaterElementPrepared);
				leftNavRepeater.ElementClearing += OnRepeaterElementClearing;
				m_leftNavItemsRepeaterElementClearingRevoker.Disposable = Disposable.Create(() => leftNavRepeater.ElementClearing -= OnRepeaterElementClearing);

				leftNavRepeater.Loaded += OnRepeaterLoaded;
				m_leftNavRepeaterLoadedRevoker.Disposable = Disposable.Create(() => leftNavRepeater.Loaded -= OnRepeaterLoaded);

				leftNavRepeater.GettingFocus += OnRepeaterGettingFocus;
				m_leftNavRepeaterGettingFocusRevoker.Disposable = Disposable.Create(() => leftNavRepeater.GettingFocus -= OnRepeaterGettingFocus);

				leftNavRepeater.ItemTemplate = m_navigationViewItemsFactory;
			}

			// Change code to NOT do this if we're in left nav mode, to prevent it from being realized:
			var topNavRepeater = GetTemplateChild(c_topNavMenuItemsHost) as ItemsRepeater;
			if (topNavRepeater != null)
			{
				m_topNavRepeater = topNavRepeater;

				// API is currently in preview, so setting this via code
				if (topNavRepeater.Layout is StackLayout stackLayout)
				{
					var stackLayoutImpl = stackLayout;
					stackLayoutImpl.DisableVirtualization = true;
				}

#if IS_UNO
				//TODO: Uno specific - remove when #4689 is fixed
				topNavRepeater.UnoBeforeElementPrepared += OnRepeaterUnoBeforeElementPrepared;
				m_topNavItemsRepeaterUnoBeforeElementPreparedRevoker.Disposable = Disposable.Create(() => topNavRepeater.UnoBeforeElementPrepared -= OnRepeaterUnoBeforeElementPrepared);
#endif

				topNavRepeater.ElementPrepared += OnRepeaterElementPrepared;
				m_topNavItemsRepeaterElementPreparedRevoker.Disposable = Disposable.Create(() => topNavRepeater.ElementPrepared -= OnRepeaterElementPrepared);
				topNavRepeater.ElementClearing += OnRepeaterElementClearing;
				m_topNavItemsRepeaterElementClearingRevoker.Disposable = Disposable.Create(() => topNavRepeater.ElementClearing -= OnRepeaterElementClearing);

				topNavRepeater.Loaded += OnRepeaterLoaded;
				m_topNavRepeaterLoadedRevoker.Disposable = Disposable.Create(() => topNavRepeater.Loaded -= OnRepeaterLoaded);

				topNavRepeater.GettingFocus += OnRepeaterGettingFocus;
				m_topNavRepeaterGettingFocusRevoker.Disposable = Disposable.Create(() => topNavRepeater.GettingFocus -= OnRepeaterGettingFocus);

				topNavRepeater.ItemTemplate = m_navigationViewItemsFactory;
			}

			// Change code to NOT do this if we're in left nav mode, to prevent it from being realized:
			var topNavListOverflowRepeater = GetTemplateChild(c_topNavMenuItemsOverflowHost) as ItemsRepeater;
			if (topNavListOverflowRepeater != null)
			{
				m_topNavRepeaterOverflowView = topNavListOverflowRepeater;

				// API is currently in preview, so setting this via code.
				// Disabling virtualization for now because of https://github.com/microsoft/microsoft-ui-xaml/issues/2095
				if (topNavListOverflowRepeater.Layout is StackLayout stackLayout)
				{
					var stackLayoutImpl = stackLayout;
					stackLayoutImpl.DisableVirtualization = true;
				}

#if IS_UNO
				//TODO: Uno specific - remove when #4689 is fixed
				topNavListOverflowRepeater.UnoBeforeElementPrepared += OnRepeaterUnoBeforeElementPrepared;
				m_topNavOverflowItemsRepeaterUnoBeforeElementPreparedRevoker.Disposable = Disposable.Create(() => topNavListOverflowRepeater.UnoBeforeElementPrepared -= OnRepeaterUnoBeforeElementPrepared);
#endif

				topNavListOverflowRepeater.ElementPrepared += OnRepeaterElementPrepared;
				m_topNavOverflowItemsRepeaterElementPreparedRevoker.Disposable = Disposable.Create(() => topNavListOverflowRepeater.ElementPrepared -= OnRepeaterElementPrepared);
				topNavListOverflowRepeater.ElementClearing += OnRepeaterElementClearing;
				m_topNavOverflowItemsRepeaterElementClearingRevoker.Disposable = Disposable.Create(() => topNavListOverflowRepeater.ElementClearing -= OnRepeaterElementClearing);

				topNavListOverflowRepeater.ItemTemplate = m_navigationViewItemsFactory;
			}

			var topNavOverflowButton = GetTemplateChild(c_topNavOverflowButton) as Button;
			if (topNavOverflowButton != null)
			{
				m_topNavOverflowButton = topNavOverflowButton;
				AutomationProperties.SetName(topNavOverflowButton, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationOverflowButtonName));
				topNavOverflowButton.Content = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationOverflowButtonText);
				var visual = ElementCompositionPreview.GetElementVisual(topNavOverflowButton);
				CreateAndAttachHeaderAnimation(visual);

				var toolTip = ToolTipService.GetToolTip(topNavOverflowButton);
				if (toolTip == null)
				{
					var tooltip = new ToolTip();
					tooltip.Content = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationOverflowButtonToolTip);
					ToolTipService.SetToolTip(topNavOverflowButton, tooltip);
				}

				var flyoutBase = topNavOverflowButton.Flyout;
				if (flyoutBase != null)
				{
					FlyoutBase topNavOverflowButtonAsFlyoutBase6 = flyoutBase;
					if (topNavOverflowButtonAsFlyoutBase6 != null)
					{
						topNavOverflowButtonAsFlyoutBase6.ShouldConstrainToRootBounds = false;
					}
					flyoutBase.Closing += OnFlyoutClosing;
					m_flyoutClosingRevoker.Disposable = Disposable.Create(() => flyoutBase.Closing -= OnFlyoutClosing);
				}
			}

			// Change code to NOT do this if we're in top nav mode, to prevent it from being realized:
			var leftFooterMenuNavRepeater = GetTemplateChild(c_footerMenuItemsHost) as ItemsRepeater;
			if (leftFooterMenuNavRepeater != null)
			{
				m_leftNavFooterMenuRepeater = leftFooterMenuNavRepeater;

				// API is currently in preview, so setting this via code.
				// Disabling virtualization for now because of https://github.com/microsoft/microsoft-ui-xaml/issues/2095
				if (leftFooterMenuNavRepeater.Layout is StackLayout stackLayout)
				{
					var stackLayoutImpl = stackLayout;
					stackLayoutImpl.DisableVirtualization = true;
				}

#if IS_UNO
				//TODO: Uno specific - remove when #4689 is fixed
				leftFooterMenuNavRepeater.UnoBeforeElementPrepared += OnRepeaterUnoBeforeElementPrepared;
				m_leftNavFooterMenuItemsRepeaterUnoBeforeElementPreparedRevoker.Disposable = Disposable.Create(() => leftFooterMenuNavRepeater.UnoBeforeElementPrepared -= OnRepeaterUnoBeforeElementPrepared);
#endif

				leftFooterMenuNavRepeater.ElementPrepared += OnRepeaterElementPrepared;
				m_leftNavFooterMenuItemsRepeaterElementPreparedRevoker.Disposable = Disposable.Create(() => leftFooterMenuNavRepeater.ElementPrepared -= OnRepeaterElementPrepared);
				leftFooterMenuNavRepeater.ElementClearing += OnRepeaterElementClearing;
				m_leftNavFooterMenuItemsRepeaterElementClearingRevoker.Disposable = Disposable.Create(() => leftFooterMenuNavRepeater.ElementClearing -= OnRepeaterElementClearing);

				leftFooterMenuNavRepeater.Loaded += OnRepeaterLoaded;
				m_leftNavFooterMenuRepeaterLoadedRevoker.Disposable = Disposable.Create(() => leftFooterMenuNavRepeater.Loaded -= OnRepeaterLoaded);

				leftFooterMenuNavRepeater.GettingFocus += OnRepeaterGettingFocus;
				m_leftNavFooterMenuRepeaterGettingFocusRevoker.Disposable = Disposable.Create(() => leftFooterMenuNavRepeater.GettingFocus -= OnRepeaterGettingFocus);

				leftFooterMenuNavRepeater.ItemTemplate = m_navigationViewItemsFactory;
			}

			// Change code to NOT do this if we're in left nav mode, to prevent it from being realized:
			var topFooterMenuNavRepeater = GetTemplateChild(c_topNavFooterMenuItemsHost) as ItemsRepeater;
			if (topFooterMenuNavRepeater != null)
			{
				m_topNavFooterMenuRepeater = topFooterMenuNavRepeater;

				// API is currently in preview, so setting this via code.
				// Disabling virtualization for now because of https://github.com/microsoft/microsoft-ui-xaml/issues/2095
				if (topFooterMenuNavRepeater.Layout is StackLayout stackLayout)
				{
					var stackLayoutImpl = stackLayout;
					stackLayoutImpl.DisableVirtualization = true;
				}

#if IS_UNO
				//TODO: Uno specific - remove when #4689 is fixed
				topFooterMenuNavRepeater.UnoBeforeElementPrepared += OnRepeaterUnoBeforeElementPrepared;
				m_topNavFooterMenuItemsRepeaterUnoBeforeElementPreparedRevoker.Disposable = Disposable.Create(() => topFooterMenuNavRepeater.UnoBeforeElementPrepared -= OnRepeaterUnoBeforeElementPrepared);
#endif

				topFooterMenuNavRepeater.ElementPrepared += OnRepeaterElementPrepared;
				m_topNavFooterMenuItemsRepeaterElementPreparedRevoker.Disposable = Disposable.Create(() => topFooterMenuNavRepeater.ElementPrepared -= OnRepeaterElementPrepared);
				topFooterMenuNavRepeater.ElementClearing += OnRepeaterElementClearing;
				m_topNavFooterMenuItemsRepeaterElementClearingRevoker.Disposable = Disposable.Create(() => topFooterMenuNavRepeater.ElementClearing -= OnRepeaterElementClearing);

				topFooterMenuNavRepeater.Loaded += OnRepeaterLoaded;
				m_topNavFooterMenuRepeaterLoadedRevoker.Disposable = Disposable.Create(() => topFooterMenuNavRepeater.Loaded -= OnRepeaterLoaded);

				topFooterMenuNavRepeater.GettingFocus += OnRepeaterGettingFocus;
				m_topNavFooterMenuRepeaterGettingFocusRevoker.Disposable = Disposable.Create(() => topFooterMenuNavRepeater.GettingFocus -= OnRepeaterGettingFocus);

				topFooterMenuNavRepeater.ItemTemplate = m_navigationViewItemsFactory;
			}

			m_topNavContentOverlayAreaGrid = (Border)GetTemplateChild(c_topNavContentOverlayAreaGrid);
			m_leftNavPaneAutoSuggestBoxPresenter = (ContentControl)GetTemplateChild(c_leftNavPaneAutoSuggestBoxPresenter);
			m_topNavPaneAutoSuggestBoxPresenter = (ContentControl)GetTemplateChild(c_topNavPaneAutoSuggestBoxPresenter);

			// Get pointer to the pane content area, for use in the selection indicator animation
			m_paneContentGrid = (UIElement)GetTemplateChild(c_paneContentGridName);

			m_contentLeftPadding = (FrameworkElement)GetTemplateChild(c_contentLeftPadding);

			m_paneHeaderCloseButtonColumn = (ColumnDefinition)GetTemplateChild(c_paneHeaderCloseButtonColumn);
			m_paneHeaderToggleButtonColumn = (ColumnDefinition)GetTemplateChild(c_paneHeaderToggleButtonColumn);
			m_paneHeaderContentBorderRow = (RowDefinition)GetTemplateChild(c_paneHeaderContentBorderRow);
			m_paneTitleFrameworkElement = (FrameworkElement)GetTemplateChild(c_paneTitleFrameworkElement);
			m_paneTitlePresenter = (ContentControl)GetTemplateChild(c_paneTitlePresenter);

			var paneTitleHolderFrameworkElement = GetTemplateChild(c_paneTitleHolderFrameworkElement) as FrameworkElement;
			if (paneTitleHolderFrameworkElement != null)
			{
				m_paneTitleHolderFrameworkElement = paneTitleHolderFrameworkElement;
				paneTitleHolderFrameworkElement.SizeChanged += OnPaneTitleHolderSizeChanged;
				m_paneTitleHolderFrameworkElementSizeChangedRevoker.Disposable = Disposable.Create(() => paneTitleHolderFrameworkElement.SizeChanged -= OnPaneTitleHolderSizeChanged);
			}

			// Set automation name on search button
			var button = GetTemplateChild(c_searchButtonName) as Button;
			if (button != null)
			{
				m_paneSearchButton = button;
				button.Click += OnPaneSearchButtonClick;
				m_paneSearchButtonClickRevoker.Disposable = Disposable.Create(() => button.Click -= OnPaneSearchButtonClick);

				var searchButtonName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationViewSearchButtonName);
				AutomationProperties.SetName(button, searchButtonName);
				var toolTip = new ToolTip();
				toolTip.Content = searchButtonName;
				ToolTipService.SetToolTip(button, toolTip);
			}

			var backButton = GetTemplateChild(c_navViewBackButton) as Button;
			if (backButton != null)
			{
				m_backButton = backButton;
				backButton.Click += OnBackButtonClicked;
				m_backButtonClickedRevoker.Disposable = Disposable.Create(() => backButton.Click -= OnBackButtonClicked);

				string navigationName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationBackButtonName);
				AutomationProperties.SetName(backButton, navigationName);
			}

			// Register for changes in title bar layout
			var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
			if (coreTitleBar != null)
			{
				m_coreTitleBar = coreTitleBar;
				coreTitleBar.LayoutMetricsChanged += OnTitleBarMetricsChanged;
				m_titleBarMetricsChangedRevoker.Disposable = Disposable.Create(() => coreTitleBar.LayoutMetricsChanged -= OnTitleBarMetricsChanged);
				coreTitleBar.IsVisibleChanged += OnTitleBarIsVisibleChanged;
				m_titleBarIsVisibleChangedRevoker.Disposable = Disposable.Create(() => coreTitleBar.IsVisibleChanged -= OnTitleBarIsVisibleChanged);

				if (ShouldPreserveNavigationViewRS4Behavior())
				{
					m_togglePaneTopPadding = (FrameworkElement)GetTemplateChild(c_togglePaneTopPadding);
					m_contentPaneTopPadding = (FrameworkElement)GetTemplateChild(c_contentPaneTopPadding);
				}
			}

			var backButtonToolTip = GetTemplateChild(c_navViewBackButtonToolTip) as ToolTip;
			if (backButtonToolTip != null)
			{
				string navigationBackButtonToolTip = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationBackButtonToolTip);
				backButtonToolTip.Content = navigationBackButtonToolTip;
			}

			var closeButton = GetTemplateChild(c_navViewCloseButton) as Button;
			if (closeButton != null)
			{
				m_closeButton = closeButton;
				closeButton.Click += OnPaneToggleButtonClick;
				m_closeButtonClickedRevoker.Disposable = Disposable.Create(() => closeButton.Click -= OnPaneToggleButtonClick);

				string navigationName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationCloseButtonName);
				AutomationProperties.SetName(closeButton, navigationName);
			}

			var closeButtonToolTip = GetTemplateChild(c_navViewCloseButtonToolTip) as ToolTip;
			if (closeButtonToolTip != null)
			{
				string navigationCloseButtonToolTip = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationButtonOpenName);
				closeButtonToolTip.Content = navigationCloseButtonToolTip;
			}

			m_itemsContainerRow = (RowDefinition)GetTemplateChild(c_itemsContainerRow);
			m_menuItemsScrollViewer = (FrameworkElement)GetTemplateChild(c_menuItemsScrollViewer);
			m_footerItemsScrollViewer = (FrameworkElement)GetTemplateChild(c_footerItemsScrollViewer);

			m_itemsContainerSizeChangedRevoker.Disposable = null;
			if (GetTemplateChild<FrameworkElement>(c_itemsContainer) is { } itemsContainer)
			{
				m_itemsContainer = itemsContainer;

				m_itemsContainer.SizeChanged += OnItemsContainerSizeChanged;
				m_itemsContainerSizeChangedRevoker.Disposable = Disposable.Create(() => m_itemsContainer.SizeChanged -= OnItemsContainerSizeChanged);
			}

			if (SharedHelpers.IsRS2OrHigher())
			{
				// Get hold of the outermost grid and enable XYKeyboardNavigationMode
				// However, we only want this to work in the content pane + the hamburger button (which is not inside the splitview)
				// so disable it on the grid in the content area of the SplitView
				var rootGrid = GetTemplateChild(c_rootGridName) as Grid;
				if (rootGrid != null)
				{
					rootGrid.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
				}

				var contentGrid = GetTemplateChild(c_contentGridName) as Grid;
				if (contentGrid != null)
				{
					contentGrid.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Disabled;
				}
			}

			AccessKeyInvoked += OnAccessKeyInvoked;
			m_accessKeyInvokedRevoker.Disposable = Disposable.Create(() => AccessKeyInvoked -= OnAccessKeyInvoked);

			if (SharedHelpers.Is21H1OrHigher())
			{
				m_shadowCaster = GetTemplateChild<Grid>(c_shadowCaster);
				m_shadowCasterEaseOutStoryboard = GetTemplateChild<Storyboard>(c_shadowCasterEaseOutStoryboard);
			}
			else
			{
				UpdatePaneShadow();
			}

			m_appliedTemplate = true;

			// Do initial setup
			UpdatePaneDisplayMode();
			UpdateHeaderVisibility();
			UpdatePaneTitleFrameworkElementParents();
			UpdateTitleBarPadding();
			UpdatePaneTabFocusNavigation();
			UpdateBackAndCloseButtonsVisibility();
			UpdateSingleSelectionFollowsFocusTemplateSetting();
			UpdatePaneVisibility();
			UpdateVisualState();
			UpdatePaneTitleMargins();
			UpdatePaneLayout();
			UpdatePaneOverlayGroup();
		}
		finally
		{
			m_fromOnApplyTemplate = false;
		}
	}

	private void UpdateRepeaterItemsSource(bool forceSelectionModelUpdate)
	{
		object GetItemsSource()
		{
			var menuItemsSource = MenuItemsSource;
			if (menuItemsSource != null)
			{
				return menuItemsSource;
			}
			UpdateSelectionForMenuItems();
			return MenuItems;
		}
		var itemsSource = GetItemsSource();

		// Selection Model has same representation of data regardless
		// of pane mode, so only update if the ItemsSource data itself
		// has changed.
		if (forceSelectionModelUpdate)
		{
			m_selectionModelSource[0] = itemsSource;
		}

		m_menuItemsCollectionChangedRevoker.Disposable = null;
		m_menuItemsSource = new InspectingDataSource(itemsSource);
		m_menuItemsSource.CollectionChanged += OnMenuItemsSourceCollectionChanged;
		m_menuItemsCollectionChangedRevoker.Disposable = Disposable.Create(() => m_menuItemsSource.CollectionChanged -= OnMenuItemsSourceCollectionChanged);

		if (IsTopNavigationView())
		{
			UpdateLeftRepeaterItemSource(null);
			UpdateTopNavRepeatersItemSource(itemsSource);
			InvalidateTopNavPrimaryLayout();
		}
		else
		{
			UpdateTopNavRepeatersItemSource(null);
			UpdateLeftRepeaterItemSource(itemsSource);
		}
	}

	private void UpdateLeftRepeaterItemSource(object items)
	{
		UpdateItemsRepeaterItemsSource(m_leftNavRepeater, items);
		// Left pane repeater has a new items source, update pane layout.
		UpdatePaneLayout();
	}

	private void UpdateTopNavRepeatersItemSource(object items)
	{
		// Change data source and setup vectors
		m_topDataProvider.SetDataSource(items);

		// rebinding
		UpdateTopNavPrimaryRepeaterItemsSource(items);
		UpdateTopNavOverflowRepeaterItemsSource(items);
	}

	private void UpdateTopNavPrimaryRepeaterItemsSource(object items)
	{
		if (items != null)
		{
			UpdateItemsRepeaterItemsSource(m_topNavRepeater, m_topDataProvider.GetPrimaryItems());
		}
		else
		{
			UpdateItemsRepeaterItemsSource(m_topNavRepeater, null);
		}
	}

	private void UpdateTopNavOverflowRepeaterItemsSource(object items)
	{
		m_topNavOverflowItemsCollectionChangedRevoker.Disposable = null;

		var overflowRepeater = m_topNavRepeaterOverflowView;
		if (overflowRepeater != null)
		{
			if (items != null)
			{
				var itemsSource = m_topDataProvider.GetOverflowItems();
				overflowRepeater.ItemsSource = itemsSource;

				// We listen to changes to the overflow menu item collection so we can set the visibility of the overflow button
				// to collapsed when it no longer has any items.
				//
				// Normally, MeasureOverride() kicks off updating the button's visibility, however, it is not run when the overflow menu
				// only contains a *single* item and we
				// - either remove that menu item or
				// - remove menu items displayed in the NavigationView pane until there is enough room for the single overflow menu item
				//   to be displayed in the pane
				overflowRepeater.ItemsSourceView.CollectionChanged += OnOverflowItemsSourceCollectionChanged;
				m_topNavOverflowItemsCollectionChangedRevoker.Disposable = Disposable.Create(() => overflowRepeater.ItemsSourceView.CollectionChanged -= OnOverflowItemsSourceCollectionChanged);
			}
			else
			{
				overflowRepeater.ItemsSource = null;
			}
		}
	}

	private void UpdateItemsRepeaterItemsSource(ItemsRepeater ir,
		 object itemsSource)
	{
		if (ir != null)
		{
			ir.ItemsSource = itemsSource;
		}
	}

	private void UpdateFooterRepeaterItemsSource(bool sourceCollectionReset, bool sourceCollectionChanged)
	{
		if (!m_appliedTemplate) return;

		object GetItemsSource()
		{
			var menuItemsSource = FooterMenuItemsSource;
			if (menuItemsSource != null)
			{
				return menuItemsSource;
			}
			UpdateSelectionForMenuItems();
			return FooterMenuItems;
		}
		var itemsSource = GetItemsSource();

		UpdateItemsRepeaterItemsSource(m_leftNavFooterMenuRepeater, null);
		UpdateItemsRepeaterItemsSource(m_topNavFooterMenuRepeater, null);

		if (m_settingsItem == null || sourceCollectionChanged || sourceCollectionReset)
		{
			var dataSource = new List<object>();

			if (m_settingsItem == null)
			{
				m_settingsItem = new NavigationViewItem();
				var settingsItem = m_settingsItem;
				settingsItem.Name("SettingsItem");
				m_navigationViewItemsFactory.SettingsItem(settingsItem);
			}

			if (sourceCollectionReset)
			{
				m_footerItemsCollectionChangedRevoker.Disposable = null;
				m_footerItemsSource = null;
			}

			if (m_footerItemsSource == null)
			{
				m_footerItemsSource = new InspectingDataSource(itemsSource);
				m_footerItemsSource.CollectionChanged += OnFooterItemsSourceCollectionChanged;
				m_footerItemsCollectionChangedRevoker.Disposable = Disposable.Create(() => m_footerItemsSource.CollectionChanged -= OnFooterItemsSourceCollectionChanged);
			}

			if (m_footerItemsSource != null)
			{
				var settingsItem = m_settingsItem;
				var size = m_footerItemsSource.Count;

				for (int i = 0; i < size; i++)
				{
					var item = m_footerItemsSource.GetAt(i);
					dataSource.Add(item);
				}

				if (IsSettingsVisible)
				{
					CreateAndHookEventsToSettings();
					// add settings item to the end of footer
					dataSource.Add(settingsItem);
				}
			}

			m_selectionModelSource[1] = dataSource;
		}

		if (IsTopNavigationView())
		{
			UpdateItemsRepeaterItemsSource(m_topNavFooterMenuRepeater, m_selectionModelSource[1]);
		}
		else
		{
			var repeater = m_leftNavFooterMenuRepeater;
			if (repeater != null)
			{
				UpdateItemsRepeaterItemsSource(m_leftNavFooterMenuRepeater, m_selectionModelSource[1]);

				// Footer items changed and we need to recalculate the layout.
				// However repeater "lags" behind, so we need to force it to reevaluate itself now.
				repeater.InvalidateMeasure();
				repeater.UpdateLayout();

				// Footer items changed, so let's update the pane layout.
				UpdatePaneLayout();
			}

			var settings = m_settingsItem;
			if (settings != null)
			{
				settings.StartBringIntoView();
			}
		}
	}

	private void OnFlyoutClosing(object sender, FlyoutBaseClosingEventArgs args)
	{
		// If the user selected an parent item in the overflow flyout then the item has not been moved to top primary yet.
		// So we need to move it.
		if (m_moveTopNavOverflowItemOnFlyoutClose && !m_selectionChangeFromOverflowMenu)
		{
			m_moveTopNavOverflowItemOnFlyoutClose = false;

			var selectedIndex = m_selectionModel.SelectedIndex;
			if (selectedIndex.GetSize() > 0)
			{
				var firstContainer = GetContainerForIndex(selectedIndex.GetAt(1), false /*infooter*/);
				if (firstContainer != null)
				{
					if (firstContainer is NavigationViewItem firstNVI)
					{
						// We want to collapse the top level item before we move it
						firstNVI.IsExpanded = false;
					}
				}

				SelectandMoveOverflowItem(SelectedItem, selectedIndex, false /*closeFlyout*/);
			}
		}
	}

	private void OnNavigationViewItemIsSelectedPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		if (sender is NavigationViewItem nvi)
		{
			// Check whether the container that triggered this call back is the selected container
			bool isContainerSelectedInModel = IsContainerTheSelectedItemInTheSelectionModel(nvi);
			bool isSelectedInContainer = nvi.IsSelected;

			if (isSelectedInContainer && !isContainerSelectedInModel)
			{
				var indexPath = GetIndexPathForContainer(nvi);
				UpdateSelectionModelSelection(indexPath);
			}
			else if (!isSelectedInContainer && isContainerSelectedInModel)
			{
				var indexPath = GetIndexPathForContainer(nvi);
				var indexPathFromModel = m_selectionModel.SelectedIndex;

				if (indexPathFromModel != null && indexPath.CompareTo(indexPathFromModel) == 0)
				{
					m_selectionModel.DeselectAt(indexPath);
				}
			}

			if (isSelectedInContainer)
			{
				nvi.IsChildSelected = false;
			}
		}
	}

	private void OnNavigationViewItemExpandedPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		if (sender is NavigationViewItem nvi)
		{
			if (nvi.IsExpanded)
			{
				RaiseExpandingEvent(nvi);
			}

			ShowHideChildrenItemsRepeater(nvi);

			if (!nvi.IsExpanded)
			{
				RaiseCollapsedEvent(nvi);
			}
		}
	}

	private void RaiseItemInvokedForNavigationViewItem(NavigationViewItem nvi)
	{
		object nextItem = null;
		var prevItem = SelectedItem;
		var parentIR = GetParentItemsRepeaterForContainer(nvi);

		var itemsSourceView = parentIR.ItemsSourceView;
		if (itemsSourceView != null)
		{
			var inspectingDataSource = (InspectingDataSource)(itemsSourceView);
			var itemIndex = parentIR.GetElementIndex(nvi);

			// Check that index is NOT -1, meaning it is actually realized
			if (itemIndex != -1)
			{
				// Something went wrong, item might not be realized yet.
				nextItem = inspectingDataSource.GetAt(itemIndex);
			}
		}

		NavigationRecommendedTransitionDirection GetRecommendedDirection(object prevItem, NavigationViewItem nvi, object parentIR)
		{
			if (IsTopNavigationView() && nvi.SelectsOnInvoked)
			{
				bool isInOverflow = parentIR == m_topNavRepeaterOverflowView;
				if (isInOverflow)
				{
					return NavigationRecommendedTransitionDirection.FromOverflow;
				}
				else if (prevItem != null)
				{
					return GetRecommendedTransitionDirection(NavigationViewItemBaseOrSettingsContentFromData(prevItem), nvi);
				}
			}
			return NavigationRecommendedTransitionDirection.Default;
		}
		// Determine the recommeded transition direction.
		// Any transitions other than `Default` only apply in top nav scenarios.
		var recommendedDirection = GetRecommendedDirection(prevItem, nvi, parentIR);

		RaiseItemInvoked(nextItem, IsSettingsItem(nvi) /*isSettings*/, nvi, recommendedDirection);
	}

	internal void OnNavigationViewItemInvoked(NavigationViewItem nvi)
	{
		m_shouldRaiseItemInvokedAfterSelection = true;

		var selectedItem = SelectedItem;
		bool updateSelection = m_selectionModel != null && nvi.SelectsOnInvoked;
		if (updateSelection)
		{
			var ip = GetIndexPathForContainer(nvi);

			// Determine if we will update collapse/expand which will happen iff the item has children
			if (DoesNavigationViewItemHaveChildren(nvi))
			{
				m_shouldIgnoreUIASelectionRaiseAsExpandCollapseWillRaise = true;
			}
			UpdateSelectionModelSelection(ip);
		}

		// Item was invoked but already selected, so raise event here.
		if (selectedItem == SelectedItem)
		{
			RaiseItemInvokedForNavigationViewItem(nvi);
		}

		ToggleIsExpandedNavigationViewItem(nvi);
		ClosePaneIfNeccessaryAfterItemIsClicked(nvi);

		if (updateSelection)
		{
			var indicatorTarget = nvi;

			// Move indicator to topmost collapsed parent
			var parent = GetParentNavigationViewItemForContainer(nvi);
			while (parent != null)
			{
				if (!parent.IsExpanded)
				{
					indicatorTarget = parent;
				}
				parent = GetParentNavigationViewItemForContainer(parent);
			}

			AnimateSelectionChanged(indicatorTarget);

			CloseFlyoutIfRequired(nvi);
		}
	}

	private bool IsRootItemsRepeater(DependencyObject element)
	{
		if (element != null)
		{
			return (element == m_topNavRepeater ||
				element == m_leftNavRepeater ||
				element == m_topNavRepeaterOverflowView ||
				element == m_leftNavFooterMenuRepeater ||
				element == m_topNavFooterMenuRepeater);
		}
		return false;
	}

	private bool IsRootGridOfFlyout(DependencyObject element)
	{
		if (element is Grid grid)
		{
			return grid.Name == c_flyoutRootGrid;
		}
		return false;
	}

	private ItemsRepeater GetParentRootItemsRepeaterForContainer(NavigationViewItemBase nvib)
	{
		var parentIR = GetParentItemsRepeaterForContainer(nvib);
		var currentNvib = nvib;
		while (!IsRootItemsRepeater(parentIR))
		{
			currentNvib = GetParentNavigationViewItemForContainer(currentNvib);
			if (currentNvib == null)
			{
				return null;
			}

			parentIR = GetParentItemsRepeaterForContainer(currentNvib);
		}
		return parentIR;
	}

	internal ItemsRepeater GetParentItemsRepeaterForContainer(NavigationViewItemBase nvib)
	{
		var parent = VisualTreeHelper.GetParent(nvib);
		if (parent != null)
		{
			var parentIR = parent as ItemsRepeater;
			if (parentIR != null)
			{
				return parentIR;
			}
		}
		return null;
	}

	private NavigationViewItem GetParentNavigationViewItemForContainer(NavigationViewItemBase nvib)
	{
		// TODO: This scenario does not find parent items when in a flyout, which causes problems if item if first loaded
		// straight in the flyout. Fix. This logic can be merged with the 'GetIndexPathForContainer' logic below.
		DependencyObject parent = GetParentItemsRepeaterForContainer(nvib);
		if (!IsRootItemsRepeater(parent))
		{
			while (parent != null)
			{
				parent = VisualTreeHelper.GetParent(parent);
				if (parent is NavigationViewItem nvi)
				{
					return nvi;
				}
			}
		}
		return null;
	}

	internal IndexPath GetIndexPathForContainer(NavigationViewItemBase nvib)
	{
		var path = new List<int>();
		bool isInFooterMenu = false;

		DependencyObject child = nvib;
		var parent = VisualTreeHelper.GetParent(child);
		if (parent == null)
		{
			return IndexPath.CreateFromIndices(path);
		}

		// Search through VisualTree for a root itemsrepeater
		while (parent != null && !IsRootItemsRepeater(parent) && !IsRootGridOfFlyout(parent))
		{
			if (parent is ItemsRepeater parentIR)
			{
				if (child is UIElement childElement)
				{
					path.Insert(0, parentIR.GetElementIndex(childElement));
				}
			}
			child = parent;
			parent = VisualTreeHelper.GetParent(parent);
		}

		// If the item is in a flyout, then we need to final index of its parent
		if (IsRootGridOfFlyout(parent))
		{
			var nvi = m_lastItemExpandedIntoFlyout;
			if (nvi != null)
			{
				child = nvi;
				parent = IsTopNavigationView() ? m_topNavRepeater : m_leftNavRepeater;
			}
		}

		// If item is in one of the disconnected ItemRepeaters, account for that in IndexPath calculations
		if (parent == m_topNavRepeaterOverflowView)
		{
			// Convert index of selected item in overflow to index in datasource
			var containerIndex = m_topNavRepeaterOverflowView.GetElementIndex(child as UIElement);
			var item = m_topDataProvider.GetOverflowItems()[containerIndex];
			var indexAtRoot = m_topDataProvider.IndexOf(item);
			path.Insert(0, indexAtRoot);
		}
		else if (parent == m_topNavRepeater)
		{
			// Convert index of selected item in overflow to index in datasource
			var containerIndex = m_topNavRepeater.GetElementIndex(child as UIElement);
			var item = m_topDataProvider.GetPrimaryItems()[containerIndex];
			var indexAtRoot = m_topDataProvider.IndexOf(item);
			path.Insert(0, indexAtRoot);
		}
		else if (parent is ItemsRepeater parentIR)
		{
			path.Insert(0, parentIR.GetElementIndex(child as UIElement));
		}

		isInFooterMenu = parent == m_leftNavFooterMenuRepeater || parent == m_topNavFooterMenuRepeater;

		path.Insert(0, isInFooterMenu ? c_footerMenuBlockIndex : c_mainMenuBlockIndex);

		return IndexPath.CreateFromIndices(path);
	}

	internal void OnRepeaterElementPrepared(ItemsRepeater ir, ItemsRepeaterElementPreparedEventArgs args)
	{
#if !HAS_UNO_WINUI
		// This validation is only relevant outside of the Windows build where WUXC and MUXC have distinct types.
		// Certain items are disallowed in a NavigationView's items list. Check for them.
		if (args.Element is Windows.UI.Xaml.Controls.NavigationViewItemBase)
		{
			throw new InvalidOperationException("MenuItems contains a Windows.UI.Xaml.Controls.NavigationViewItem. This control requires that the NavigationViewItems be of type Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.NavigationViewItem.");
		}
#endif

		if (args.Element is NavigationViewItemBase nvib)
		{
			var nvibImpl = nvib;
			nvibImpl.SetNavigationViewParent(this);
			nvibImpl.IsTopLevelItem = IsTopLevelItem(nvib);

			NavigationViewRepeaterPosition GetPosition(ItemsRepeater ir)
			{
				if (IsTopNavigationView())
				{
					if (ir == m_topNavRepeater)
					{
						return NavigationViewRepeaterPosition.TopPrimary;
					}
					if (ir == m_topNavFooterMenuRepeater)
					{
						return NavigationViewRepeaterPosition.TopFooter;
					}
					return NavigationViewRepeaterPosition.TopOverflow;
				}
				if (ir == m_leftNavFooterMenuRepeater)
				{
					return NavigationViewRepeaterPosition.LeftFooter;
				}
				return NavigationViewRepeaterPosition.LeftNav;
			}
			// Visual state info propagation
			var position = GetPosition(ir);
			nvibImpl.Position = position;

			var parentNVI = GetParentNavigationViewItemForContainer(nvib);
			if (parentNVI != null)
			{
				var parentNVIImpl = parentNVI;
				var itemDepth = parentNVIImpl.ShouldRepeaterShowInFlyout() ? 0 : parentNVIImpl.Depth + 1;
				nvibImpl.Depth = itemDepth;
			}

			else
			{
				nvibImpl.Depth = 0;
			}

			// Apply any custom container styling
			ApplyCustomMenuItemContainerStyling(nvib, ir, args.Index);

			if (args.Element is NavigationViewItem nvi)
			{
				int GetChildDepth(NavigationViewRepeaterPosition position, NavigationViewItemBase nvibImpl)
				{
					if (position == NavigationViewRepeaterPosition.TopPrimary)
					{
						return 0;
					}
					return nvibImpl.Depth + 1;

				}

				// Propagate depth to children items if they exist
				var childDepth = GetChildDepth(position, nvibImpl);
				nvi.PropagateDepthToChildren(childDepth);

				SetNavigationViewItemRevokers(nvi);

				var item = MenuItemFromContainer(nvi);

				if (SelectedItem == item && IsVisible(nvi))
				{
					if (m_isSelectionChangedPending && m_pendingSelectionChangedItem is not null)
					{
						MUX_ASSERT(m_pendingSelectionChangedItem == item);
					}

					nvi.LayoutUpdated += OnSelectedItemLayoutUpdated;
					m_selectedItemLayoutUpdatedRevoker.Disposable = Disposable.Create(() => nvi.LayoutUpdated -= OnSelectedItemLayoutUpdated);
				}

#if IS_UNO
				// TODO: Uno specific - remove when #4689 is fixed
				// This ensures the item is properly initialized and the selected item is displayed
				nvibImpl.Reinitialize();
				if (SelectedItem != null && m_activeIndicator == null)
				{
					AnimateSelectionChanged(SelectedItem);
				}
#endif
			}
		}
	}

	private void ApplyCustomMenuItemContainerStyling(NavigationViewItemBase nvib, ItemsRepeater ir, int index)
	{
		var menuItemContainerStyle = MenuItemContainerStyle;
		var menuItemContainerStyleSelector = MenuItemContainerStyleSelector;
		if (menuItemContainerStyle != null)
		{
			nvib.Style = menuItemContainerStyle;
		}
		else if (menuItemContainerStyleSelector != null)
		{
			var itemsSourceView = ir.ItemsSourceView;
			if (itemsSourceView != null)
			{
				var item = itemsSourceView.GetAt(index);
				if (item != null)
				{
					var selectedStyle = menuItemContainerStyleSelector.SelectStyle(item, nvib);
					if (selectedStyle != null)
					{
						nvib.Style = selectedStyle;
					}
				}
			}
		}
	}

	internal void OnRepeaterElementClearing(ItemsRepeater ir, ItemsRepeaterElementClearingEventArgs args)
	{
		if (args.Element is NavigationViewItemBase nvib)
		{
			var nvibImpl = nvib;
			nvibImpl.Depth = 0;
			nvibImpl.IsTopLevelItem = false;
			if (nvib is NavigationViewItem nvi)
			{
				// Revoke all the events that we were listing to on the item
				ClearNavigationViewItemRevokers(nvi);
			}
		}
	}

	// Hook up the Settings Item Invoked event listener
	private void CreateAndHookEventsToSettings()
	{
		if (m_settingsItem == null)
		{
			return;
		}

		var settingsItem = m_settingsItem;
		var settingsIcon = new AnimatedIcon();
		settingsIcon.Source = new AnimatedSettingsVisualSource();
		var settingsFallbackIcon = new SymbolIconSource();
		settingsFallbackIcon.Symbol = Symbol.Setting;
		settingsIcon.FallbackIconSource = settingsFallbackIcon;
		settingsItem.Icon = settingsIcon;

		// Do localization for settings item label and Automation Name
		var localizedSettingsName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_SettingsButtonName);
		AutomationProperties.SetName(settingsItem, localizedSettingsName);
		settingsItem.Tag = c_settingsItemTag;
		UpdateSettingsItemToolTip();

		// Add the name only in case of horizontal nav
		if (!IsTopNavigationView())
		{
			settingsItem.Content = localizedSettingsName;
		}
		else
		{
			settingsItem.Content = null;
		}

		// hook up SettingsItem
		SetValue(SettingsItemProperty, settingsItem);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		if (IsTopNavigationView() && IsTopPrimaryListVisible())
		{
			if (availableSize.Width == double.PositiveInfinity)
			{
				// We have infinite space, so move all items to primary list
				m_topDataProvider.MoveAllItemsToPrimaryList();
			}
			else
			{
				HandleTopNavigationMeasureOverride(availableSize);
#if DEBUG
				if (m_topDataProvider.Size() > 0)
				{
					// We should always have at least one item in primary.
					MUX_ASSERT(m_topDataProvider.GetPrimaryItems().Count > 0);
				}
#endif // DEBUG
			}
		}

		m_layoutUpdatedToken.Disposable = null;
		LayoutUpdated += OnLayoutUpdated;
		m_layoutUpdatedToken.Disposable = Disposable.Create(() => LayoutUpdated -= OnLayoutUpdated);

		return base.MeasureOverride(availableSize);
	}

	private void OnLayoutUpdated(object sender, object e)
	{
		// We only need to handle once after MeasureOverride, so revoke the token.
		m_layoutUpdatedToken.Disposable = null;

		// In topnav, when an item in overflow menu is clicked, the animation is delayed because that item is not move to primary list yet.
		// And it depends on LayoutUpdated to re-play the animation. m_lastSelectedItemPendingAnimationInTopNav is the last selected overflow item.
		var lastSelectedItemInTopNav = m_lastSelectedItemPendingAnimationInTopNav;
		if (lastSelectedItemInTopNav != null)
		{
			m_lastSelectedItemPendingAnimationInTopNav = null;
			AnimateSelectionChanged(lastSelectedItemInTopNav);
		}

		if (m_OrientationChangedPendingAnimation)
		{
			m_OrientationChangedPendingAnimation = false;
			AnimateSelectionChanged(SelectedItem);
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		var width = args.NewSize.Width;
		UpdateOpenPaneLength(width);
		UpdateAdaptiveLayout(width);
		UpdateTitleBarPadding();
		UpdateBackAndCloseButtonsVisibility();
		UpdatePaneLayout();
	}

	private void UpdateOpenPaneLength(double width)
	{
		if (!IsTopNavigationView() && m_rootSplitView != null)
		{
			m_openPaneLength = Math.Max(0.0, Math.Min(width, OpenPaneLength));

			var templateSettings = GetTemplateSettings();
			templateSettings.OpenPaneLength = m_openPaneLength;
		}
	}

	private void OnItemsContainerSizeChanged(object sender, SizeChangedEventArgs args)
	{
		UpdatePaneLayout();
	}

	// forceSetDisplayMode: On first call to SetDisplayMode, force setting to initial values
	private void UpdateAdaptiveLayout(double width, bool forceSetDisplayMode = false)
	{
		// In top nav, there is no adaptive pane layout
		if (IsTopNavigationView())
		{
			return;
		}

		if (m_rootSplitView == null)
		{
			return;
		}

		// If we decide we want it to animate open/closed when you resize the
		// window we'll have to change how we figure out the initial state
		// instead of this:
		m_initialListSizeStateSet = false; // see UpdateIsClosedCompact()

		NavigationViewDisplayMode displayMode = NavigationViewDisplayMode.Compact;

		var paneDisplayMode = PaneDisplayMode;
		if (paneDisplayMode == NavigationViewPaneDisplayMode.Auto)
		{
			if (width >= ExpandedModeThresholdWidth)
			{
				displayMode = NavigationViewDisplayMode.Expanded;
			}
			else if (width > 0 && width < CompactModeThresholdWidth)
			{
				displayMode = NavigationViewDisplayMode.Minimal;
			}
		}
		else if (paneDisplayMode == NavigationViewPaneDisplayMode.Left)
		{
			displayMode = NavigationViewDisplayMode.Expanded;
		}
		else if (paneDisplayMode == NavigationViewPaneDisplayMode.LeftCompact)
		{
			displayMode = NavigationViewDisplayMode.Compact;
		}
		else if (paneDisplayMode == NavigationViewPaneDisplayMode.LeftMinimal)
		{
			displayMode = NavigationViewDisplayMode.Minimal;
		}
		else
		{
			throw new InvalidOperationException("Invalid pane display mode");
		}

		if (!forceSetDisplayMode && m_InitialNonForcedModeUpdate)
		{
			if (displayMode == NavigationViewDisplayMode.Minimal ||
				displayMode == NavigationViewDisplayMode.Compact)
			{
				ClosePane();
			}
			m_InitialNonForcedModeUpdate = false;
		}

		var previousMode = DisplayMode;
		SetDisplayMode(displayMode, forceSetDisplayMode);

		if (displayMode == NavigationViewDisplayMode.Expanded && IsPaneVisible)
		{
			if (!m_wasForceClosed)
			{
				OpenPane();
			}
		}

		if (previousMode == NavigationViewDisplayMode.Expanded
			&& displayMode == NavigationViewDisplayMode.Compact)
		{
			m_initialListSizeStateSet = false;
			ClosePane();
		}

		if (displayMode == NavigationViewDisplayMode.Minimal)
		{
			ClosePane();
		}
	}

	private void UpdatePaneLayout()
	{
		if (!IsTopNavigationView())
		{
			double GetTotalAvailableHeight()
			{
				var paneContentRow = m_itemsContainerRow;
				if (paneContentRow != null)
				{
					double GetItemsContainerMargin()
					{
						if (m_itemsContainer is { } itemsContainer)
						{
							var margin = itemsContainer.Margin;
							return margin.Top + margin.Bottom;
						}
						return 0.0;
					}
					var itemsContainerMargin = GetItemsContainerMargin();
					return paneContentRow.ActualHeight - itemsContainerMargin;
				}
				return 0.0;
			}

			var totalAvailableHeight = GetTotalAvailableHeight();

			// Only continue if we have a positive amount of space to manage.
			if (totalAvailableHeight > 0)
			{
				// We need this value more than twice, so cache it.
				var totalAvailableHeightHalf = totalAvailableHeight / 2;

				double GetHeightForMenuItems(double totalAvailableHeight, double totalAvailableHeightHalf)
				{
					var footerItemsScrollViewer = m_footerItemsScrollViewer;
					if (footerItemsScrollViewer != null)
					{
						var footerItemsRepeater = m_leftNavFooterMenuRepeater;
						if (footerItemsRepeater != null)
						{
							// We know the actual height of footer items, so use that to determine how to split pane.
							var menuItems = m_leftNavRepeater;
							if (menuItems != null)
							{
								double GetFootersActualHeight(ItemsRepeater footerItemsRepeater)
								{
									double footerItemsRepeaterTopBottomMargin = 0.0;
									if (footerItemsRepeater.Visibility == Visibility.Visible)
									{
										var footerItemsRepeaterMargin = footerItemsRepeater.Margin;
										footerItemsRepeaterTopBottomMargin = footerItemsRepeaterMargin.Top + footerItemsRepeaterMargin.Bottom;
									}
#if __IOS__ // Uno workaround: The arrange is async on iOS, ActualHeight is not set yet. This would constraints the footer to MaxHeight 0.
									return footerItemsRepeater.DesiredSize.Height + footerItemsRepeaterTopBottomMargin;
#else
									return footerItemsRepeater.ActualHeight + footerItemsRepeaterTopBottomMargin;
#endif
								}
								var footersActualHeight = GetFootersActualHeight(footerItemsRepeater);

								double GetPaneFooterActualHeight()
								{
									if (m_leftNavFooterContentBorder is { } paneFooter)
									{
										double paneFooterTopBottomMargin = 0.0;
										if (paneFooter.Visibility == Visibility.Visible)
										{
											var paneFooterMargin = paneFooter.Margin;
											paneFooterTopBottomMargin = paneFooterMargin.Top + paneFooterMargin.Bottom;
										}
#if __IOS__ // Uno workaround: The arrange is async on iOS, ActualHeight is not set yet. This would constraints the footer to MaxHeight 0.
										return paneFooter.DesiredSize.Height + paneFooterTopBottomMargin;
#else
										return paneFooter.ActualHeight + paneFooterTopBottomMargin;
#endif
									}
									return 0.0;
								}
								var paneFooterActualHeight = GetPaneFooterActualHeight();

								// This is the value computed during the measure pass of the layout process. This will be the value used to determine
								// the partition logic between menuItems and footerGroup, since the ActualHeight may be taller if there's more space.
								var menuItemsDesiredHeight = menuItems.DesiredSize.Height;

								// This is what the height ended up being, so will be the value that is used to calculate the partition
								// between menuItems and footerGroup.
								double GetMenuItemsActualHeight(ItemsRepeater menuItems)
								{
									double menuItemsTopBottomMargin = 0.0;
									if (menuItems.Visibility == Visibility.Visible)
									{
										var menuItemsMargin = menuItems.Margin;
										menuItemsTopBottomMargin = menuItemsMargin.Top + menuItemsMargin.Bottom;
									}
#if __IOS__ // Uno workaround: The arrange is async on iOS, ActualHeight is not set yet. This would constraints the footer to MaxHeight 0.
									return menuItems.DesiredSize.Height + menuItemsTopBottomMargin;
#else
									return menuItems.ActualHeight + menuItemsTopBottomMargin;
#endif
								}
								var menuItemsActualHeight = GetMenuItemsActualHeight(menuItems);

								// Footer and PaneFooter are included in the footerGroup to calculate available height for menu items.
								var footerGroupActualHeight = footersActualHeight + paneFooterActualHeight;

								if (m_footerItemsSource.Count == 0 && !IsSettingsVisible)
								{
									VisualStateManager.GoToState(this, c_separatorCollapsedStateName, false);
									return totalAvailableHeight;
								}
								else if (m_menuItemsSource.Count == 0)
								{
									footerItemsScrollViewer.MaxHeight = totalAvailableHeight;
									VisualStateManager.GoToState(this, c_separatorCollapsedStateName, false);
									return 0.0;
								}
								else if (totalAvailableHeight >= menuItemsDesiredHeight + footersActualHeight)
								{
									// We have enough space for two so let everyone get as much as they need.
									footerItemsScrollViewer.MaxHeight = footersActualHeight;
									VisualStateManager.GoToState(this, c_separatorCollapsedStateName, false);
									return totalAvailableHeight - footerGroupActualHeight;
								}
								else if (menuItemsDesiredHeight <= totalAvailableHeightHalf)
								{
									// Footer items exceed over the half, so let's limit them.
									footerItemsScrollViewer.MaxHeight = totalAvailableHeight - menuItemsActualHeight;
									VisualStateManager.GoToState(this, c_separatorVisibleStateName, false);
									return menuItemsActualHeight;
								}
								else if (footerGroupActualHeight <= totalAvailableHeightHalf)
								{
									// Menu items exceed over the half, so let's limit them.
									footerItemsScrollViewer.MaxHeight = footersActualHeight;
									VisualStateManager.GoToState(this, c_separatorVisibleStateName, false);
									return totalAvailableHeight - footerGroupActualHeight;
								}
								else
								{
									// Both are more than half the height, so split evenly.
									footerItemsScrollViewer.MaxHeight = totalAvailableHeightHalf;
									VisualStateManager.GoToState(this, c_separatorVisibleStateName, false);
									return totalAvailableHeightHalf;
								}
							}
							else
							{
								// Couldn't determine the menuItems.
								// Let's just take all the height and let the other repeater deal with it.
								return totalAvailableHeight - footerItemsRepeater.ActualHeight;
							}
						}
						// We have no idea how much space to occupy as we are not able to get the size of the footer repeater.
						// Stick with 50% as backup.
						footerItemsScrollViewer.MaxHeight = totalAvailableHeightHalf;
					}
					// We couldn't find a good strategy, so limit to 50% percent for the menu items.
					return totalAvailableHeightHalf;
				}
				var heightForMenuItems = GetHeightForMenuItems(totalAvailableHeight, totalAvailableHeightHalf);
				// Footer items should have precedence as that usually contains very
				// important items such as settings or the profile.

				var menuItemsScrollViewer = m_menuItemsScrollViewer;
				if (menuItemsScrollViewer != null)
				{
					// Update max height for menu items.
					menuItemsScrollViewer.MaxHeight = heightForMenuItems;
				}
			}
		}
	}

	private void OnPaneToggleButtonClick(object sender, RoutedEventArgs args)
	{
		if (IsPaneOpen)
		{
			m_wasForceClosed = true;
			ClosePane();
		}
		else
		{
			m_wasForceClosed = false;
			OpenPane();
		}
	}

	private void OnPaneSearchButtonClick(object sender, RoutedEventArgs args)
	{
		m_wasForceClosed = false;
		OpenPane();

		var autoSuggestBox = AutoSuggestBox;
		if (autoSuggestBox != null)
		{
			autoSuggestBox.Focus(FocusState.Keyboard);
		}
	}

	private void OnPaneTitleHolderSizeChanged(object sender, SizeChangedEventArgs args)
	{
		UpdateBackAndCloseButtonsVisibility();
	}

	private void OpenPane()
	{
		try
		{
			m_isOpenPaneForInteraction = true;
			IsPaneOpen = true;
		}
		finally
		{
			m_isOpenPaneForInteraction = false;
		}
	}


	// Call this when you want an uncancellable close
	private void ClosePane()
	{
		CollapseMenuItemsInRepeater(m_leftNavRepeater);
		try
		{
			m_isOpenPaneForInteraction = true;
			IsPaneOpen = false; // the SplitView is two-way bound to this value
		}
		finally
		{
			m_isOpenPaneForInteraction = false;
		}
	}

	// Call this when NavigationView itself is going to trigger a close
	// where you will stop the close if the cancel is triggered
	private bool AttemptClosePaneLightly()
	{
		bool pendingPaneClosingCancel = false;

		if (SharedHelpers.IsRS3OrHigher())
		{
			var eventArgs = new NavigationViewPaneClosingEventArgs();
			PaneClosing?.Invoke(this, eventArgs);
			pendingPaneClosingCancel = eventArgs.Cancel;
		}

		if (!pendingPaneClosingCancel || m_wasForceClosed)
		{
			m_blockNextClosingEvent = true;
			ClosePane();
			return true;
		}

		return false;
	}

	private void OnSplitViewClosedCompactChanged(DependencyObject sender, DependencyProperty args)
	{
		if (args == SplitView.IsPaneOpenProperty ||
			args == SplitView.DisplayModeProperty)
		{
			UpdateIsClosedCompact();
		}
	}

	private void OnSplitViewPaneClosed(DependencyObject sender, object obj)
	{
		PaneClosed?.Invoke(this, null);
	}

	private void OnSplitViewPaneClosing(DependencyObject sender, SplitViewPaneClosingEventArgs args)
	{
		bool pendingPaneClosingCancel = false;
		if (PaneClosing != null)
		{
			if (!m_blockNextClosingEvent) // If this is true, we already sent one out "manually" and don't need to forward SplitView's event
			{
				var eventArgs = new NavigationViewPaneClosingEventArgs();
				eventArgs.SplitViewClosingArgs(args);
				PaneClosing?.Invoke(this, eventArgs);
				pendingPaneClosingCancel = eventArgs.Cancel;
			}
			else
			{
				m_blockNextClosingEvent = false;
			}
		}

		if (!pendingPaneClosingCancel) // will be set in above event!
		{
			var splitView = m_rootSplitView;
			if (splitView != null)
			{
				var paneList = m_leftNavRepeater;
				if (paneList != null)
				{
					if (splitView.DisplayMode == SplitViewDisplayMode.CompactOverlay || splitView.DisplayMode == SplitViewDisplayMode.CompactInline)
					{
						// See UpdateIsClosedCompact 'RS3+ animation timing enhancement' for explanation:
						VisualStateManager.GoToState(this, "ListSizeCompact", true /*useTransitions*/);
						UpdatePaneToggleSize();
					}
				}
			}
		}
	}

	private void OnSplitViewPaneOpened(DependencyObject sender, object obj)
	{
		PaneOpened?.Invoke(this, null);
	}

	private void OnSplitViewPaneOpening(DependencyObject sender, object obj)
	{
		if (m_leftNavRepeater != null)
		{
			// See UpdateIsClosedCompact 'RS3+ animation timing enhancement' for explanation:
			VisualStateManager.GoToState(this, "ListSizeFull", true /*useTransitions*/);
		}

		PaneOpening?.Invoke(this, null);
	}

	private void UpdateIsClosedCompact()
	{
		var splitView = m_rootSplitView;
		if (splitView != null)
		{
			// Check if the pane is closed and if the splitview is in either compact mode.
			var splitViewDisplayMode = splitView.DisplayMode;
			m_isClosedCompact = !splitView.IsPaneOpen && (splitViewDisplayMode == SplitViewDisplayMode.CompactOverlay || splitViewDisplayMode == SplitViewDisplayMode.CompactInline);
			VisualStateManager.GoToState(this, m_isClosedCompact ? "ClosedCompact" : "NotClosedCompact", true /*useTransitions*/);

			// Set the initial state of the list size
			if (!m_initialListSizeStateSet)
			{
				m_initialListSizeStateSet = true;
				VisualStateManager.GoToState(this, m_isClosedCompact ? "ListSizeCompact" : "ListSizeFull", true /*useTransitions*/);
			}
			else if (!SharedHelpers.IsRS3OrHigher()) // Do any changes that would otherwise happen on opening/closing for RS2 and earlier:
			{
				// RS3+ animation timing enhancement:
				// Pre-RS3, we didn't have the full suite of Closed, Closing, Opened,
				// Opening events on SplitView. So when doing open/closed operations,
				// we have to do them immediately. Just one example: on RS2 when you
				// close the pane, the PaneTitle will disappear *immediately* which
				// looks janky. But on RS4, it'll have its visibility set after the
				// closed event fires.
				VisualStateManager.GoToState(this, m_isClosedCompact ? "ListSizeCompact" : "ListSizeFull", true /*useTransitions*/);
			}

			UpdateTitleBarPadding();
			UpdateBackAndCloseButtonsVisibility();
			UpdatePaneTitleMargins();
			UpdatePaneToggleSize();
		}
	}

	private void UpdatePaneButtonsWidths()
	{
		var templateSettings = GetTemplateSettings();

		double GetNewButtonWidths()
		{
			return CompactPaneLength;
		}
		var newButtonWidths = GetNewButtonWidths();

		templateSettings.PaneToggleButtonWidth = newButtonWidths;
		templateSettings.SmallerPaneToggleButtonWidth = Math.Max(0.0, newButtonWidths - 8);
	}

	private void OnBackButtonClicked(object sender, RoutedEventArgs args)
	{
		var eventArgs = new NavigationViewBackRequestedEventArgs();
		BackRequested?.Invoke(this, eventArgs);
	}

	private bool IsOverlay()
	{
		var splitView = m_rootSplitView;
		if (splitView != null)
		{
			return splitView.DisplayMode == SplitViewDisplayMode.Overlay;
		}
		else
		{
			return false;
		}
	}

	private bool IsLightDismissible()
	{
		var splitView = m_rootSplitView;
		if (splitView != null)
		{
			return splitView.DisplayMode != SplitViewDisplayMode.Inline && splitView.DisplayMode != SplitViewDisplayMode.CompactInline;
		}

		else
		{
			return false;
		}
	}

	private bool ShouldShowBackButton()
	{
		if (m_backButton != null && !ShouldPreserveNavigationViewRS3Behavior())
		{
			if (DisplayMode == NavigationViewDisplayMode.Minimal && IsPaneOpen)
			{
				return false;
			}

			return ShouldShowBackOrCloseButton();
		}

		return false;
	}

	private bool ShouldShowCloseButton()
	{
		if (m_backButton != null && !ShouldPreserveNavigationViewRS3Behavior() && m_closeButton != null)
		{
			if (!IsPaneOpen)
			{
				return false;
			}

			var paneDisplayMode = PaneDisplayMode;

			if (paneDisplayMode != NavigationViewPaneDisplayMode.LeftMinimal &&
				(paneDisplayMode != NavigationViewPaneDisplayMode.Auto || DisplayMode != NavigationViewDisplayMode.Minimal))
			{
				return false;
			}

			return ShouldShowBackOrCloseButton();
		}

		return false;
	}

	private bool ShouldShowBackOrCloseButton()
	{
		var visibility = IsBackButtonVisible;
		// Uno specific: When Auto, we hide the back button on Android as per the Android
		// design guidelines - see first paragraph of https://developer.android.com/guide/navigation/navigation-custom-back
		bool isAndroid = AnalyticsInfo.VersionInfo.DeviceFamily.StartsWith("Android", StringComparison.InvariantCultureIgnoreCase);
		return (visibility == NavigationViewBackButtonVisible.Visible || (visibility == NavigationViewBackButtonVisible.Auto && (!SharedHelpers.IsOnXbox() && !isAndroid)));
	}

	// The automation name and tooltip for the pane toggle button changes depending on whether it is open or closed
	// put the logic here as it will be called in a couple places
	private void SetPaneToggleButtonAutomationName()
	{
		string navigationName;
		if (IsPaneOpen)
		{
			navigationName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationButtonOpenName);
		}
		else
		{
			navigationName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationButtonClosedName);
		}

		var paneToggleButton = m_paneToggleButton;
		if (paneToggleButton != null)
		{
			AutomationProperties.SetName(paneToggleButton, navigationName);
			var toolTip = new ToolTip();
			toolTip.Content = navigationName;
			ToolTipService.SetToolTip(paneToggleButton, toolTip);
		}
	}

	private void UpdateSettingsItemToolTip()
	{
		var settingsItem = m_settingsItem;
		if (settingsItem != null)
		{
			if (!IsTopNavigationView() && IsPaneOpen)
			{
				ToolTipService.SetToolTip(settingsItem, null);
			}
			else
			{
				var localizedSettingsName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_SettingsButtonName);
				var toolTip = new ToolTip();
				toolTip.Content = localizedSettingsName;
				ToolTipService.SetToolTip(settingsItem, toolTip);
			}
		}
	}

	// Updates the PaneTitleHolder.Visibility and PaneTitleTextBlock.Parent properties based on the PaneDisplayMode, PaneTitle and IsPaneToggleButtonVisible properties.
	private void UpdatePaneTitleFrameworkElementParents()
	{
		var paneTitleHolderFrameworkElement = m_paneTitleHolderFrameworkElement;
		if (paneTitleHolderFrameworkElement != null)
		{
			var isPaneToggleButtonVisible = IsPaneToggleButtonVisible;
			var isTopNavigationView = IsTopNavigationView();
			var paneTitleSize = PaneTitle?.Length ?? 0;

			m_isLeftPaneTitleEmpty = (isPaneToggleButtonVisible ||
				isTopNavigationView ||
				paneTitleSize == 0 ||
				(PaneDisplayMode == NavigationViewPaneDisplayMode.LeftMinimal && !IsPaneOpen));

			paneTitleHolderFrameworkElement.Visibility = m_isLeftPaneTitleEmpty ? Visibility.Collapsed : Visibility.Visible;

			if (m_paneTitleFrameworkElement is { } paneTitleFrameworkElement)
			{
				var paneTitleTopPane = m_paneTitleOnTopPane;

				var first = SetPaneTitleFrameworkElementParent(m_paneToggleButton, paneTitleFrameworkElement, isTopNavigationView || !isPaneToggleButtonVisible);
				var second = SetPaneTitleFrameworkElementParent(m_paneTitlePresenter, paneTitleFrameworkElement, isTopNavigationView || isPaneToggleButtonVisible);
				var third = SetPaneTitleFrameworkElementParent(paneTitleTopPane, paneTitleFrameworkElement, !isTopNavigationView || isPaneToggleButtonVisible);
				if (first != null)
				{
					first();
				}
				else if (second != null)
				{
					second();
				}
				else if (third != null)
				{
					third();
				}

				if (paneTitleTopPane != null)
				{
					paneTitleTopPane.Visibility = third != null && paneTitleSize != 0 ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		}
	}

	private Action SetPaneTitleFrameworkElementParent(ContentControl parent, FrameworkElement paneTitle, bool shouldNotContainPaneTitle)
	{
		if (parent != null)
		{
			if ((parent.Content == paneTitle) == shouldNotContainPaneTitle)
			{
				if (shouldNotContainPaneTitle)
				{
					parent.Content = null;
				}
				else
				{
					return () => { parent.Content = paneTitle; };
				}
			}
		}
		return null;
	}

#if !HAS_UNO
	private readonly Vector2 c_frame1point1 = new Vector2(0.9f, 0.1f);
	private readonly Vector2 c_frame1point2 = new Vector2(1.0f, 0.2f);
	private readonly Vector2 c_frame2point1 = new Vector2(0.1f, 0.9f);
	private readonly Vector2 c_frame2point2 = new Vector2(0.2f, 1.0f);
#endif

	private void AnimateSelectionChangedToItem(object selectedItem)
	{
		if (selectedItem != null && !IsSelectionSuppressed(selectedItem))
		{
			AnimateSelectionChanged(selectedItem);
		}
	}

	// Please clear the field m_lastSelectedItemPendingAnimationInTopNav when calling this method to prevent garbage value and incorrect animation
	// when the layout is invalidated as it's called in OnLayoutUpdated.
	private void AnimateSelectionChanged(object nextItem)
	{
		// If we are delaying animation due to item movement in top nav overflow, dont do anything
		if (m_lastSelectedItemPendingAnimationInTopNav != null)
		{
			return;
		}

		UIElement prevIndicator = m_activeIndicator;
		UIElement nextIndicator = FindSelectionIndicator(nextItem);

		bool haveValidAnimation = false;
		// It's possible that AnimateSelectionChanged is called multiple times before the first animation is complete.
		// To have better user experience, if the selected target is the same, keep the first animation
		// If the selected target is not the same, abort the first animation and launch another animation.
		if (m_prevIndicator != null || m_nextIndicator != null) // There is ongoing animation
		{
			if (nextIndicator != null && m_nextIndicator == nextIndicator) // animate to the same target, just wait for animation complete
			{
				if (prevIndicator != null && prevIndicator != m_prevIndicator)
				{
					ResetElementAnimationProperties(prevIndicator, 0.0f);
				}
				haveValidAnimation = true;
			}
			else
			{
				// If the last animation is still playing, force it to complete.
				OnAnimationComplete(null, null);
			}
		}

		if (!haveValidAnimation)
		{
			UIElement paneContentGrid = m_paneContentGrid;

			if ((prevIndicator != nextIndicator) && paneContentGrid != null && prevIndicator != null && nextIndicator != null && SharedHelpers.IsAnimationsEnabled())
			{
				// Make sure both indicators are visible and in their original locations
				ResetElementAnimationProperties(prevIndicator, 1.0f);
				ResetElementAnimationProperties(nextIndicator, 1.0f);

				// get the item positions in the pane
				Point point = new Point(0, 0);
				double prevPos;
				double nextPos;

				Point prevPosPoint = prevIndicator.TransformToVisual(paneContentGrid).TransformPoint(point);
				Point nextPosPoint = nextIndicator.TransformToVisual(paneContentGrid).TransformPoint(point);
				Size prevSize = prevIndicator.RenderSize;
				Size nextSize = nextIndicator.RenderSize;

				bool areElementsAtSameDepth = false;
				if (IsTopNavigationView())
				{
					prevPos = prevPosPoint.X;
					nextPos = nextPosPoint.X;
					areElementsAtSameDepth = prevPosPoint.Y == nextPosPoint.Y;
				}
				else
				{
					prevPos = prevPosPoint.Y;
					nextPos = nextPosPoint.Y;
					areElementsAtSameDepth = prevPosPoint.X == nextPosPoint.X;
				}

#if !IS_UNO // disable animations for now
				Visual visual = ElementCompositionPreview.GetElementVisual(this);
				CompositionScopedBatch scopedBatch = visual.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

				if (!areElementsAtSameDepth)
				{
					bool isNextBelow = prevPosPoint.Y < nextPosPoint.Y;
					if (prevIndicator.RenderSize.Height > prevIndicator.RenderSize.Width)
					{
						PlayIndicatorNonSameLevelAnimations(prevIndicator, true, isNextBelow ? false : true);
					}
					else
					{
						PlayIndicatorNonSameLevelTopPrimaryAnimation(prevIndicator, true);
					}

					if (nextIndicator.RenderSize.Height > nextIndicator.RenderSize.Width)
					{
						PlayIndicatorNonSameLevelAnimations(nextIndicator, false, isNextBelow ? true : false);
					}
					else
					{
						PlayIndicatorNonSameLevelTopPrimaryAnimation(nextIndicator, false);
					}
				}
				else
				{

					float outgoingEndPosition = (float)(nextPos - prevPos);
					float incomingStartPosition = (float)(prevPos - nextPos);

					// Play the animation on both the previous and next indicators
					PlayIndicatorAnimations(prevIndicator,
						0,
						outgoingEndPosition,
						prevSize,
						nextSize,
						true);
					PlayIndicatorAnimations(nextIndicator,
						incomingStartPosition,
						0,
						prevSize,
						nextSize,
						false);
				}

				scopedBatch.End();
#endif
				m_prevIndicator = prevIndicator;
				m_nextIndicator = nextIndicator;

#if !IS_UNO // disable animations for now
				void OnCompleted(object sender, CompositionBatchCompletedEventArgs args)
				{
					this.OnAnimationComplete(sender, args);
					scopedBatch.Completed -= OnCompleted;
				}
				scopedBatch.Completed += OnCompleted;
#else
				OnAnimationComplete(null, null);
#endif
			}
			else if (prevIndicator != nextIndicator)
			{
				// if all else fails, or if animations are turned off, attempt to correctly set the positions and opacities of the indicators.
				ResetElementAnimationProperties(prevIndicator, 0.0f);
				ResetElementAnimationProperties(nextIndicator, 1.0f);
			}

			m_activeIndicator = nextIndicator;
		}
	}

#if !IS_UNO
	private void PlayIndicatorNonSameLevelAnimations(UIElement indicator, bool isOutgoing, bool fromTop)
	{
		Visual visual = ElementCompositionPreview.GetElementVisual(indicator);
		Compositor comp = visual.Compositor;

		// Determine scaling of indicator (whether it is appearing or dissapearing)
		float beginScale = isOutgoing ? 1.0f : 0.0f;
		float endScale = isOutgoing ? 0.0f : 1.0f;
		ScalarKeyFrameAnimation scaleAnim = comp.CreateScalarKeyFrameAnimation();
		scaleAnim.InsertKeyFrame(0.0f, beginScale);
		scaleAnim.InsertKeyFrame(1.0f, endScale);
		scaleAnim.Duration = TimeSpan.FromMilliseconds(600);

		// Determine where the indicator is animating from/to
		Size size = indicator.RenderSize;
		double dimension = IsTopNavigationView() ? size.Width : size.Height;
		double newCenter = fromTop ? 0.0f : dimension;
		var indicatorCenterPoint = visual.CenterPoint;
		indicatorCenterPoint.Y = (float)newCenter;
		visual.CenterPoint = indicatorCenterPoint;

		visual.StartAnimation("Scale.Y", scaleAnim);
	}

	private void PlayIndicatorNonSameLevelTopPrimaryAnimation(UIElement indicator, bool isOutgoing)
	{
		Visual visual = ElementCompositionPreview.GetElementVisual(indicator);
		Compositor comp = visual.Compositor;

		// Determine scaling of indicator (whether it is appearing or dissapearing)
		float beginScale = isOutgoing ? 1.0f : 0.0f;
		float endScale = isOutgoing ? 0.0f : 1.0f;
		ScalarKeyFrameAnimation scaleAnim = comp.CreateScalarKeyFrameAnimation();
		scaleAnim.InsertKeyFrame(0.0f, beginScale);
		scaleAnim.InsertKeyFrame(1.0f, endScale);
		scaleAnim.Duration = TimeSpan.FromMilliseconds(600);

		// Determine where the indicator is animating from/to
		Size size = indicator.RenderSize;
		double newCenter = size.Width / 2;
		var indicatorCenterPoint = visual.CenterPoint;
		indicatorCenterPoint.Y = (float)newCenter;
		visual.CenterPoint = indicatorCenterPoint;

		visual.StartAnimation("Scale.X", scaleAnim);
	}
	private void PlayIndicatorAnimations(UIElement indicator, float from, float to, Size beginSize, Size endSize, bool isOutgoing)
	{
		Visual visual = ElementCompositionPreview.GetElementVisual(indicator);
		Compositor comp = visual.Compositor;

		Size size = indicator.RenderSize;
		float dimension = IsTopNavigationView() ? (float)size.Width : (float)size.Height;

		float beginScale = 1.0f;
		float endScale = 1.0f;
		if (IsTopNavigationView() && Math.Abs(size.Width) > 0.001f)
		{
			beginScale = (float)(beginSize.Width / size.Width);
			endScale = (float)(endSize.Width / size.Width);
		}

		StepEasingFunction singleStep = comp.CreateStepEasingFunction();
		singleStep.IsFinalStepSingleFrame = true;

		if (isOutgoing)
		{
			// fade the outgoing indicator so it looks nice when animating over the scroll area
			ScalarKeyFrameAnimation opacityAnim = comp.CreateScalarKeyFrameAnimation();
			opacityAnim.InsertKeyFrame(0.0f, 1.0f);
			opacityAnim.InsertKeyFrame(0.333f, 1.0f, singleStep);
			opacityAnim.InsertKeyFrame(1.0f, 0.0f, comp.CreateCubicBezierEasingFunction(c_frame2point1, c_frame2point2));
			opacityAnim.Duration = TimeSpan.FromMilliseconds(600);

			visual.StartAnimation("Opacity", opacityAnim);
		}

		ScalarKeyFrameAnimation posAnim = comp.CreateScalarKeyFrameAnimation();
		posAnim.InsertKeyFrame(0.0f, from < to ? from : (from + (dimension * (beginScale - 1))));
		posAnim.InsertKeyFrame(0.333f, from < to ? (to + (dimension * (endScale - 1))) : to, singleStep);
		posAnim.Duration = TimeSpan.FromMilliseconds(600);

		ScalarKeyFrameAnimation scaleAnim = comp.CreateScalarKeyFrameAnimation();
		scaleAnim.InsertKeyFrame(0.0f, beginScale);
		scaleAnim.InsertKeyFrame(0.333f, Math.Abs(to - from) / dimension + (from < to ? endScale : beginScale), comp.CreateCubicBezierEasingFunction(c_frame1point1, c_frame1point2));
		scaleAnim.InsertKeyFrame(1.0f, endScale, comp.CreateCubicBezierEasingFunction(c_frame2point1, c_frame2point2));
		scaleAnim.Duration = TimeSpan.FromMilliseconds(600);

		ScalarKeyFrameAnimation centerAnim = comp.CreateScalarKeyFrameAnimation();
		centerAnim.InsertKeyFrame(0.0f, from < to ? 0.0f : dimension);
		centerAnim.InsertKeyFrame(1.0f, from < to ? dimension : 0.0f, singleStep);
		centerAnim.Duration = TimeSpan.FromMilliseconds(200);

		if (IsTopNavigationView())
		{
			visual.StartAnimation("Offset.X", posAnim);
			visual.StartAnimation("Scale.X", scaleAnim);
			visual.StartAnimation("CenterPoint.X", centerAnim);
		}
		else
		{
			visual.StartAnimation("Offset.Y", posAnim);
			visual.StartAnimation("Scale.Y", scaleAnim);
			visual.StartAnimation("CenterPoint.Y", centerAnim);
		}
	}
#endif
	private void OnAnimationComplete(object sender, CompositionBatchCompletedEventArgs args)
	{
		var indicator = m_prevIndicator;
		ResetElementAnimationProperties(indicator, 0.0f);
		m_prevIndicator = null;

		indicator = m_nextIndicator;
		ResetElementAnimationProperties(indicator, 1.0f);
		m_nextIndicator = null;
	}

	private void ResetElementAnimationProperties(UIElement element, float desiredOpacity)
	{
		if (element != null)
		{
			element.Opacity = desiredOpacity;
			Visual visual = ElementCompositionPreview.GetElementVisual(element);
			if (visual != null)
			{
				visual.Offset = new Vector3(0.0f, 0.0f, 0.0f);
				visual.Scale = new Vector3(1.0f, 1.0f, 1.0f);
				visual.Opacity = desiredOpacity;
			}
		}
	}

	private NavigationViewItemBase NavigationViewItemBaseOrSettingsContentFromData(object data)
	{
		return GetContainerForData<NavigationViewItemBase>(data);
	}

	private NavigationViewItem NavigationViewItemOrSettingsContentFromData(object data)
	{
		return GetContainerForData<NavigationViewItem>(data);
	}

	private bool IsSelectionSuppressed(object item)
	{
		if (item != null)
		{
			var nvi = NavigationViewItemOrSettingsContentFromData(item);
			if (nvi != null)
			{
				return !nvi.SelectsOnInvoked;
			}
		}

		return false;
	}

	private bool ShouldPreserveNavigationViewRS4Behavior()
	{
		// Since RS5, we support topnav
		return m_topNavGrid == null;
	}

	private bool ShouldPreserveNavigationViewRS3Behavior()
	{
		// Since RS4, we support backbutton
		return m_backButton == null;
	}

	private UIElement FindSelectionIndicator(object item)
	{
		if (item != null)
		{
			var container = NavigationViewItemOrSettingsContentFromData(item);
			if (container != null)
			{
				var indicator = container.GetSelectionIndicator();
				if (indicator != null)
				{
					return indicator;
				}
				else
				{
					// Indicator was not found, so maybe the layout hasn't updated yet.
					// So let's do that now.
					container.UpdateLayout();
					container.ApplyTemplate();
					return container.GetSelectionIndicator();
				}
			}
		}
		return null;
	}

	private void RaiseSelectionChangedEvent(object nextItem, bool isSettingsItem, NavigationRecommendedTransitionDirection recommendedDirection)
	{
		var eventArgs = new NavigationViewSelectionChangedEventArgs();
		eventArgs.SelectedItem = nextItem;
		eventArgs.IsSettingsSelected = isSettingsItem;
		if (nextItem is not null)
		{
			if (NavigationViewItemBaseOrSettingsContentFromData(nextItem) is { } fromDataContainer)
			{
				eventArgs.SelectedItemContainer = fromDataContainer;
			}
			else if (GetContainerForIndexPath(m_selectionModel.SelectedIndex, false /* lastVisible */, true /* forceRealize */) is { } container)
			{
				MUX_ASSERT(MenuItemFromContainer(container) == nextItem);
				eventArgs.SelectedItemContainer = container;
			}
		}
		eventArgs.RecommendedNavigationTransitionInfo = CreateNavigationTransitionInfo(recommendedDirection);
		SelectionChanged?.Invoke(this, eventArgs);
	}

	// SelectedItem change can be invoked by API or user's action like clicking. if it's not from API, m_shouldRaiseInvokeItemInSelectionChange would be true
	// If nextItem is selectionsuppressed, we should undo the selection. We didn't undo it OnSelectionChange because we want change by API has the same undo logic.
	private void ChangeSelection(object prevItem, object nextItem)
	{
		bool isSettingsItem = IsSettingsItem(nextItem);

		if (IsSelectionSuppressed(nextItem))
		{
			// This should not be a common codepath. Only happens if customer passes a 'selectionsuppressed' item via API.
			UndoSelectionAndRevertSelectionTo(prevItem, nextItem);
			RaiseItemInvoked(nextItem, isSettingsItem);
		}
		else
		{
			// Other transition other than default only apply to topnav
			// when clicking overflow on topnav, transition is from bottom
			// otherwise if prevItem is on left side of nextActualItem, transition is from left
			//           if prevItem is on right side of nextActualItem, transition is from right
			// click on Settings item is considered Default
			NavigationRecommendedTransitionDirection GetRecommendedDirection(object prevItem, object nextItem)
			{
				if (IsTopNavigationView())
				{
					if (m_selectionChangeFromOverflowMenu)
					{
						return NavigationRecommendedTransitionDirection.FromOverflow;
					}
					else if (prevItem != null && nextItem != null)
					{
						return GetRecommendedTransitionDirection(NavigationViewItemBaseOrSettingsContentFromData(prevItem),
							NavigationViewItemBaseOrSettingsContentFromData(nextItem));
					}
				}
				return NavigationRecommendedTransitionDirection.Default;
			}
			var recommendedDirection = GetRecommendedDirection(prevItem, nextItem);

			// Bug 17850504, Customer may use NavigationViewItem.IsSelected in ItemInvoke or SelectionChanged Event.
			// To keep the logic the same as RS4, ItemInvoke is before unselect the old item
			// And SelectionChanged is after we selected the new item.
			var selectedItem = SelectedItem;
			if (m_shouldRaiseItemInvokedAfterSelection)
			{
				// If selection changed inside ItemInvoked, the flag does not get said to false and the event get's raised again,so we need to set it to false now!
				m_shouldRaiseItemInvokedAfterSelection = false;
				RaiseItemInvoked(nextItem, isSettingsItem, NavigationViewItemOrSettingsContentFromData(nextItem), recommendedDirection);
			}
			// Selection was modified inside ItemInvoked, skip everything here!
			if (selectedItem != SelectedItem)
			{
				return;
			}
			UnselectPrevItem(prevItem, nextItem);
			ChangeSelectStatusForItem(nextItem, true /*selected*/);
			IndexPath indexPath = null;

			if (NavigationViewItemBaseOrSettingsContentFromData(nextItem) is { } container)
			{
				indexPath = GetIndexPathForContainer(container);
			}
			else
			{
				indexPath = GetIndexPathOfItem(nextItem);
			}

			if (indexPath is not null && indexPath.GetSize() > 0)
			{
				// The SelectedItem property has already been updated. So we want to block any logic from executing
				// in the SelectionModel selection changed callback.
				using var scopeGuard = Disposable.Create(() => m_shouldIgnoreNextSelectionChange = false);
				m_shouldIgnoreNextSelectionChange = true;
				UpdateSelectionModelSelection(indexPath);
			}

			using (_ = Disposable.Create(() => m_shouldIgnoreUIASelectionRaiseAsExpandCollapseWillRaise = false))
			{
				// Selection changed and we need to notify UIA
				// HOWEVER expand collapse can also trigger if an item can expand/collapse
				// There are multiple cases when selection changes:
				// - Through click on item with no children . No expand/collapse change
				// - Through click on item with children . Expand/collapse change
				// - Through API with item without children . No expand/collapse change
				// - Through API with item with children . No expand/collapse change
				if (!m_shouldIgnoreUIASelectionRaiseAsExpandCollapseWillRaise)
				{
					AutomationPeer peer = FrameworkElementAutomationPeer.FromElement(this);
					if (peer != null)
					{
						var navViewItemPeer = (NavigationViewAutomationPeer)peer;
						navViewItemPeer.RaiseSelectionChangedEvent(
							prevItem, nextItem
						);
					}
				}
			}

			// If this item has an associated container, we'll raise the SelectionChanged event on it immediately.
			if (NavigationViewItemOrSettingsContentFromData(nextItem) is { } nvi)
			{
				AnimateSelectionChanged(nvi);
				RaiseSelectionChangedEvent(nextItem, isSettingsItem, recommendedDirection);
				ClosePaneIfNeccessaryAfterItemIsClicked(nvi);
			}
			else
			{
				// Otherwise, we'll wait until a container gets realized for this item and raise it then.
				m_isSelectionChangedPending = true;
				m_pendingSelectionChangedItem = nextItem;
				m_pendingSelectionChangedDirection = recommendedDirection;

				var weakThis = WeakReferencePool.RentSelfWeakReference(this);
				Action completePendingSelectionChange = () =>
				{
					if (weakThis.IsAlive && weakThis.Target is NavigationView strongThis)
					{
						strongThis.CompletePendingSelectionChange();
					}
				};

				SharedHelpers.ScheduleActionAfterWait(completePendingSelectionChange, 100);
			}
		}
	}

	private void CompletePendingSelectionChange()
	{
		// It may be the case that this item is in a collapsed repeater, in which case
		// no container will be realized for it.  We'll assume that this this is the case
		// if the UI thread has fallen idle without any SelectionChanged being raised.
		// In this case, we'll raise the SelectionChanged at that time, as otherwise it'll never be raised.
		if (m_isSelectionChangedPending)
		{
			AnimateSelectionChanged(FindLowestLevelContainerToDisplaySelectionIndicator());

			m_isSelectionChangedPending = false;

			var item = m_pendingSelectionChangedItem;
			var direction = m_pendingSelectionChangedDirection;

			m_pendingSelectionChangedItem = null;
			m_pendingSelectionChangedDirection = NavigationRecommendedTransitionDirection.Default;

			RaiseSelectionChangedEvent(item, IsSettingsItem(item), direction);
		}
	}

	private void UpdateSelectionModelSelection(IndexPath ip)
	{
		var prevIndexPath = m_selectionModel.SelectedIndex;
		m_selectionModel.SelectAt(ip);
		UpdateIsChildSelected(prevIndexPath, ip);
	}

	private void UpdateIsChildSelected(IndexPath prevIP, IndexPath nextIP)
	{
		if (prevIP != null && prevIP.GetSize() > 0)
		{
			UpdateIsChildSelectedForIndexPath(prevIP, false /*isChildSelected*/);
		}

		if (nextIP != null && nextIP.GetSize() > 0)
		{
			UpdateIsChildSelectedForIndexPath(nextIP, true /*isChildSelected*/);
		}
	}

	private void UpdateIsChildSelectedForIndexPath(IndexPath ip, bool isChildSelected)
	{
		// Update the isChildSelected property for every container on the IndexPath (with the exception of the actual container pointed to by the indexpath)
		var container = GetContainerForIndex(ip.GetAt(1), ip.GetAt(0) == c_footerMenuBlockIndex /*inFooter*/);
		// first index is fo mainmenu or footer
		// second is index of item in mainmenu or footer
		// next in menuitem children
		var index = 2;
		while (container != null)
		{
			if (container is NavigationViewItem nvi)
			{
				nvi.IsChildSelected = isChildSelected;
				var nextIR = nvi.GetRepeater();
				if (nextIR != null)
				{
					if (index < ip.GetSize() - 1)
					{
						container = nextIR.TryGetElement(ip.GetAt(index));
						index++;
						continue;
					}
				}
			}
			container = null;
		}
	}

	private void RaiseItemInvoked(
		object item,
		bool isSettings,
		NavigationViewItemBase container = null,
		NavigationRecommendedTransitionDirection recommendedDirection = NavigationRecommendedTransitionDirection.Default)
	{
		var invokedItem = item;
		var invokedContainer = container;

		var eventArgs = new NavigationViewItemInvokedEventArgs();

		if (container != null)
		{
			invokedItem = container.Content;
		}
		else
		{
			// InvokedItem is container for Settings, but Content of item for other ListViewItem
			if (!isSettings)
			{
				var containerFromData = NavigationViewItemBaseOrSettingsContentFromData(item);
				if (containerFromData != null)
				{
					invokedItem = containerFromData.Content;
					invokedContainer = containerFromData;
				}
			}
			else
			{
				MUX_ASSERT(item != null);
				invokedContainer = item as NavigationViewItemBase;
				MUX_ASSERT(invokedContainer != null);
			}
		}
		eventArgs.InvokedItem = invokedItem;
		eventArgs.InvokedItemContainer = invokedContainer;
		eventArgs.IsSettingsInvoked = isSettings;
		eventArgs.RecommendedNavigationTransitionInfo = CreateNavigationTransitionInfo(recommendedDirection);
		ItemInvoked?.Invoke(this, eventArgs);
	}

	// forceSetDisplayMode: On first call to SetDisplayMode, force setting to initial values
	private void SetDisplayMode(NavigationViewDisplayMode displayMode, bool forceSetDisplayMode)
	{
		// Need to keep the VisualStateGroup "DisplayModeGroup" updated even if the actual
		// display mode is not changed. This is due to the fact that there can be a transition between
		// 'Minimal' and 'MinimalWithBackButton'.
		UpdateVisualStateForDisplayModeGroup(displayMode);

		if (forceSetDisplayMode || DisplayMode != displayMode)
		{
			// Update header visibility based on what the new display mode will be
			UpdateHeaderVisibility(displayMode);

			UpdatePaneTabFocusNavigation();

			UpdatePaneToggleSize();

			RaiseDisplayModeChanged(displayMode);
		}
	}

	// To support TopNavigationView, DisplayModeGroup in visualstate(We call it VisualStateDisplayMode) is decoupled with DisplayMode.
	// The VisualStateDisplayMode is the combination of TopNavigationView, DisplayMode, PaneDisplayMode.
	// Here is the mapping:
	//    TopNav . Minimal
	//    PaneDisplayMode.Left || (PaneDisplayMode.Auto && DisplayMode.Expanded) . Expanded
	//    PaneDisplayMode.LeftCompact || (PaneDisplayMode.Auto && DisplayMode.Compact) . Compact
	//    Map others to Minimal or MinimalWithBackButton
	NavigationViewVisualStateDisplayMode GetVisualStateDisplayMode(NavigationViewDisplayMode displayMode)
	{
		var paneDisplayMode = PaneDisplayMode;

		if (IsTopNavigationView())
		{
			return NavigationViewVisualStateDisplayMode.Minimal;
		}

		if (paneDisplayMode == NavigationViewPaneDisplayMode.Left ||
			(paneDisplayMode == NavigationViewPaneDisplayMode.Auto && displayMode == NavigationViewDisplayMode.Expanded))
		{
			return NavigationViewVisualStateDisplayMode.Expanded;
		}

		if (paneDisplayMode == NavigationViewPaneDisplayMode.LeftCompact ||
			(paneDisplayMode == NavigationViewPaneDisplayMode.Auto && displayMode == NavigationViewDisplayMode.Compact))
		{
			return NavigationViewVisualStateDisplayMode.Compact;
		}

		// In minimal mode, when the NavView is closed, the HeaderContent doesn't have
		// its own dedicated space, and must 'share' the top of the NavView with the
		// pane toggle button ('hamburger' button) and the back button.
		// When the NavView is open, the close button is taking space instead of the back button.
		if (ShouldShowBackButton() || ShouldShowCloseButton())
		{
			return NavigationViewVisualStateDisplayMode.MinimalWithBackButton;
		}
		else
		{
			return NavigationViewVisualStateDisplayMode.Minimal;
		}
	}

	private void UpdateVisualStateForDisplayModeGroup(NavigationViewDisplayMode displayMode)
	{
		var splitView = m_rootSplitView;
		if (splitView != null)
		{
			var visualStateDisplayMode = GetVisualStateDisplayMode(displayMode);
			var visualStateName = "";
			var splitViewDisplayMode = SplitViewDisplayMode.Overlay;
			var visualStateNameMinimal = "Minimal";

			switch (visualStateDisplayMode)
			{
				case NavigationViewVisualStateDisplayMode.MinimalWithBackButton:
					visualStateName = "MinimalWithBackButton";
					splitViewDisplayMode = SplitViewDisplayMode.Overlay;
					break;
				case NavigationViewVisualStateDisplayMode.Minimal:
					visualStateName = visualStateNameMinimal;
					splitViewDisplayMode = SplitViewDisplayMode.Overlay;
					break;
				case NavigationViewVisualStateDisplayMode.Compact:
					visualStateName = "Compact";
					splitViewDisplayMode = SplitViewDisplayMode.CompactOverlay;
					break;
				case NavigationViewVisualStateDisplayMode.Expanded:
					visualStateName = "Expanded";
					splitViewDisplayMode = SplitViewDisplayMode.CompactInline;
					break;
			}

			// When the pane is made invisible we need to collapse the pane part of the SplitView
			if (!IsPaneVisible)
			{
				splitViewDisplayMode = SplitViewDisplayMode.CompactOverlay;
			}

			var handled = false;
			if (visualStateName == visualStateNameMinimal && IsTopNavigationView())
			{
				// TopNavigationMinimal was introduced in 19H1. We need to fallback to Minimal if the customer uses an older template.
				handled = VisualStateManager.GoToState(this, "TopNavigationMinimal", false /*useTransitions*/);
			}
			if (!handled)
			{
				VisualStateManager.GoToState(this, visualStateName, false /*useTransitions*/);
			}

			// Updating the splitview 'DisplayMode' property in some diplaymodes causes children to be added to the popup root.
			// This causes an exception if the NavigationView is in the popup root itself (as SplitView is trying to add children to the tree while it is being measured).
			// Due to this, we want to defer updating this property for all calls coming from `OnApplyTemplate`to the OnLoaded function.
			if (m_fromOnApplyTemplate)
			{
				m_updateVisualStateForDisplayModeFromOnLoaded = true;
			}
			else
			{
				splitView.DisplayMode = splitViewDisplayMode;
			}
		}
	}

	private void OnNavigationViewItemTapped(object sender, TappedRoutedEventArgs args)
	{
		if (sender is NavigationViewItem nvi)
		{
			OnNavigationViewItemInvoked(nvi);
			nvi.Focus(FocusState.Pointer);
			args.Handled = true;
		}
	}

	private void OnNavigationViewItemKeyDown(object sender, KeyRoutedEventArgs args)
	{
		if ((args.OriginalKey == VirtualKey.GamepadA
			|| args.Key == VirtualKey.Enter
			|| args.Key == VirtualKey.Space))
		{
			// Only handle those keys if the key is not being held down!
			if (!args.KeyStatus.WasKeyDown)
			{
				if (sender is NavigationViewItem nvi)
				{
					HandleKeyEventForNavigationViewItem(nvi, args);
				}
			}
		}
		else
		{
			if (sender is NavigationViewItem nvi)
			{
				HandleKeyEventForNavigationViewItem(nvi, args);
			}
		}
	}

	private void HandleKeyEventForNavigationViewItem(NavigationViewItem nvi, KeyRoutedEventArgs args)
	{
		var key = args.Key;
		switch (key)
		{
			case VirtualKey.Enter:
			case VirtualKey.Space:
				args.Handled = true;
				OnNavigationViewItemInvoked(nvi);
				break;
			case VirtualKey.Home:
				args.Handled = true;
				KeyboardFocusFirstItemFromItem(nvi);
				break;
			case VirtualKey.End:
				args.Handled = true;
				KeyboardFocusLastItemFromItem(nvi);
				break;
			case VirtualKey.Down:
				FocusNextDownItem(nvi, args);
				break;
			case VirtualKey.Up:
				FocusNextUpItem(nvi, args);
				break;
		}
	}

	private void FocusNextUpItem(NavigationViewItem nvi, KeyRoutedEventArgs args)
	{
		if (args.OriginalSource != nvi)
		{
			return;
		}

		bool shouldHandleFocus = true;
		var nviImpl = nvi;
		var nextFocusableElement = FocusManager.FindNextFocusableElement(FocusNavigationDirection.Up);

		if (nextFocusableElement is NavigationViewItem nextFocusableNVI)
		{

			var nextFocusableNVIImpl = nextFocusableNVI;

			if (nextFocusableNVIImpl.Depth == nviImpl.Depth)
			{
				// If we not at the top of the list for our current depth and the item above us has children, check whether we should move focus onto a child
				if (DoesNavigationViewItemHaveChildren(nextFocusableNVI))
				{
					// Focus on last lowest level visible container
					var childRepeater = nextFocusableNVIImpl.GetRepeater();
					if (childRepeater != null)
					{
						var lastFocusableElement = FocusManager.FindLastFocusableElement(childRepeater);
						if (lastFocusableElement != null)
						{
							if (lastFocusableElement is Control lastFocusableNVI)
							{
								args.Handled = lastFocusableNVI.Focus(FocusState.Keyboard);
							}
						}

						else
						{
							args.Handled = nextFocusableNVIImpl.Focus(FocusState.Keyboard);
						}

					}
				}
				else
				{
					// Traversing up a list where XYKeyboardFocus will result in correct behavior
					shouldHandleFocus = false;
				}
			}
		}

		// We are at the top of the list, focus on parent
		if (shouldHandleFocus && !args.Handled && nviImpl.Depth > 0)
		{
			var parentContainer = GetParentNavigationViewItemForContainer(nvi);
			if (parentContainer != null)
			{
				args.Handled = parentContainer.Focus(FocusState.Keyboard);
			}
		}
	}

	// If item has focusable children, move focus to first focusable child, otherise just defer to default XYKeyboardFocus behavior
	private void FocusNextDownItem(NavigationViewItem nvi, KeyRoutedEventArgs args)
	{
		if (args.OriginalSource != nvi)
		{
			return;
		}

		if (DoesNavigationViewItemHaveChildren(nvi))
		{
			var nviImpl = nvi;
			var childRepeater = nviImpl.GetRepeater();
			if (childRepeater != null)
			{
				var firstFocusableElement = FocusManager.FindFirstFocusableElement(childRepeater);
				if (firstFocusableElement is Control controlFirst)
				{
					args.Handled = controlFirst.Focus(FocusState.Keyboard);
				}
			}
		}
	}

	private void KeyboardFocusFirstItemFromItem(NavigationViewItemBase nvib)
	{
		var parentIR = GetParentRootItemsRepeaterForContainer(nvib);
		var firstElement = parentIR.TryGetElement(0);

		if (firstElement is Control controlFirst)
		{
			controlFirst.Focus(FocusState.Keyboard);
		}
	}

	private void KeyboardFocusLastItemFromItem(NavigationViewItemBase nvib)
	{
		var parentIR = GetParentRootItemsRepeaterForContainer(nvib);

		var itemsSourceView = parentIR.ItemsSourceView;
		if (itemsSourceView != null)
		{
			var lastIndex = itemsSourceView.Count - 1;
			var lastElement = parentIR.TryGetElement(lastIndex);
			if (lastElement != null)
			{
				if (lastElement is Control controlLast)
				{
					controlLast.Focus(FocusState.Programmatic);
				}
			}
		}
	}

	private void OnRepeaterGettingFocus(object sender, GettingFocusEventArgs args)
	{
		// if focus change was invoked by tab key
		// and there is selected item in ItemsRepeater that gatting focus
		// we should put focus on selected item
		if (m_TabKeyPrecedesFocusChange && args.InputDevice == FocusInputDeviceKind.Keyboard && m_selectionModel.SelectedIndex != null)
		{
			var oldFocusedElement = args.OldFocusedElement;
			if (oldFocusedElement != null)
			{
				if (sender is ItemsRepeater newRootItemsRepeater)
				{
					bool GetIsFocusOutsideCurrentRootRepeater(DependencyObject oldFocusedElement, ItemsRepeater newRootItemsRepeater)
					{
						bool isFocusOutsideCurrentRootRepeater = true;
						var treeWalkerCursor = oldFocusedElement;

						// check if last focused element was in same root repeater
						while (treeWalkerCursor != null)
						{
							if (treeWalkerCursor is NavigationViewItemBase oldFocusedNavigationItemBase)
							{
								var oldParentRootRepeater = GetParentRootItemsRepeaterForContainer(oldFocusedNavigationItemBase);
								isFocusOutsideCurrentRootRepeater = oldParentRootRepeater != newRootItemsRepeater;
								break;
							}

							treeWalkerCursor = VisualTreeHelper.GetParent(treeWalkerCursor);
						}

						return isFocusOutsideCurrentRootRepeater;
					}

					var isFocusOutsideCurrentRootRepeater = GetIsFocusOutsideCurrentRootRepeater(oldFocusedElement, newRootItemsRepeater);

					ItemsRepeater GetRootRepeaterForSelectedItem()
					{
						if (IsTopNavigationView())
						{
							return m_selectionModel.SelectedIndex.GetAt(0) == c_mainMenuBlockIndex ? m_topNavRepeater : m_topNavFooterMenuRepeater;
						}
						return m_selectionModel.SelectedIndex.GetAt(0) == c_mainMenuBlockIndex ? m_leftNavRepeater : m_leftNavFooterMenuRepeater;
					}

					var rootRepeaterForSelectedItem = GetRootRepeaterForSelectedItem();

					// If focus is coming from outside the root repeater,
					// and selected item is within current repeater
					// we should put focus on selected item
					if (args is GettingFocusEventArgs argsAsIGettingFocusEventArgs2)
					{
						if (newRootItemsRepeater == rootRepeaterForSelectedItem && isFocusOutsideCurrentRootRepeater)
						{
							var selectedContainer = GetContainerForIndexPath(m_selectionModel.SelectedIndex, true /* lastVisible */);
							if (argsAsIGettingFocusEventArgs2.TrySetNewFocusedElement(selectedContainer))
							{
								args.Handled = true;
							}
						}
					}
				}
			}
		}

		m_TabKeyPrecedesFocusChange = false;
	}

	private void OnNavigationViewItemOnGotFocus(object sender, RoutedEventArgs e)
	{
		if (sender is NavigationViewItem nvi)
		{
			// Achieve selection follows focus behavior
			if (IsNavigationViewListSingleSelectionFollowsFocus())
			{
				// if nvi is already selected we don't need to invoke it again
				// otherwise ItemInvoked fires twice when item was tapped
				// or fired when window gets focus
				if (nvi.SelectsOnInvoked && !nvi.IsSelected)
				{
					if (IsTopNavigationView())
					{
						var parentIR = GetParentItemsRepeaterForContainer(nvi);
						if (parentIR != null)
						{
							if (parentIR != m_topNavRepeaterOverflowView)
							{
								OnNavigationViewItemInvoked(nvi);
							}
						}
					}
					else
					{
						OnNavigationViewItemInvoked(nvi);
					}
				}
			}
		}
	}

	internal void OnSettingsInvoked()
	{
		var settingsItem = m_settingsItem;
		if (settingsItem != null)
		{
			OnNavigationViewItemInvoked(settingsItem);
		}
	}

	protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
	{
		m_TabKeyPrecedesFocusChange = false;
		base.OnPreviewKeyDown(e);
	}

	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		var eventArgs = e;
		var key = eventArgs.Key;

		bool handled = false;
		m_TabKeyPrecedesFocusChange = false;

		switch (key)
		{
			case VirtualKey.GamepadView:
				if (!IsPaneOpen && !IsTopNavigationView())
				{
					OpenPane();
					handled = true;
				}
				break;
			case VirtualKey.GoBack:
			case VirtualKey.XButton1:
				if (IsPaneOpen && IsLightDismissible())
				{
					handled = AttemptClosePaneLightly();
				}
				break;
			case VirtualKey.GamepadLeftShoulder:
				handled = BumperNavigation(-1);
				break;
			case VirtualKey.GamepadRightShoulder:
				handled = BumperNavigation(1);
				break;
			case VirtualKey.Tab:
				// arrow keys navigation through ItemsRepeater don't get here
				// so handle tab key to distinguish between tab focus and arrow focus navigation
				m_TabKeyPrecedesFocusChange = true;
				break;
			case VirtualKey.Left:
				var altState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Menu);
				bool isAltPressed = (altState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

				if (isAltPressed && IsPaneOpen && IsLightDismissible())
				{
					handled = AttemptClosePaneLightly();
				}

				break;
		}

		eventArgs.Handled = handled;

		base.OnKeyDown(e);
	}

	private bool BumperNavigation(int offset)
	{
		// By passing an offset indicating direction (ideally 1 or -1, meaning right or left respectively)
		// we'll try to move focus to an item. We won't be moving focus to items in the overflow menu and this won't
		// work on left navigation, only dealing with the top primary list here and only with items that don't have
		// !SelectsOnInvoked set to true. If !SelectsOnInvoked is true, we'll skip the item and try focusing on the next one
		// that meets the conditions, in the same direction.
		var shoulderNavigationEnabledParamValue = ShoulderNavigationEnabled;
		var shoulderNavigationForcedDisabled = (shoulderNavigationEnabledParamValue == NavigationViewShoulderNavigationEnabled.Never);
		var shoulderNavigationOptionalDisabled = (shoulderNavigationEnabledParamValue == NavigationViewShoulderNavigationEnabled.WhenSelectionFollowsFocus
		   && SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled);

		if (!IsTopNavigationView()
			|| shoulderNavigationOptionalDisabled
			|| shoulderNavigationForcedDisabled)
		{
			return false;
		}

		var shoulderNavigationSelectionFollowsFocusEnabled = (SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Enabled
		   && shoulderNavigationEnabledParamValue == NavigationViewShoulderNavigationEnabled.WhenSelectionFollowsFocus);

		var shoulderNavigationEnabled = (shoulderNavigationSelectionFollowsFocusEnabled
		   || shoulderNavigationEnabledParamValue == NavigationViewShoulderNavigationEnabled.Always);

		if (!shoulderNavigationEnabled)
		{
			return false;
		}

		var item = SelectedItem;

		if (item != null)
		{
			var nvi = NavigationViewItemOrSettingsContentFromData(item);
			if (nvi != null)
			{
				var indexPath = GetIndexPathForContainer(nvi);
				var isInFooter = indexPath.GetAt(0) == c_footerMenuBlockIndex;

				var indexInMainList = isInFooter ? -1 : indexPath.GetAt(1);
				var indexInFooter = isInFooter ? indexPath.GetAt(1) : -1;

				var topNavRepeater = m_topNavRepeater;
				var topPrimaryListSize = m_topDataProvider.GetPrimaryListSize();

				var footerRepeater = m_topNavFooterMenuRepeater;
				var footerItemsSize = FooterMenuItems.Count;

				if (IsSettingsVisible)
				{
					footerItemsSize++;
				}

				if (indexInMainList >= 0)
				{

					if (SelectSelectableItemWithOffset(indexInMainList, offset, topNavRepeater, topPrimaryListSize))
					{
						return true;
					}

					// No sutable item found in main list so try to select item in footer
					if (offset > 0)
					{
						return SelectSelectableItemWithOffset(-1, offset, footerRepeater, footerItemsSize);
					}

					return false;
				}

				if (indexInFooter >= 0)
				{

					if (SelectSelectableItemWithOffset(indexInFooter, offset, footerRepeater, footerItemsSize))
					{
						return true;
					}

					// No sutable item found in footer so try to select item in main list
					if (offset < 0)
					{
						return SelectSelectableItemWithOffset(topPrimaryListSize, offset, topNavRepeater, topPrimaryListSize);
					}
				}
			}
		}

		return false;
	}

	private bool SelectSelectableItemWithOffset(int startIndex, int offset, ItemsRepeater repeater, int repeaterCollectionSize)
	{
		startIndex += offset;
		while (startIndex > -1 && startIndex < repeaterCollectionSize)
		{
			var newItem = repeater.TryGetElement(startIndex);
			if (newItem is NavigationViewItem newNavViewItem)
			{
				// This is done to skip Separators or other items that are not NavigationViewItems
				if (newNavViewItem.SelectsOnInvoked)
				{
					newNavViewItem.IsSelected = true;
					return true;
				}
			}

			startIndex += offset;
		}
		return false;
	}

	public object MenuItemFromContainer(DependencyObject container)
	{
		if (container != null)
		{
			if (container is NavigationViewItemBase nvib)
			{
				var parentRepeater = GetParentItemsRepeaterForContainer(nvib);
				if (parentRepeater != null)
				{
					var containerIndex = parentRepeater.GetElementIndex(nvib);
					if (containerIndex >= 0)
					{
						return GetItemFromIndex(parentRepeater, containerIndex);
					}
				}
			}
		}
		return null;
	}

	public DependencyObject ContainerFromMenuItem(object item)
	{
		var data = item;
		if (data != null)
		{
			return NavigationViewItemBaseOrSettingsContentFromData(item);
		}

		return null;
	}

	private void OnTopNavDataSourceChanged(NotifyCollectionChangedEventArgs args)
	{
		CloseTopNavigationViewFlyout();

		// Assume that raw data doesn't change very often for navigationview.
		// So here is a simple implementation and for each data item change, it request a layout change
		// update this in the future if there is performance problem

		// If it's Uninitialized, it means that we didn't start the layout yet.
		if (m_topNavigationMode != TopNavigationViewLayoutState.Uninitialized)
		{
			m_topDataProvider.MoveAllItemsToPrimaryList();
		}

		m_lastSelectedItemPendingAnimationInTopNav = null;
	}

	internal int GetNavigationViewItemCountInPrimaryList()
	{
		return m_topDataProvider.GetNavigationViewItemCountInPrimaryList();
	}

	internal int GetNavigationViewItemCountInTopNav()
	{
		return m_topDataProvider.GetNavigationViewItemCountInTopNav();
	}

	internal SplitView GetSplitView()
	{
		return m_rootSplitView;
	}

	internal void TopNavigationViewItemContentChanged()
	{
		if (m_appliedTemplate)
		{
			if (MenuItemsSource == null)
			{
				m_topDataProvider.InvalidWidthCache();
			}
			InvalidateMeasure();
		}
	}

	private void OnAccessKeyInvoked(object sender, AccessKeyInvokedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		// For topnav, invoke Morebutton, otherwise togglebutton
		var button = IsTopNavigationView() ? m_topNavOverflowButton : m_paneToggleButton;
		if (button != null)
		{
			var peer = FrameworkElementAutomationPeer.FromElement(button) as ButtonAutomationPeer;
			if (peer != null)
			{
				peer.Invoke();
				args.Handled = true;
			}
		}
	}

	private NavigationTransitionInfo CreateNavigationTransitionInfo(NavigationRecommendedTransitionDirection recommendedTransitionDirection)
	{
		// In current implementation, if click is from overflow item, just recommend FromRight Slide animation.
		if (recommendedTransitionDirection == NavigationRecommendedTransitionDirection.FromOverflow)
		{
			recommendedTransitionDirection = NavigationRecommendedTransitionDirection.FromRight;
		}

		if ((recommendedTransitionDirection == NavigationRecommendedTransitionDirection.FromLeft
			|| recommendedTransitionDirection == NavigationRecommendedTransitionDirection.FromRight)
			&& SharedHelpers.IsRS5OrHigher())
		{
			var sliderNav = new SlideNavigationTransitionInfo();
			SlideNavigationTransitionEffect effect =
			   recommendedTransitionDirection == NavigationRecommendedTransitionDirection.FromRight ?
			   SlideNavigationTransitionEffect.FromRight :
			   SlideNavigationTransitionEffect.FromLeft;
			// PR 1895355: Bug 17724768: Remove Side-to-Side navigation transition velocity key
			// https://microsoft.visualstudio.com/_git/os/commit/7d58531e69bc8ad1761cff938d8db25f6fb6a841
			// We want to use Effect, but it's not in all os of rs5. as a workaround, we only apply effect to the os which is already remove velocity key.
			if (sliderNav is SlideNavigationTransitionInfo sliderNav2)
			{
				sliderNav.Effect = effect;
			}
			return sliderNav;
		}

		else
		{
			var defaultInfo = new EntranceNavigationTransitionInfo();
			return defaultInfo;
		}
	}

	private NavigationRecommendedTransitionDirection GetRecommendedTransitionDirection(DependencyObject prev, DependencyObject next)
	{
		var recommendedTransitionDirection = NavigationRecommendedTransitionDirection.Default;
		var ir = m_topNavRepeater;

		if (prev != null && next != null && ir != null)
		{
			var prevIndexPath = GetIndexPathForContainer(prev as NavigationViewItemBase);
			var nextIndexPath = GetIndexPathForContainer(next as NavigationViewItemBase);

			var compare = prevIndexPath.CompareTo(nextIndexPath);

			switch (compare)
			{
				case -1:
					recommendedTransitionDirection = NavigationRecommendedTransitionDirection.FromRight;
					break;
				case 1:
					recommendedTransitionDirection = NavigationRecommendedTransitionDirection.FromLeft;
					break;
				default:
					recommendedTransitionDirection = NavigationRecommendedTransitionDirection.Default;
					break;
			}
		}
		return recommendedTransitionDirection;
	}

	private NavigationViewTemplateSettings GetTemplateSettings()
	{
		return TemplateSettings;
	}

	private bool IsNavigationViewListSingleSelectionFollowsFocus()
	{
		return SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Enabled;
	}

	private void UpdateSingleSelectionFollowsFocusTemplateSetting()
	{
		GetTemplateSettings().SingleSelectionFollowsFocus = IsNavigationViewListSingleSelectionFollowsFocus();
	}

	private void OnMenuItemsSourceCollectionChanged(object sender, object args)
	{
		if (!IsTopNavigationView())
		{
			var repeater = m_leftNavRepeater;
			if (repeater != null)
			{
				repeater.UpdateLayout();
			}
			UpdatePaneLayout();
		}
	}

	private void OnSelectedItemPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var newItem = args.NewValue;
		var oldItem = args.OldValue;

		ChangeSelection(oldItem, newItem);

		if (m_appliedTemplate && IsTopNavigationView())
		{
			if (m_layoutUpdatedToken.Disposable == null ||
				(newItem != null && m_topDataProvider.IndexOf(newItem) != itemNotFound && m_topDataProvider.IndexOf(newItem, NavigationViewSplitVectorID.PrimaryList) == itemNotFound)) // selection is in overflow
			{
				InvalidateTopNavPrimaryLayout();
			}
		}
	}

	private void SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(object item)
	{
		SelectedItem = item;
	}

	private void ChangeSelectStatusForItem(object item, bool selected)
	{
		var container = NavigationViewItemOrSettingsContentFromData(item);
		if (container != null)
		{
			// If we unselect an item, ListView doesn't tolerate setting the SelectedItem to null.
			// Instead we remove IsSelected from the item itself, and it make ListView to unselect it.
			// If we select an item, we follow the unselect to simplify the code.
			container.IsSelected = selected;
		}
		else if (selected)
		{
			// If we are selecting an item and have not found a realized container for it,
			// we may need to manually resolve a container for this in order to update the
			// SelectionModel's selected IndexPath.
			var ip = GetIndexPathOfItem(item);
			if (ip != null && ip.GetSize() > 0)
			{
				// The SelectedItem property has already been updated. So we want to block any logic from executing
				// in the SelectionModel selection changed callback.
				try
				{
					m_shouldIgnoreNextSelectionChange = true;
					UpdateSelectionModelSelection(ip);
				}
				finally
				{
					m_shouldIgnoreNextSelectionChange = false;
				}
			}
		}
	}

	private bool IsSettingsItem(object item)
	{
		bool isSettingsItem = false;
		if (item != null)
		{
			var settingItem = m_settingsItem;
			if (settingItem != null)
			{
				isSettingsItem = (settingItem == item) || (settingItem.Content == item);
			}
		}
		return isSettingsItem;
	}

	private void UnselectPrevItem(object prevItem, object nextItem)
	{
		if (prevItem != null && prevItem != nextItem)
		{
			var setIgnoreNextSelectionChangeToFalse = !m_shouldIgnoreNextSelectionChange;
			try
			{
				m_shouldIgnoreNextSelectionChange = true;
				ChangeSelectStatusForItem(prevItem, false /*selected*/);
			}
			finally
			{
				if (setIgnoreNextSelectionChangeToFalse)
				{
					m_shouldIgnoreNextSelectionChange = false;
				}
			}
		}
	}

	private void UndoSelectionAndRevertSelectionTo(object prevSelectedItem, object nextItem)
	{
		object selectedItem = null;
		if (prevSelectedItem != null)
		{
			if (IsSelectionSuppressed(prevSelectedItem))
			{
				AnimateSelectionChanged(null);
			}
			else
			{
				ChangeSelectStatusForItem(prevSelectedItem, true /*selected*/);
				AnimateSelectionChangedToItem(prevSelectedItem);
				selectedItem = prevSelectedItem;
			}
		}
		else
		{
			// Bug 18033309, A SelectsOnInvoked=false item is clicked, if we don't unselect it from listview, the second click will not raise ItemClicked
			// because listview doesn't raise SelectionChange.
			ChangeSelectStatusForItem(nextItem, false /*selected*/);
		}
		SelectedItem = selectedItem;
	}

	private void CloseTopNavigationViewFlyout()
	{
		var button = m_topNavOverflowButton;
		if (button != null)
		{
			var flyout = button.Flyout;
			if (flyout != null)
			{
				flyout.Hide();
			}
		}
	}

	private new void UpdateVisualState(bool useTransitions = false)
	{
		if (m_appliedTemplate)
		{
			var box = AutoSuggestBox;
			VisualStateManager.GoToState(this, box != null ? "AutoSuggestBoxVisible" : "AutoSuggestBoxCollapsed", false /*useTransitions*/);

			bool isVisible = IsSettingsVisible;
			VisualStateManager.GoToState(this, isVisible ? "SettingsVisible" : "SettingsCollapsed", false /*useTransitions*/);

			if (IsTopNavigationView())
			{
				UpdateVisualStateForOverflowButton();
			}
			else
			{
				UpdateLeftNavigationOnlyVisualState(useTransitions);
			}
		}
	}

	private void UpdateVisualStateForOverflowButton()
	{
		var state = (OverflowLabelMode == NavigationViewOverflowLabelMode.MoreLabel) ?
			"OverflowButtonWithLabel" :
			"OverflowButtonNoLabel";
		VisualStateManager.GoToState(this, state, false /* useTransitions*/);
	}

	private void UpdateLeftNavigationOnlyVisualState(bool useTransitions)
	{
		bool isToggleButtonVisible = IsPaneToggleButtonVisible;
		VisualStateManager.GoToState(this, isToggleButtonVisible || !m_isLeftPaneTitleEmpty ? "TogglePaneButtonVisible" : "TogglePaneButtonCollapsed", false /*useTransitions*/);
	}

	private void SetNavigationViewItemRevokers(NavigationViewItem nvi)
	{
		nvi.Tapped += OnNavigationViewItemTapped;
		nvi.KeyDown += OnNavigationViewItemKeyDown;
		nvi.GotFocus += OnNavigationViewItemOnGotFocus;
		var isSelectedSubscription = nvi.RegisterPropertyChangedCallback(NavigationViewItemBase.IsSelectedProperty, OnNavigationViewItemIsSelectedPropertyChanged);
		var isExpandedSubscription = nvi.RegisterPropertyChangedCallback(NavigationViewItem.IsExpandedProperty, OnNavigationViewItemExpandedPropertyChanged);
		nvi.EventRevoker.Disposable = Disposable.Create(() =>
		{
			nvi.Tapped -= OnNavigationViewItemTapped;
			nvi.KeyDown -= OnNavigationViewItemKeyDown;
			nvi.GotFocus -= OnNavigationViewItemOnGotFocus;
			nvi.UnregisterPropertyChangedCallback(NavigationViewItemBase.IsSelectedProperty, isSelectedSubscription);
			nvi.UnregisterPropertyChangedCallback(NavigationViewItem.IsExpandedProperty, isExpandedSubscription);
		});
		m_itemsWithRevokerObjects.Add(nvi);
	}

	private void ClearNavigationViewItemRevokers(NavigationViewItem nvi)
	{
		RevokeNavigationViewItemRevokers(nvi);

		nvi.EventRevoker.Disposable = null;
		m_itemsWithRevokerObjects.Remove(nvi);
	}

	private void ClearAllNavigationViewItemRevokers()
	{
		foreach (var nvi in m_itemsWithRevokerObjects)
		{
			// ClearAllNavigationViewItemRevokers is only called in the destructor, where exceptions cannot be thrown.
			// If the associated NV has not yet been cleaned up, we must detach these revokers or risk a call into freed
			// memory being made.  However if they have been cleaned up these calls will throw. In this case we can ignore
			// those exceptions.
			try
			{
				RevokeNavigationViewItemRevokers(nvi);

#if !HAS_UNO // TODO Uno specific: Revokers are implemented differently than in WinUI.
				nvi.SetValue(s_NavigationViewItemRevokersProperty, nullptr);
#endif
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError("Failed to clear revokers for NavigationViewItem.", ex);
				}
			}
		}
		m_itemsWithRevokerObjects.Clear();
	}

	private void RevokeNavigationViewItemRevokers(NavigationViewItem nvi)
	{
		// TODO Uno specific: Revokers are implemented differently than in WinUI.
		nvi.EventRevoker.Disposable = null;
	}

	private void InvalidateTopNavPrimaryLayout()
	{
		if (m_appliedTemplate && IsTopNavigationView())
		{
			InvalidateMeasure();
		}
	}

	private double MeasureTopNavigationViewDesiredWidth(Size availableSize)
	{
		return LayoutUtils.MeasureAndGetDesiredWidthFor(m_topNavGrid, availableSize);
	}

	private double MeasureTopNavMenuItemsHostDesiredWidth(Size availableSize)
	{
		return LayoutUtils.MeasureAndGetDesiredWidthFor(m_topNavRepeater, availableSize);
	}

	private double GetTopNavigationViewActualWidth()
	{
		double width = LayoutUtils.GetActualWidthFor(m_topNavGrid);
		MUX_ASSERT(width < double.MaxValue);
		return (double)(width);
	}

	private bool HasTopNavigationViewItemNotInPrimaryList()
	{
		return m_topDataProvider.GetPrimaryListSize() != m_topDataProvider.Size();
	}

	private void ResetAndRearrangeTopNavItems(Size availableSize)
	{
		if (HasTopNavigationViewItemNotInPrimaryList())
		{
			m_topDataProvider.MoveAllItemsToPrimaryList();
		}
		ArrangeTopNavItems(availableSize);
	}

	private void HandleTopNavigationMeasureOverride(Size availableSize)
	{
		// Determine if TopNav is in Overflow
		if (HasTopNavigationViewItemNotInPrimaryList())
		{
			HandleTopNavigationMeasureOverrideOverflow(availableSize);
		}
		else
		{
			HandleTopNavigationMeasureOverrideNormal(availableSize);
		}

		if (m_topNavigationMode == TopNavigationViewLayoutState.Uninitialized)
		{
			m_topNavigationMode = TopNavigationViewLayoutState.Initialized;
		}
	}

	private void HandleTopNavigationMeasureOverrideNormal(Size availableSize)
	{
		var desiredWidth = MeasureTopNavigationViewDesiredWidth(c_infSize);
		if (desiredWidth > availableSize.Width)
		{
			ResetAndRearrangeTopNavItems(availableSize);
		}
	}

	private void HandleTopNavigationMeasureOverrideOverflow(Size availableSize)
	{
		var desiredWidth = MeasureTopNavigationViewDesiredWidth(c_infSize);
		if (desiredWidth > availableSize.Width)
		{
			ShrinkTopNavigationSize(desiredWidth, availableSize);
		}
		else if (desiredWidth < availableSize.Width)
		{
			var fullyRecoverWidth = m_topDataProvider.WidthRequiredToRecoveryAllItemsToPrimary();
			if (availableSize.Width >= desiredWidth + fullyRecoverWidth + m_topNavigationRecoveryGracePeriodWidth)
			{
				// It's possible to recover from Overflow to Normal state, so we restart the MeasureOverride from first step
				ResetAndRearrangeTopNavItems(availableSize);
			}
			else
			{
				var movableItems = FindMovableItemsRecoverToPrimaryList(availableSize.Width - desiredWidth, Array.Empty<int>()/*includeItems*/);
				m_topDataProvider.MoveItemsToPrimaryList(movableItems);
			}
		}
	}

	private void ArrangeTopNavItems(Size availableSize)
	{
		SetOverflowButtonVisibility(Visibility.Collapsed);
		var desiredWidth = MeasureTopNavigationViewDesiredWidth(c_infSize);
		if (!(desiredWidth < availableSize.Width))
		{
			// overflow
			SetOverflowButtonVisibility(Visibility.Visible);
			var desiredWidthForOverflowButton = MeasureTopNavigationViewDesiredWidth(c_infSize);

			MUX_ASSERT(desiredWidthForOverflowButton >= desiredWidth);
			m_topDataProvider.OverflowButtonWidth = desiredWidthForOverflowButton - desiredWidth;

			ShrinkTopNavigationSize(desiredWidthForOverflowButton, availableSize);
		}
	}

	private void SetOverflowButtonVisibility(Visibility visibility)
	{
		if (visibility != TemplateSettings.OverflowButtonVisibility)
		{
			GetTemplateSettings().OverflowButtonVisibility = visibility;
		}
	}

	private void SelectOverflowItem(object item, IndexPath ip)
	{
		object GetItemBeingMoved(object item, IndexPath ip)
		{
			if (ip.GetSize() > 2)
			{
				return GetItemFromIndex(m_topNavRepeaterOverflowView, m_topDataProvider.ConvertOriginalIndexToIndex(ip.GetAt(1)));
			}
			return item;
		}
		var itemBeingMoved = GetItemBeingMoved(item, ip);

		// Calculate selected overflow item size.
		var selectedOverflowItemIndex = m_topDataProvider.IndexOf(itemBeingMoved);
		MUX_ASSERT(selectedOverflowItemIndex != itemNotFound);
		var selectedOverflowItemWidth = m_topDataProvider.GetWidthForItem(selectedOverflowItemIndex);

		bool needInvalidMeasure = !m_topDataProvider.IsValidWidthForItem(selectedOverflowItemIndex);

		if (!needInvalidMeasure)
		{
			var actualWidth = GetTopNavigationViewActualWidth();
			var desiredWidth = MeasureTopNavigationViewDesiredWidth(c_infSize);
			// This assert triggers on the InfoBadge page, however it seems to recover fine, disabling the assert for now.
			// Github issue: https://github.com/microsoft/microsoft-ui-xaml/issues/5771
			// MUX_ASSERT(desiredWidth <= actualWidth);

			// Calculate selected item size
			var selectedItemIndex = itemNotFound;
			var selectedItemWidth = 0.0;
			var selectedItem = SelectedItem;
			if (selectedItem != null)
			{
				selectedItemIndex = m_topDataProvider.IndexOf(selectedItem);
				if (selectedItemIndex != itemNotFound)
				{
					selectedItemWidth = m_topDataProvider.GetWidthForItem(selectedItemIndex);
				}
			}

			var widthAtLeastToBeRemoved = desiredWidth + selectedOverflowItemWidth - actualWidth;

			// calculate items to be removed from primary because a overflow item is selected.
			// SelectedItem is assumed to be removed from primary first, then added it back if it should not be removed
			var itemsToBeRemoved = FindMovableItemsToBeRemovedFromPrimaryList(widthAtLeastToBeRemoved, Array.Empty<int>() /*excludeItems*/);

			// calculate the size to be removed
			var toBeRemovedItemWidth = m_topDataProvider.CalculateWidthForItems(itemsToBeRemoved);

			var widthAvailableToRecover = toBeRemovedItemWidth - widthAtLeastToBeRemoved;
			var itemsToBeAdded = FindMovableItemsRecoverToPrimaryList(widthAvailableToRecover, new int[] { selectedOverflowItemIndex }/*includeItems*/);

			CollectionHelper.UniquePushBack(itemsToBeAdded, selectedOverflowItemIndex);

			// Keep track of the item being moved in order to know where to animate selection indicator
			m_lastSelectedItemPendingAnimationInTopNav = itemBeingMoved;
			if (ip != null && ip.GetSize() > 0)
			{
				foreach (var it in itemsToBeRemoved)
				{
					if (it == ip.GetAt(1))
					{
						var indicator = m_activeIndicator;
						if (indicator != null)
						{
							// If the previously selected item is being moved into overflow, hide its indicator
							// as we will no longer need to animate from its location.
							AnimateSelectionChanged(null);
						}
						break;
					}
				}
			}

			if (m_topDataProvider.HasInvalidWidth(itemsToBeAdded))
			{
				needInvalidMeasure = true;
			}
			else
			{
				// Exchange items between Primary and Overflow
				{
					m_topDataProvider.MoveItemsToPrimaryList(itemsToBeAdded);
					m_topDataProvider.MoveItemsOutOfPrimaryList(itemsToBeRemoved);
				}

				if (NeedRearrangeOfTopElementsAfterOverflowSelectionChanged(selectedOverflowItemIndex))
				{
					needInvalidMeasure = true;
				}

				if (!needInvalidMeasure)
				{
					SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(item);
					InvalidateMeasure();
				}
			}
		}

		// TODO: Verify that this is no longer needed and delete
		if (needInvalidMeasure)
		{
			// not all items have known width, need to redo the layout
			m_topDataProvider.MoveAllItemsToPrimaryList();
			SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(item);
			InvalidateTopNavPrimaryLayout();
		}
	}

	private bool NeedRearrangeOfTopElementsAfterOverflowSelectionChanged(int selectedOriginalIndex)
	{
		bool needRearrange = false;

		var primaryList = m_topDataProvider.GetPrimaryItems();
		var primaryListSize = primaryList.Count;
		var indexInPrimary = m_topDataProvider.ConvertOriginalIndexToIndex(selectedOriginalIndex);
		// We need to verify that through various overflow selection combinations, the primary
		// items have not been put into a state of non-logical item layout (aka not in proper sequence).
		// To verify this, if the newly selected item has items following it in the primary items:
		// - we verify that they are meant to follow the selected item as specified in the original order
		// - we verify that the preceding item is meant to directly precede the selected item in the original order
		// If these two conditions are not met, we move all items to the primary list and trigger a re-arrangement of the items.
		if (indexInPrimary < (int)(primaryListSize - 1))
		{
			var nextIndexInPrimary = indexInPrimary + 1;
			var nextIndexInOriginal = selectedOriginalIndex + 1;
			var prevIndexInOriginal = selectedOriginalIndex - 1;

			// Check whether item preceding the selected is not directly preceding
			// in the original.
			if (indexInPrimary > 0)
			{
				List<int> prevIndexInVector = new List<int>();
				prevIndexInVector.Add(nextIndexInPrimary - 1);
				var prevOriginalIndexOfPrevPrimaryItem = m_topDataProvider.ConvertPrimaryIndexToIndex(prevIndexInVector);
				if (prevOriginalIndexOfPrevPrimaryItem[0] != prevIndexInOriginal)
				{
					needRearrange = true;
				}
			}


			// Check whether items following the selected item are out of order
			while (!needRearrange && nextIndexInPrimary < (int)(primaryListSize))
			{
				List<int> nextIndexInVector = new List<int>();
				nextIndexInVector.Add(nextIndexInPrimary);
				var originalIndex = m_topDataProvider.ConvertPrimaryIndexToIndex(nextIndexInVector);
				if (nextIndexInOriginal != originalIndex[0])
				{
					needRearrange = true;
					break;
				}
				nextIndexInPrimary++;
				nextIndexInOriginal++;
			}
		}

		return needRearrange;
	}

	private void ShrinkTopNavigationSize(double desiredWidth, Size availableSize)
	{
		UpdateTopNavigationWidthCache();

		var selectedItemIndex = GetSelectedItemIndex();

		var possibleWidthForPrimaryList = MeasureTopNavMenuItemsHostDesiredWidth(c_infSize) - (desiredWidth - availableSize.Width);
		if (possibleWidthForPrimaryList >= 0)
		{
			// Remove all items which is not visible except first item and selected item.
			var itemToBeRemoved = FindMovableItemsBeyondAvailableWidth(possibleWidthForPrimaryList);
			// should keep at least one item in primary
			KeepAtLeastOneItemInPrimaryList(itemToBeRemoved, true/*shouldKeepFirst*/);
			m_topDataProvider.MoveItemsOutOfPrimaryList(itemToBeRemoved);
		}

		// measure again to make sure SelectedItem is realized
		desiredWidth = MeasureTopNavigationViewDesiredWidth(c_infSize);

		var widthAtLeastToBeRemoved = desiredWidth - availableSize.Width;
		if (widthAtLeastToBeRemoved > 0)
		{
			var itemToBeRemoved = FindMovableItemsToBeRemovedFromPrimaryList(widthAtLeastToBeRemoved, new int[] { selectedItemIndex });

			// At least one item is kept on primary list
			KeepAtLeastOneItemInPrimaryList(itemToBeRemoved, false/*shouldKeepFirst*/);

			// There should be no item is virtualized in this step
			MUX_ASSERT(!m_topDataProvider.HasInvalidWidth(itemToBeRemoved));
			m_topDataProvider.MoveItemsOutOfPrimaryList(itemToBeRemoved);
		}
	}

	private IList<int> FindMovableItemsRecoverToPrimaryList(double availableWidth, IList<int> includeItems)
	{
		List<int> toBeMoved = new List<int>();

		var size = m_topDataProvider.Size();

		// Included Items take high priority, all of them are included in recovery list
		foreach (var index in includeItems)
		{
			var width = m_topDataProvider.GetWidthForItem(index);
			toBeMoved.Add(index);
			availableWidth -= width;
		}

		int i = 0;
		while (i < size && availableWidth > 0)
		{
			if (!m_topDataProvider.IsItemInPrimaryList(i) && !includeItems.Contains(i))
			{
				var width = m_topDataProvider.GetWidthForItem(i);
				if (availableWidth >= width)
				{
					toBeMoved.Add(i);
					availableWidth -= width;
				}
				else
				{
					break;
				}
			}
			i++;
		}
		// Keep at one item is not in primary list. Two possible reason:
		//  1, Most likely it's caused by m_topNavigationRecoveryGracePeriod
		//  2, virtualization and it doesn't have cached width
		if (i == size && toBeMoved.Count > 0)
		{
			toBeMoved.Remove(toBeMoved.Count - 1);
		}
		return toBeMoved;
	}

	private IList<int> FindMovableItemsToBeRemovedFromPrimaryList(double widthAtLeastToBeRemoved, IList<int> excludeItems)
	{
		List<int> toBeMoved = new List<int>();

		int i = m_topDataProvider.Size() - 1;
		while (i >= 0 && widthAtLeastToBeRemoved > 0)
		{
			if (m_topDataProvider.IsItemInPrimaryList(i))
			{
				if (!excludeItems.Contains(i))
				{
					var width = m_topDataProvider.GetWidthForItem(i);
					toBeMoved.Add(i);
					widthAtLeastToBeRemoved -= width;
				}
			}
			i--;
		}

		return toBeMoved;
	}

	private IList<int> FindMovableItemsBeyondAvailableWidth(double availableWidth)
	{
		List<int> toBeMoved = new List<int>();
		var ir = m_topNavRepeater;
		if (ir != null)
		{
			int selectedItemIndexInPrimary = m_topDataProvider.IndexOf(SelectedItem, NavigationViewSplitVectorID.PrimaryList);
			int size = m_topDataProvider.GetPrimaryListSize();

			double requiredWidth = 0;

			for (int i = 0; i < size; i++)
			{
				if (i != selectedItemIndexInPrimary)
				{
					bool shouldMove = true;
					if (requiredWidth <= availableWidth)
					{
						var container = ir.TryGetElement(i);
						if (container != null)
						{
							if (container is UIElement containerAsUIElement)
							{
								var width = containerAsUIElement.DesiredSize.Width;
								requiredWidth += width;
								shouldMove = requiredWidth > availableWidth;
							}
						}
						else
						{
							// item is virtualized but not realized.
						}
					}

					if (shouldMove)
					{
						toBeMoved.Add(i);
					}
				}
			}
		}

		return m_topDataProvider.ConvertPrimaryIndexToIndex(toBeMoved);
	}

	private void KeepAtLeastOneItemInPrimaryList(IList<int> itemInPrimaryToBeRemoved, bool shouldKeepFirst)
	{
		if (itemInPrimaryToBeRemoved.Count > 0 && (int)(itemInPrimaryToBeRemoved.Count) == m_topDataProvider.GetPrimaryListSize())
		{
			if (shouldKeepFirst)
			{
				itemInPrimaryToBeRemoved.RemoveAt(0);
			}
			else
			{
				itemInPrimaryToBeRemoved.RemoveAt(itemInPrimaryToBeRemoved.Count - 1);
			}
		}
	}

	private int GetSelectedItemIndex()
	{
		return m_topDataProvider.IndexOf(SelectedItem);
	}

	private double GetPaneToggleButtonWidth()
	{
		return Convert.ToDouble(SharedHelpers.FindInApplicationResources("PaneToggleButtonWidth", c_paneToggleButtonWidth), CultureInfo.InvariantCulture);
	}

	private double GetPaneToggleButtonHeight()
	{
		return Convert.ToDouble(SharedHelpers.FindInApplicationResources("PaneToggleButtonHeight", c_paneToggleButtonHeight), CultureInfo.InvariantCulture);
	}

	private void UpdateTopNavigationWidthCache()
	{
		int size = m_topDataProvider.GetPrimaryListSize();
		var ir = m_topNavRepeater;
		if (ir != null)
		{
			for (int i = 0; i < size; i++)
			{
				var container = ir.TryGetElement(i);
				if (container != null)
				{
					if (container is UIElement containerAsUIElement)
					{
						var width = containerAsUIElement.DesiredSize.Width;
						m_topDataProvider.UpdateWidthForPrimaryItem(i, width);
					}
				}
				else
				{
					break;
				}
			}
		}
	}

	private bool IsTopNavigationView()
	{
		return PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
	}

	private bool IsTopPrimaryListVisible()
	{
		return m_topNavRepeater != null && (TemplateSettings.TopPaneVisibility == Visibility.Visible);
	}

	private void CoerceToGreaterThanZero(ref double value)
	{
		// Property coercion for OpenPaneLength, CompactPaneLength, CompactModeThresholdWidth, ExpandedModeThresholdWidth
		value = Math.Max(value, 0.0);
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty property = args.Property;

		if (property == IsPaneOpenProperty)
		{
			OnIsPaneOpenChanged();
			UpdateVisualStateForDisplayModeGroup(DisplayMode);
		}
		else if (property == CompactModeThresholdWidthProperty ||
			property == ExpandedModeThresholdWidthProperty)
		{
			UpdateAdaptiveLayout(ActualWidth);
		}
		else if (property == AlwaysShowHeaderProperty || property == HeaderProperty)
		{
			UpdateHeaderVisibility();
		}
		else if (property == SelectedItemProperty)
		{
			OnSelectedItemPropertyChanged(args);
		}
		else if (property == PaneTitleProperty)
		{
			UpdatePaneTitleFrameworkElementParents();
			UpdateBackAndCloseButtonsVisibility();
			UpdatePaneToggleSize();
		}
		else if (property == IsBackButtonVisibleProperty)
		{
			UpdateBackAndCloseButtonsVisibility();
			UpdateAdaptiveLayout(ActualWidth);
			if (IsTopNavigationView())
			{
				InvalidateTopNavPrimaryLayout();
			}

			//if (g_IsTelemetryProviderEnabled && IsBackButtonVisible == NavigationViewBackButtonVisible.Collapsed)
			//{
			//  Explicitly disabling BackUI on NavigationView
			//[[gsl.suppress(con.4)]] TraceLoggingWrite(
			//	g_hTelemetryProvider,
			//	"NavigationView_DisableBackUI",
			//	TraceLoggingDescription("Developer explicitly disables the BackUI on NavigationView"));
			//}
			// Enabling back button shifts grid instead of resizing, so let's update the layout.
			var backButton = m_backButton;
			if (backButton != null)
			{
				backButton.UpdateLayout();
			}
			UpdatePaneLayout();
		}

		else if (property == MenuItemsSourceProperty)
		{
			UpdateRepeaterItemsSource(true /*forceSelectionModelUpdate*/);
		}
		else if (property == MenuItemsProperty)
		{
			UpdateRepeaterItemsSource(true /*forceSelectionModelUpdate*/);
		}
		else if (property == FooterMenuItemsSourceProperty)
		{
			UpdateFooterRepeaterItemsSource(true /*sourceCollectionReset*/, true /*sourceCollectionChanged*/);
		}
		else if (property == FooterMenuItemsProperty)
		{
			UpdateFooterRepeaterItemsSource(true /*sourceCollectionReset*/, true /*sourceCollectionChanged*/);
		}
		else if (property == PaneDisplayModeProperty)
		{
			// m_wasForceClosed is set to true because ToggleButton is clicked and Pane is closed.
			// When PaneDisplayMode is changed, reset the force flag to make the Pane can be opened automatically again.
			m_wasForceClosed = false;

			CollapseTopLevelMenuItems((NavigationViewPaneDisplayMode)args.OldValue);
			UpdatePaneToggleButtonVisibility();
			UpdatePaneDisplayMode((NavigationViewPaneDisplayMode)args.OldValue, (NavigationViewPaneDisplayMode)args.NewValue);
			UpdatePaneTitleFrameworkElementParents();
			UpdatePaneVisibility();
			UpdateVisualState();
			UpdatePaneButtonsWidths();
		}
		else if (property == IsPaneVisibleProperty)
		{
			UpdatePaneVisibility();
			UpdateVisualStateForDisplayModeGroup(DisplayMode);

			// When NavView is in expaneded mode with fixed window size, setting IsPaneVisible to false doesn't closes the pane
			// We manually close/open it for this case
			if (!IsPaneVisible && IsPaneOpen)
			{
				ClosePane();
			}

			if (IsPaneVisible && DisplayMode == NavigationViewDisplayMode.Expanded && !IsPaneOpen)
			{
				OpenPane();
			}
		}
		else if (property == OverflowLabelModeProperty)
		{
			if (m_appliedTemplate)
			{
				UpdateVisualStateForOverflowButton();
				InvalidateTopNavPrimaryLayout();
			}
		}
		else if (property == AutoSuggestBoxProperty)
		{
			InvalidateTopNavPrimaryLayout();
			if (args.OldValue != null)
			{
				m_autoSuggestBoxQuerySubmittedRevoker.Disposable = null;
			}
			if (args.NewValue is AutoSuggestBox newAutoSuggestBox)
			{
				newAutoSuggestBox.QuerySubmitted += OnAutoSuggestBoxQuerySubmitted;
				m_autoSuggestBoxQuerySubmittedRevoker.Disposable = Disposable.Create(() => newAutoSuggestBox.QuerySubmitted -= OnAutoSuggestBoxQuerySubmitted);
			}
			UpdateVisualState(false);
		}
		else if (property == SelectionFollowsFocusProperty)
		{
			UpdateSingleSelectionFollowsFocusTemplateSetting();
		}
		else if (property == IsPaneToggleButtonVisibleProperty)
		{
			UpdatePaneTitleFrameworkElementParents();
			UpdateBackAndCloseButtonsVisibility();
			UpdatePaneToggleButtonVisibility();
			UpdateTitleBarPadding();
			UpdateVisualState();
		}
		else if (property == IsSettingsVisibleProperty)
		{
			UpdateFooterRepeaterItemsSource(false /*sourceCollectionReset*/, true /*sourceCollectionChanged*/);
		}
		else if (property == CompactPaneLengthProperty)
		{
			if (!SharedHelpers.Is21H1OrHigher())
			{
				// Need to update receiver margins when CompactPaneLength changes
				UpdatePaneShadow();
			}

			// Update pane-button-grid width when pane is closed and we are not in minimal
			UpdatePaneButtonsWidths();
		}
		else if (property == IsTitleBarAutoPaddingEnabledProperty)
		{
			UpdateTitleBarPadding();
		}
		else if (property == MenuItemTemplateProperty ||
			property == MenuItemTemplateSelectorProperty)
		{
			SyncItemTemplates();
		}
		else if (property == PaneFooterProperty)
		{
			UpdatePaneLayout();
		}
		else if (property == OpenPaneLengthProperty)
		{
			UpdateOpenPaneLength(ActualWidth);
		}
	}

	private void UpdateNavigationViewItemsFactory()
	{
		object newItemTemplate = MenuItemTemplate;
		if (newItemTemplate == null)
		{
			newItemTemplate = MenuItemTemplateSelector;
		}
		m_navigationViewItemsFactory.UserElementFactory(newItemTemplate);
	}

	private void SyncItemTemplates()
	{
		UpdateNavigationViewItemsFactory();
	}

	private void OnRepeaterLoaded(object sender, RoutedEventArgs args)
	{
		var item = SelectedItem;
		if (item != null)
		{
			if (!IsSelectionSuppressed(item))
			{
				var navViewItem = NavigationViewItemOrSettingsContentFromData(item);
				if (navViewItem != null)
				{
					navViewItem.IsSelected = true;
				}
			}
			AnimateSelectionChanged(item);
		}
	}

	// If app is .net app, the lifetime of NavigationView maybe depends on garbage collection.
	// Unlike other revoker, TitleBar is in global space and we need to stop receiving changed event when it's unloaded.
	// So we do hook it in Loaded and Unhook it in Unloaded
	private void OnUnloaded(object sender, RoutedEventArgs args)
	{
		var coreTitleBar = m_coreTitleBar;
		if (coreTitleBar != null)
		{
			coreTitleBar.LayoutMetricsChanged -= OnTitleBarMetricsChanged;
			coreTitleBar.IsVisibleChanged -= OnTitleBarIsVisibleChanged;
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		if (m_updateVisualStateForDisplayModeFromOnLoaded)
		{
			m_updateVisualStateForDisplayModeFromOnLoaded = false;
			UpdateVisualStateForDisplayModeGroup(DisplayMode);
		}

		var coreTitleBar = m_coreTitleBar;
		if (coreTitleBar != null)
		{
			coreTitleBar.LayoutMetricsChanged += OnTitleBarMetricsChanged;
			coreTitleBar.IsVisibleChanged += OnTitleBarIsVisibleChanged;
		}
		// Update pane buttons now since we the CompactPaneLength is actually known now.
		UpdatePaneButtonsWidths();
	}

	private void OnIsPaneOpenChanged()
	{
		var isPaneOpen = IsPaneOpen;
		if (isPaneOpen && m_wasForceClosed)
		{
			m_wasForceClosed = false; // remove the pane open flag since Pane is opened.
		}
		else if (!m_isOpenPaneForInteraction && !isPaneOpen)
		{
			var splitView = m_rootSplitView;
			if (splitView != null)
			{
				// splitview.IsPaneOpen and nav.IsPaneOpen is two way binding. If nav.IsPaneOpen=false and splitView.IsPaneOpen=true,
				// then the pane has been closed by API and we treat it as a forced close.
				// If, however, splitView.IsPaneOpen=false, then nav.IsPaneOpen is just following the SplitView here and the pane
				// was closed, for example, due to app window resizing. We don't set the force flag in this situation.
				m_wasForceClosed = splitView.IsPaneOpen;
			}

			else
			{
				// If there is no SplitView (for example it hasn't been loaded yet) then nav.IsPaneOpen was set directly
				// so we treat it as a closed force.
				m_wasForceClosed = true;
			}
		}

		SetPaneToggleButtonAutomationName();
		UpdatePaneTabFocusNavigation();
		UpdateSettingsItemToolTip();
		UpdatePaneTitleFrameworkElementParents();
		UpdatePaneOverlayGroup();
		UpdatePaneButtonsWidths();

		if (SharedHelpers.IsThemeShadowAvailable())
		{
			// Drop Shadows were only introduced in OS versions 21h1 or higher. Projected Shadows will be used for older versions.
			if (SharedHelpers.Is21H1OrHigher())
			{
				if (IsPaneOpen)
				{
					SetDropShadow();
				}
				else
				{
					UnsetDropShadow();
				}
			}
			else
			{
				if (m_rootSplitView is { } splitView)
				{
					var displayMode = splitView.DisplayMode;
					var isOverlay = displayMode == SplitViewDisplayMode.Overlay || displayMode == SplitViewDisplayMode.CompactOverlay;
					if (splitView.Pane is { } paneRoot)
					{
						var currentTranslation = paneRoot.Translation;
						var translation = new Vector3(currentTranslation.X, currentTranslation.Y, IsPaneOpen && isOverlay ? c_paneElevationTranslationZ : 0.0f);
						paneRoot.Translation = translation;
					}
				}
			}
		}
	}

	private void UpdatePaneToggleButtonVisibility()
	{
		var visible = IsPaneToggleButtonVisible && !IsTopNavigationView();
		GetTemplateSettings().PaneToggleButtonVisibility = Util.VisibilityFromBool(visible);
	}

	private void UpdatePaneDisplayMode()
	{
		if (!m_appliedTemplate)
		{
			return;
		}
		if (!IsTopNavigationView())
		{
			UpdateAdaptiveLayout(ActualWidth, true /*forceSetDisplayMode*/);

			SwapPaneHeaderContent(m_leftNavPaneHeaderContentBorder, m_paneHeaderOnTopPane, "PaneHeader");
			SwapPaneHeaderContent(m_leftNavPaneCustomContentBorder, m_paneCustomContentOnTopPane, "PaneCustomContent");
			SwapPaneHeaderContent(m_leftNavFooterContentBorder, m_paneFooterOnTopPane, "PaneFooter");

			CreateAndHookEventsToSettings();

			//if (UIElement8 thisAsUIElement8 = this)
			{
				var paneToggleButton = m_paneToggleButton;
				if (paneToggleButton != null)
				{
					KeyTipTarget = paneToggleButton;
				}
			}

		}

		else
		{
			ClosePane();
			SetDisplayMode(NavigationViewDisplayMode.Minimal, true);

			SwapPaneHeaderContent(m_paneHeaderOnTopPane, m_leftNavPaneHeaderContentBorder, "PaneHeader");
			SwapPaneHeaderContent(m_paneCustomContentOnTopPane, m_leftNavPaneCustomContentBorder, "PaneCustomContent");
			SwapPaneHeaderContent(m_paneFooterOnTopPane, m_leftNavFooterContentBorder, "PaneFooter");

			CreateAndHookEventsToSettings();

			//if (UIElement8 thisAsUIElement8 = this)
			{
				var topNavOverflowButton = m_topNavOverflowButton;
				if (topNavOverflowButton != null)
				{
					KeyTipTarget = topNavOverflowButton;
				}
			}
		}

		UpdateContentBindingsForPaneDisplayMode();
		UpdateRepeaterItemsSource(false /*forceSelectionModelUpdate*/);
		UpdateFooterRepeaterItemsSource(false /*sourceCollectionReset*/, false /*sourceCollectionChanged*/);
		var selectedItem = SelectedItem;
		if (selectedItem != null)
		{
			m_OrientationChangedPendingAnimation = true;
		}
	}

	private void UpdatePaneDisplayMode(NavigationViewPaneDisplayMode oldDisplayMode, NavigationViewPaneDisplayMode newDisplayMode)
	{
		if (!m_appliedTemplate)
		{
			return;
		}

		UpdatePaneDisplayMode();

		// For better user experience, We help customer to Open/Close Pane automatically when we switch between LeftMinimal <-> Left.
		// From other navigation PaneDisplayMode to LeftMinimal, we expect pane is closed.
		// From LeftMinimal to Left, it is expected the pane is open. For other configurations, this seems counterintuitive.
		// See #1702 and #1787
		if (!IsTopNavigationView())
		{
			// In rare cases it is possible to end up in a state where two calls to OnPropertyChanged for PaneDisplayMode can end up on the stack
			// Calls above to UpdatePaneDisplayMode() can result in further property updates.
			// As a result of this reentrancy, we can end up with an incorrect result for IsPaneOpen as the later OnPropertyChanged for PaneDisplayMode
			// will complete during the OnPropertyChanged of the earlier one. 
			// To avoid this, we only call OpenPane()/ClosePane() if PaneDisplayMode has not changed.
			if (newDisplayMode == PaneDisplayMode)
			{
				if (IsPaneOpen)
				{
					if (newDisplayMode == NavigationViewPaneDisplayMode.LeftMinimal)
					{
						ClosePane();
					}
				}
				else
				{
					if (oldDisplayMode == NavigationViewPaneDisplayMode.LeftMinimal
						&& newDisplayMode == NavigationViewPaneDisplayMode.Left)
					{
						OpenPane();
					}
				}
			}
		}
	}

	private void UpdatePaneVisibility()
	{
		var templateSettings = GetTemplateSettings();
		if (IsPaneVisible)
		{
			if (IsTopNavigationView())
			{
				templateSettings.LeftPaneVisibility = Visibility.Collapsed;
				templateSettings.TopPaneVisibility = Visibility.Visible;
			}
			else
			{
				templateSettings.TopPaneVisibility = Visibility.Collapsed;
				templateSettings.LeftPaneVisibility = Visibility.Visible;
			}

			VisualStateManager.GoToState(this, "PaneVisible", false /*useTransitions*/);
		}
		else
		{
			templateSettings.TopPaneVisibility = Visibility.Collapsed;
			templateSettings.LeftPaneVisibility = Visibility.Collapsed;

			VisualStateManager.GoToState(this, "PaneCollapsed", false /*useTransitions*/);
		}
	}

	private void SwapPaneHeaderContent(ContentControl newParentTrackRef, ContentControl oldParentTrackRef, string propertyPathName)
	{
		var newParent = newParentTrackRef;
		if (newParent != null)
		{
			var oldParent = oldParentTrackRef;
			if (oldParent != null)
			{
				oldParent.ClearValue(ContentControl.ContentProperty);
			}

			SharedHelpers.SetBinding(propertyPathName, newParent, ContentControl.ContentProperty);
		}
	}

	private void UpdateContentBindingsForPaneDisplayMode()
	{
		UIElement autoSuggestBoxContentControl = null;
		UIElement notControl = null;
		if (!IsTopNavigationView())
		{
			autoSuggestBoxContentControl = m_leftNavPaneAutoSuggestBoxPresenter;
			notControl = m_topNavPaneAutoSuggestBoxPresenter;
		}
		else
		{
			autoSuggestBoxContentControl = m_topNavPaneAutoSuggestBoxPresenter;
			notControl = m_leftNavPaneAutoSuggestBoxPresenter;
		}

		if (autoSuggestBoxContentControl != null)
		{
			if (notControl != null)
			{
				notControl.ClearValue(ContentControl.ContentProperty);
			}

			SharedHelpers.SetBinding("AutoSuggestBox", autoSuggestBoxContentControl, ContentControl.ContentProperty);
		}
	}

	private void UpdateHeaderVisibility()
	{
		if (!m_appliedTemplate)
		{
			return;
		}

		UpdateHeaderVisibility(DisplayMode);
	}

	private void UpdateHeaderVisibility(NavigationViewDisplayMode displayMode)
	{
		// Ignore AlwaysShowHeader property in case DisplayMode is Minimal and it's not Top NavigationView
		bool showHeader = AlwaysShowHeader || (!IsTopNavigationView() && displayMode == NavigationViewDisplayMode.Minimal);

		// Like bug 17517627, Customer like WallPaper Studio 10 expects a HeaderContent visual even if Header() is null.
		// App crashes when they have dependency on that visual, but the crash is not directly state that it's a header problem.
		// NavigationView doesn't use quirk, but we determine the version by themeresource.
		// As a workaround, we 'quirk' it for RS4 or before release. if it's RS4 or before, HeaderVisible is not related to Header().
		// If theme resource is RS5 or later, we will not show header if header is null.
		if (SharedHelpers.IsRS5OrHigher())
		{
			showHeader = Header != null && showHeader;
		}
		VisualStateManager.GoToState(this, showHeader ? "HeaderVisible" : "HeaderCollapsed", false /*useTransitions*/);
	}

	void UpdatePaneTabFocusNavigation()
	{
		if (!m_appliedTemplate)
		{
			return;
		}

		if (SharedHelpers.IsRS2OrHigher())
		{
			KeyboardNavigationMode mode = KeyboardNavigationMode.Local;

			var splitView = m_rootSplitView;
			if (splitView != null)
			{
				// If the pane is open in an overlay (light-dismiss) mode, trap keyboard focus inside the pane
				if (IsPaneOpen && (splitView.DisplayMode == SplitViewDisplayMode.Overlay || splitView.DisplayMode == SplitViewDisplayMode.CompactOverlay))
				{
					mode = KeyboardNavigationMode.Cycle;
				}
			}

			var paneContentGrid = m_paneContentGrid;
			if (paneContentGrid != null)
			{
				paneContentGrid.TabFocusNavigation = mode;
			}
		}
	}

	private void UpdatePaneToggleSize()
	{
		if (!ShouldPreserveNavigationViewRS3Behavior())
		{
			var splitView = m_rootSplitView;
			if (splitView != null)
			{
				double width = GetTemplateSettings().PaneToggleButtonWidth;
				double togglePaneButtonWidth = width;

				if (ShouldShowBackButton() && splitView.DisplayMode == SplitViewDisplayMode.Overlay)
				{
					double backButtonWidth = c_backButtonWidth;
					var backButton = m_backButton;
					if (backButton != null)
					{
						backButtonWidth = backButton.Width;
					}

					width += backButtonWidth;
				}

				if (!m_isClosedCompact && !string.IsNullOrEmpty(PaneTitle))
				{
					if (splitView.DisplayMode == SplitViewDisplayMode.Overlay && IsPaneOpen)
					{
						width = m_openPaneLength;
						togglePaneButtonWidth = m_openPaneLength - ((ShouldShowBackButton() || ShouldShowCloseButton()) ? c_backButtonWidth : 0);
					}
					else if (!(splitView.DisplayMode == SplitViewDisplayMode.Overlay && !IsPaneOpen))
					{
						width = m_openPaneLength;
						togglePaneButtonWidth = m_openPaneLength;
					}
				}

				var toggleButton = m_paneToggleButton;
				if (toggleButton != null)
				{
					toggleButton.Width = togglePaneButtonWidth;
				}
			}
		}
	}

	private void UpdateBackAndCloseButtonsVisibility()
	{
		if (!m_appliedTemplate)
		{
			return;
		}

		var shouldShowBackButton = ShouldShowBackButton();
		var backButtonVisibility = Util.VisibilityFromBool(shouldShowBackButton);
		var visualStateDisplayMode = GetVisualStateDisplayMode(DisplayMode);
		bool useLeftPaddingForBackOrCloseButton =
		   (visualStateDisplayMode == NavigationViewVisualStateDisplayMode.Minimal && !IsTopNavigationView()) ||
		   visualStateDisplayMode == NavigationViewVisualStateDisplayMode.MinimalWithBackButton;
		double leftPaddingForBackOrCloseButton = 0.0;
		double paneHeaderPaddingForToggleButton = 0.0;
		double paneHeaderPaddingForCloseButton = 0.0;
		double paneHeaderContentBorderRowMinHeight = 0.0;

		GetTemplateSettings().BackButtonVisibility = backButtonVisibility;

		if (m_paneToggleButton != null && IsPaneToggleButtonVisible)
		{
			paneHeaderContentBorderRowMinHeight = GetPaneToggleButtonHeight();
			paneHeaderPaddingForToggleButton = GetPaneToggleButtonWidth();

			if (useLeftPaddingForBackOrCloseButton)
			{
				leftPaddingForBackOrCloseButton = paneHeaderPaddingForToggleButton;
			}
		}

		var backButton = m_backButton;
		if (backButton != null)
		{
			if (ShouldPreserveNavigationViewRS4Behavior())
			{
				backButton.Visibility = backButtonVisibility;
			}

			if (useLeftPaddingForBackOrCloseButton && backButtonVisibility == Visibility.Visible)
			{
				leftPaddingForBackOrCloseButton += backButton.Width;
			}
		}

		var closeButton = m_closeButton;
		if (closeButton != null)
		{
			var closeButtonVisibility = Util.VisibilityFromBool(ShouldShowCloseButton());

			closeButton.Visibility = closeButtonVisibility;

			if (closeButtonVisibility == Visibility.Visible)
			{
				paneHeaderContentBorderRowMinHeight = Math.Max(paneHeaderContentBorderRowMinHeight, closeButton.Height);

				if (useLeftPaddingForBackOrCloseButton)
				{
					paneHeaderPaddingForCloseButton = closeButton.Width;
					leftPaddingForBackOrCloseButton += paneHeaderPaddingForCloseButton;
				}
			}
		}

		var contentLeftPadding = m_contentLeftPadding;
		if (contentLeftPadding != null)
		{
			contentLeftPadding.Width = leftPaddingForBackOrCloseButton;
		}

		var paneHeaderToggleButtonColumn = m_paneHeaderToggleButtonColumn;
		if (paneHeaderToggleButtonColumn != null)
		{
			// Account for the PaneToggleButton's width in the PaneHeader's placement.
			paneHeaderToggleButtonColumn.Width = GridLengthHelper.FromValueAndType(paneHeaderPaddingForToggleButton, GridUnitType.Pixel);
		}

		var paneHeaderCloseButtonColumn = m_paneHeaderCloseButtonColumn;
		if (paneHeaderCloseButtonColumn != null)
		{
			// Account for the CloseButton's width in the PaneHeader's placement.
			paneHeaderCloseButtonColumn.Width = GridLengthHelper.FromValueAndType(paneHeaderPaddingForCloseButton, GridUnitType.Pixel);
		}

		var paneHeaderContentBorderRow = m_paneHeaderContentBorderRow;
		if (paneHeaderContentBorderRow != null)
		{
			paneHeaderContentBorderRow.MinHeight = paneHeaderContentBorderRowMinHeight;

#if IS_UNO
			SetHeaderContentMinHeight(paneHeaderContentBorderRowMinHeight);
#endif
		}

		var paneContentGridAsUIE = m_paneContentGrid;
		if (paneContentGridAsUIE != null)
		{
			if (paneContentGridAsUIE is Grid paneContentGrid)
			{
				var rowDefs = paneContentGrid.RowDefinitions;

				if (rowDefs.Count >= c_backButtonRowDefinition)
				{
					var rowDef = rowDefs[c_backButtonRowDefinition];

					int backButtonRowHeight = 0;
					if (!IsOverlay() && shouldShowBackButton)
					{
						backButtonRowHeight = c_backButtonHeight;
					}
					else if (ShouldPreserveNavigationViewRS3Behavior())
					{
						// This row represented the height of the hamburger+margin in RS3 and prior
						backButtonRowHeight = c_toggleButtonHeightWhenShouldPreserveNavigationViewRS3Behavior;
					}

					var length = GridLengthHelper.FromPixels(backButtonRowHeight);
					rowDef.Height = length;
				}
			}
		}

		if (!ShouldPreserveNavigationViewRS4Behavior())
		{
			VisualStateManager.GoToState(this, shouldShowBackButton ? "BackButtonVisible" : "BackButtonCollapsed", false /*useTransitions*/);
		}
		UpdateTitleBarPadding();
	}

	private void UpdatePaneTitleMargins()
	{
		if (ShouldPreserveNavigationViewRS4Behavior())
		{
			var paneTitleFrameworkElement = m_paneTitleFrameworkElement;
			if (paneTitleFrameworkElement != null)
			{
				double width = GetPaneToggleButtonWidth();

				if (ShouldShowBackButton() && IsOverlay())
				{
					width += c_backButtonWidth;
				}

				paneTitleFrameworkElement.Margin = new Thickness(width, 0, 0, 0); // see "Hamburger title" on uni
			}
		}
	}

	private void UpdateSelectionForMenuItems()
	{
		// Allow customer to set selection by NavigationViewItem.IsSelected.
		// If there are more than two items are set IsSelected=true, the first one is actually selected.
		// If SelectedItem is set, IsSelected is ignored.
		//         <MenuItems>
		//              <NavigationViewItem Content = "Collection" IsSelected = "True" / >
		//         </MenuItems>
		if (SelectedItem == null)
		{
			bool foundFirstSelected = false;

			// firstly check Menu items
			if (MenuItems is IList<object> menuItems)
			{
				foundFirstSelected = UpdateSelectedItemFromMenuItems(menuItems);
			}

			// then do same for footer items and tell wenever selected item alreadyfound in MenuItems
			if (FooterMenuItems is IList<object> footerItems)
			{
				UpdateSelectedItemFromMenuItems(footerItems, foundFirstSelected);
			}
		}
	}

	private bool UpdateSelectedItemFromMenuItems(IList<object> menuItems, bool foundFirstSelected = false)
	{
		for (int i = 0; i < (int)(menuItems.Count); i++)
		{
			if (menuItems[i] is NavigationViewItem item)
			{
				if (item.IsSelected)
				{
					if (!foundFirstSelected)
					{
						try
						{
							m_shouldIgnoreNextSelectionChange = true;
							SelectedItem = item;
							foundFirstSelected = true;
						}
						finally
						{
							m_shouldIgnoreNextSelectionChange = false;
						}
					}
					else
					{
						item.IsSelected = false;
					}
				}
			}
		}
		return foundFirstSelected;
	}

	private void OnTitleBarMetricsChanged(object sender, object args)
	{
		UpdateTitleBarPadding();
	}

	private void OnTitleBarIsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
	{
		UpdateTitleBarPadding();
	}

	private void ClosePaneIfNeccessaryAfterItemIsClicked(NavigationViewItem selectedContainer)
	{
		if (IsPaneOpen &&
			DisplayMode != NavigationViewDisplayMode.Expanded &&
			!DoesNavigationViewItemHaveChildren(selectedContainer) &&
			!m_shouldIgnoreNextSelectionChange)
		{
			ClosePane();
		}
	}

	private bool NeedTopPaddingForRS5OrHigher(CoreApplicationViewTitleBar coreTitleBar)
	{
		// Starting on RS5, we will be using the following IsVisible API together with ExtendViewIntoTitleBar
		// to decide whether to try to add top padding or not.
		// We don't add padding when in fullscreen or tablet mode.
		return coreTitleBar.IsVisible && coreTitleBar.ExtendViewIntoTitleBar
			&& !IsFullScreenOrTabletMode();
	}

	private void UpdateTitleBarPadding()
	{
		if (!m_appliedTemplate)
		{
			return;
		}

		double topPadding = 0;

		var coreTitleBar = m_coreTitleBar;
		if (coreTitleBar != null)
		{
			bool needsTopPadding = false;

			// Do not set a top padding when the IsTitleBarAutoPaddingEnabled property is set to False.
			if (IsTitleBarAutoPaddingEnabled)
			{
				if (ShouldPreserveNavigationViewRS3Behavior())
				{
					needsTopPadding = true;
				}
				else if (ShouldPreserveNavigationViewRS4Behavior())
				{
					// For RS4 apps maintain the behavior that we shipped for RS4.
					// We keep this behavior for app compact purposes.
					needsTopPadding = !coreTitleBar.ExtendViewIntoTitleBar;
				}
				else
				{
					needsTopPadding = NeedTopPaddingForRS5OrHigher(coreTitleBar);
				}
			}

			if (needsTopPadding && XamlRoot?.HostWindow is { } window)
			{
				// Only add extra padding if the NavView is the "root" of the app,
				// but not if the app is expanding into the titlebar
				UIElement root = window.Content;
				GeneralTransform gt = TransformToVisual(root);
				Point pos = gt.TransformPoint(Point.Zero);

				if (pos.Y == 0.0f)
				{
					topPadding = coreTitleBar.Height;
				}
			}

			if (ShouldPreserveNavigationViewRS4Behavior())
			{
				var fe = m_togglePaneTopPadding;
				if (fe != null)
				{
					fe.Height = topPadding;
				}

				fe = m_contentPaneTopPadding;
				if (fe != null)
				{
					fe.Height = topPadding;
				}
			}

			var paneTitleHolderFrameworkElement = m_paneTitleHolderFrameworkElement;
			var paneToggleButton = m_paneToggleButton;

			bool setPaneTitleHolderFrameworkElementMargin = paneTitleHolderFrameworkElement != null && paneTitleHolderFrameworkElement.Visibility == Visibility.Visible;
			bool setPaneToggleButtonMargin = !setPaneTitleHolderFrameworkElementMargin && paneToggleButton != null && paneToggleButton.Visibility == Visibility.Visible;

			if (setPaneTitleHolderFrameworkElementMargin || setPaneToggleButtonMargin)
			{
				var thickness = ThicknessHelper.FromLengths(0, 0, 0, 0);

				if (ShouldShowBackButton())
				{
					if (IsOverlay())
					{
						thickness = ThicknessHelper.FromLengths(c_backButtonWidth, 0, 0, 0);
					}
					else
					{
						thickness = ThicknessHelper.FromLengths(0, c_backButtonHeight, 0, 0);
					}
				}
				else if (ShouldShowCloseButton() && IsOverlay())
				{
					thickness = ThicknessHelper.FromLengths(c_backButtonWidth, 0, 0, 0);
				}

				if (setPaneTitleHolderFrameworkElementMargin)
				{
					// The PaneHeader is hosted by PaneTitlePresenter and PaneTitleHolder.
					paneTitleHolderFrameworkElement.Margin(thickness);
				}
				else
				{
					// The PaneHeader is hosted by PaneToggleButton
					paneToggleButton.Margin(thickness);
				}
			}
		}

		var templateSettings = TemplateSettings;
		if (templateSettings != null)
		{
			// 0.0 and 0.00000000 is not the same in double world. try to reduce the number of TopPadding update event. epsilon is 0.1 here.
			if (Math.Abs(templateSettings.TopPadding - topPadding) > 0.1)
			{
				GetTemplateSettings().TopPadding = topPadding;
			}
		}
	}

	private void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
	{
		// When in compact or minimal, we want to close pane when an item gets chosen.
		if (DisplayMode != NavigationViewDisplayMode.Expanded && args.ChosenSuggestion != null)
		{
			ClosePane();
		}
	}

	private void RaiseDisplayModeChanged(NavigationViewDisplayMode displayMode)
	{
		SetValue(DisplayModeProperty, displayMode);
		var eventArgs = new NavigationViewDisplayModeChangedEventArgs(displayMode);
		DisplayModeChanged?.Invoke(this, eventArgs);
	}

	// This method attaches the series of animations which are fired off dependent upon the amount
	// of space give and the length of the strings involved. It occurs upon re-rendering.
	internal static void CreateAndAttachHeaderAnimation(Visual visual)
	{
#if !IS_UNO
		var compositor = visual.Compositor;
		var cubicFunction = compositor.CreateCubicBezierEasingFunction(new Vector2(0.0f, 0.35f), new Vector2(0.15f, 1.0f));
		var moveAnimation = compositor.CreateVector3KeyFrameAnimation();
		moveAnimation.Target = "Offset";
		moveAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", cubicFunction);
		moveAnimation.Duration = TimeSpan.FromMilliseconds(200);

		var collection = compositor.CreateImplicitAnimationCollection();
		collection.Add("Offset", moveAnimation);
		visual.ImplicitAnimations = collection;
#endif
	}

	private bool IsFullScreenOrTabletMode()
	{
		// ApplicationView.GetForCurrentView() is an expensive call - make sure to cache the ApplicationView
		if (m_applicationView == null)
		{
			m_applicationView = ApplicationView.GetForCurrentViewSafe();
		}

		// UIViewSettings.GetForCurrentView() is an expensive call - make sure to cache the UIViewSettings
		if (m_uiViewSettings == null)
		{
			m_uiViewSettings = UIViewSettings.GetForCurrentView();
		}

		bool isFullScreenMode = m_applicationView.IsFullScreenMode;
		bool isTabletMode = m_uiViewSettings.UserInteractionMode == UserInteractionMode.Touch;

		return isFullScreenMode || isTabletMode;
	}

	private void SetDropShadow()
	{
		var displayMode = DisplayMode;

		if (displayMode == NavigationViewDisplayMode.Compact || displayMode == NavigationViewDisplayMode.Minimal)
		{
			if (m_shadowCaster is { } shadowCaster)
			{
				// Uno specific: Instead for UIElement10 we check for ThemeShadow support
				if (IsThemeShadowSupported())
				{
					shadowCaster.Shadow = new ThemeShadow();
				}
			}
		}
	}

	private void UnsetDropShadow()
	{
		var shadowCaster = m_shadowCaster;

		if (m_shadowCasterEaseOutStoryboard is { } shadowCasterEaseOutStoryboard)
		{
			shadowCasterEaseOutStoryboard.Begin();

			m_shadowCasterEaseOutStoryboardRevoker.Disposable = null;
			void Completed(object sender, object args)
			{
				ShadowCasterEaseOutStoryboard_Completed(shadowCaster);
			}
			shadowCasterEaseOutStoryboard.Completed += Completed;
			m_shadowCasterEaseOutStoryboardRevoker.Disposable = Disposable.Create(() => shadowCasterEaseOutStoryboard.Completed -= Completed);
		}
	}

	private void ShadowCasterEaseOutStoryboard_Completed(Grid shadowCaster)
	{
		// Uno specific: Instead for UIElement10 we check for ThemeShadow support
		if (IsThemeShadowSupported())
		{
			if (shadowCaster.Shadow != null)
			{
				shadowCaster.Shadow = null;
			}
		}
	}

	private void UpdatePaneShadow()
	{
		if (SharedHelpers.IsThemeShadowAvailable())
		{
			Canvas shadowReceiver = (Canvas)GetTemplateChild(c_paneShadowReceiverCanvas);
			if (shadowReceiver == null)
			{
				shadowReceiver = new Canvas();
				shadowReceiver.Name(c_paneShadowReceiverCanvas);

				var contentGrid = GetTemplateChild(c_contentGridName) as Grid;
				if (contentGrid != null)
				{
					Grid.SetRowSpan(shadowReceiver, contentGrid.RowDefinitions.Count);
					Grid.SetRow(shadowReceiver, 0);
					// Only register to columns if those are actually defined
					if (contentGrid.ColumnDefinitions.Count > 0)
					{
						Grid.SetColumn(shadowReceiver, 0);
						Grid.SetColumnSpan(shadowReceiver, contentGrid.ColumnDefinitions.Count);
					}
					contentGrid.Children.Add(shadowReceiver);

					var shadow = new ThemeShadow();
					shadow.Receivers.Add(shadowReceiver);
					var splitView = m_rootSplitView;
					if (splitView != null)
					{
						var paneRoot = splitView.Pane;
						if (paneRoot != null)
						{
							paneRoot.Shadow = shadow;
						}
					}
				}
			}

			// Shadow will get clipped if casting on the splitView.Content directly
			// Creating a canvas with negative margins as receiver to allow shadow to be drawn outside the content grid
			Thickness shadowReceiverMargin = new Thickness(0, -c_paneElevationTranslationZ, -c_paneElevationTranslationZ, -c_paneElevationTranslationZ);

			// Ensuring shadow is aligned to the left
			shadowReceiver.HorizontalAlignment = HorizontalAlignment.Left;

			// Ensure shadow is as wide as the pane when it is open
			if (DisplayMode == NavigationViewDisplayMode.Compact)
			{
				shadowReceiver.Width = m_openPaneLength;
			}
			else
			{
				shadowReceiver.Width = m_openPaneLength - shadowReceiverMargin.Right;
			}
			shadowReceiver.Margin(shadowReceiverMargin);
		}
	}

	private void UpdatePaneOverlayGroup()
	{
		if (m_rootSplitView is { } splitView)
		{
			if (IsPaneOpen && (splitView.DisplayMode == SplitViewDisplayMode.CompactOverlay || splitView.DisplayMode == SplitViewDisplayMode.Overlay))
			{
				VisualStateManager.GoToState(this, "PaneOverlaying", true /*useTransitions*/);
			}
			else
			{
				VisualStateManager.GoToState(this, "PaneNotOverlaying", true /*useTransitions*/);
			}
		}
	}

	private T GetContainerForData<T>(object data)
		where T : class
	{
		if (data == null)
		{
			return null;
		}

		var nvi = data as T;
		if (nvi != null)
		{
			return nvi;
		}

		// First conduct a basic top level search in main menu, which should succeed for a lot of scenarios.
		var mainRepeater = IsTopNavigationView() ? m_topNavRepeater : m_leftNavRepeater;
		var itemIndex = GetIndexFromItem(mainRepeater, data);
		UIElement container;
		if (itemIndex >= 0)
		{
			container = mainRepeater.TryGetElement(itemIndex);
			if (container != null)
			{
				return container as T;
			}
		}

		// then look in footer menu
		var footerRepeater = IsTopNavigationView() ? m_topNavFooterMenuRepeater : m_leftNavFooterMenuRepeater;
		itemIndex = GetIndexFromItem(footerRepeater, data);
		if (itemIndex >= 0)
		{
			container = footerRepeater.TryGetElement(itemIndex);
			if (container != null)
			{
				return container as T;
			}
		}

		// If unsuccessful, unfortunately we are going to have to search through the whole tree
		// TODO: Either fix or remove implementation for TopNav.
		// It may not be required due to top nav rarely having realized children in its default state.
		container = SearchEntireTreeForContainer(mainRepeater, data);
		if (container != null)
		{
			return container as T;
		}

		container = SearchEntireTreeForContainer(footerRepeater, data);
		if (container != null)
		{
			return container as T;
		}

		return null;
	}

	private UIElement SearchEntireTreeForContainer(ItemsRepeater rootRepeater, object data)
	{
		//WINUI TODO: Temporary inefficient solution that results in unnecessary time complexity, fix.
		var index = GetIndexFromItem(rootRepeater, data);
		if (index != -1)
		{
			return rootRepeater.TryGetElement(index);
		}

		for (int i = 0; i < GetContainerCountInRepeater(rootRepeater); i++)
		{
			var container = rootRepeater.TryGetElement(i);
			if (container != null)
			{
				if (container is NavigationViewItem nvi)
				{
					var nviRepeater = nvi.GetRepeater();
					if (nviRepeater != null)
					{
						var foundElement = SearchEntireTreeForContainer(nviRepeater, data);
						if (foundElement != null)
						{
							return foundElement;
						}
					}
				}
			}
		}
		return null;
	}

	private IndexPath SearchEntireTreeForIndexPath(ItemsRepeater rootRepeater, object data, bool isFooterRepeater)
	{
		for (int i = 0; i < GetContainerCountInRepeater(rootRepeater); i++)
		{
			var container = rootRepeater.TryGetElement(i);
			if (container != null)
			{
				if (container is NavigationViewItem nvi)
				{
					var ip = new IndexPath(new List<int>() { isFooterRepeater ? c_footerMenuBlockIndex : c_mainMenuBlockIndex, i });
					var indexPath = SearchEntireTreeForIndexPath(nvi, data, ip);
					if (indexPath != null)
					{
						return indexPath;
					}
				}
			}
		}
		return null;
	}

	// There are two possibilities here if the passed in item has children. Either the children of the passed in container have already been realized,
	// in which case we simply just iterate through the children containers, or they have not been realized yet and we have to iterate through the data
	// and manually realize each item.
	private IndexPath SearchEntireTreeForIndexPath(NavigationViewItem parentContainer, object data, IndexPath ip)
	{
		bool areChildrenRealized = false;
		var childrenRepeater = parentContainer?.GetRepeater();
		if (childrenRepeater != null)
		{
			if (DoesRepeaterHaveRealizedContainers(childrenRepeater))
			{
				areChildrenRealized = true;
				for (int i = 0; i < GetContainerCountInRepeater(childrenRepeater); i++)
				{
					var container = childrenRepeater.TryGetElement(i);
					if (container != null)
					{
						if (container is NavigationViewItem nvi)
						{
							var newIndexPath = ip.CloneWithChildIndex(i);
							if (nvi.Content == data)
							{
								return newIndexPath;
							}
							else
							{
								var foundIndexPath = SearchEntireTreeForIndexPath(nvi, data, newIndexPath);
								if (foundIndexPath != null)
								{
									return foundIndexPath;
								}
							}
						}
					}
					else
					{
						// We found an unrealized child, so we'll want to manually realize and search if we don't find the item.
						areChildrenRealized = false;
					}
				}
			}
		}

		//If children are not realized, manually realize and search.
		if (!areChildrenRealized)
		{
			var childrenData = GetChildren(parentContainer);
			if (childrenData != null)
			{
				// Get children data in an enumarable form
				var newDataSource = childrenData as ItemsSourceView;
				if (childrenData != null && newDataSource == null)
				{
					newDataSource = new InspectingDataSource(childrenData);
				}

				for (int i = 0; i < newDataSource.Count; i++)
				{
					var newIndexPath = ip.CloneWithChildIndex(i);
					var childData = newDataSource.GetAt(i);
					if (childData == data)
					{
						return newIndexPath;
					}
					else
					{
						// Resolve databinding for item and search through that item's children
						var nvib = ResolveContainerForItem(childData, i);
						if (nvib != null)
						{
							if (nvib is NavigationViewItem nvi)
							{
								// Process x:bind
								var extension = CachedVisualTreeHelpers.GetDataTemplateComponent(nvi);
								if (extension != null)
								{
									// Clear out old data.
									extension.Recycle();
									int nextPhase = VirtualizationInfo.PhaseReachedEnd;
									// Run Phase 0
									extension.ProcessBindings(childData, i, 0 /* currentPhase */, out nextPhase);

									//WINUI TODO: If nextPhase is not -1, ProcessBinding for all the phases
								}

								var foundIndexPath = SearchEntireTreeForIndexPath(nvi, data, newIndexPath);
								if (foundIndexPath != null)
								{
									return foundIndexPath;
								}

								//WINUI TODO: Recycle container!
							}
						}
					}
				}
			}
		}

		return null;
	}

	private NavigationViewItemBase ResolveContainerForItem(object item, int index)
	{
		var args = new ElementFactoryGetArgs();
		args.Data = item;
		args.Index = index;

		var container = m_navigationViewItemsFactory.GetElement(args);
		if (container != null)
		{
			if (container is NavigationViewItemBase nvib)
			{
				return nvib;
			}
		}
		return null;
	}

	private void RecycleContainer(UIElement container)
	{
		var args = new ElementFactoryRecycleArgs();
		args.Element = container;
		m_navigationViewItemsFactory.RecycleElement(args);
	}

	private int GetContainerCountInRepeater(ItemsRepeater ir)
	{
		if (ir != null)
		{
			var repeaterItemSourceView = ir.ItemsSourceView;
			if (repeaterItemSourceView != null)
			{
				return repeaterItemSourceView.Count;
			}
		}
		return -1;
	}

	private bool DoesRepeaterHaveRealizedContainers(ItemsRepeater ir)
	{
		if (ir != null)
		{
			if (ir.TryGetElement(0) != null)
			{
				return true;
			}
		}
		return false;
	}

	private int GetIndexFromItem(ItemsRepeater ir, object data)
	{
		if (ir != null)
		{
			var itemsSourceView = ir.ItemsSourceView;
			if (itemsSourceView != null)
			{
				return itemsSourceView.IndexOf(data);
			}
		}
		return -1;
	}

	private object GetItemFromIndex(ItemsRepeater ir, int index)
	{
		if (ir != null)
		{
			var itemsSourceView = ir.ItemsSourceView;
			if (itemsSourceView != null)
			{
				return itemsSourceView.GetAt(index);
			}
		}
		return null;
	}

	private IndexPath GetIndexPathOfItem(object data)
	{
		if (data is NavigationViewItemBase nvib)
		{
			return GetIndexPathForContainer(nvib);
		}

		// In the databinding scenario, we need to conduct a search where we go through every item,
		// realizing it if necessary.
		if (IsTopNavigationView())
		{
			// First search through primary list
			var ip = SearchEntireTreeForIndexPath(m_topNavRepeater, data, false /*isFooterRepeater*/);
			if (ip != null)
			{
				return ip;
			}

			// If item was not located in primary list, search through overflow
			ip = SearchEntireTreeForIndexPath(m_topNavRepeaterOverflowView, data, false /*isFooterRepeater*/);
			if (ip != null)
			{
				return ip;
			}

			// If item was not located in primary list and overflow, search through footer
			ip = SearchEntireTreeForIndexPath(m_topNavFooterMenuRepeater, data, true /*isFooterRepeater*/);
			if (ip != null)
			{
				return ip;
			}
		}
		else
		{
			var ip = SearchEntireTreeForIndexPath(m_leftNavRepeater, data, false /*isFooterRepeater*/);
			if (ip != null)
			{
				return ip;
			}

			// If item was not located in primary list, search through footer
			ip = SearchEntireTreeForIndexPath(m_leftNavFooterMenuRepeater, data, true /*isFooterRepeater*/);
			if (ip != null)
			{
				return ip;
			}
		}

		return new IndexPath(Array.Empty<int>());
	}

	private UIElement GetContainerForIndex(int index, bool inFooter)
	{
		if (IsTopNavigationView())
		{
			// Get the repeater that is presenting the first item
			var ir = inFooter ? m_topNavFooterMenuRepeater
				: (m_topDataProvider.IsItemInPrimaryList(index) ? m_topNavRepeater : m_topNavRepeaterOverflowView);

			// Get the index of the item in the repeater
			var irIndex = inFooter ? index : m_topDataProvider.ConvertOriginalIndexToIndex(index);

			// Get the container of the first item
			var container = ir.TryGetElement(irIndex);
			if (container != null)
			{
				return container;
			}
		}

		else
		{
			var container = inFooter ? m_leftNavFooterMenuRepeater.TryGetElement(index)
				: m_leftNavRepeater.TryGetElement(index);
			if (container != null)
			{
				return container as NavigationViewItemBase;
			}
		}
		return null;
	}

	private NavigationViewItemBase GetContainerForIndexPath(IndexPath ip, bool lastVisible = false, bool forceRealize = false)
	{
		if (ip != null && ip.GetSize() > 0)
		{
			var container = GetContainerForIndex(ip.GetAt(1), ip.GetAt(0) == c_footerMenuBlockIndex /*inFooter*/);
			if (container != null)
			{
				if (lastVisible)
				{
					if (container is NavigationViewItem nvi)
					{
						if (!nvi.IsExpanded)
						{
							return nvi;
						}
					}
				}

				// WINUI TODO: Fix below for top flyout scenario once the flyout is introduced in the XAML.
				// We want to be able to retrieve containers for items that are in the flyout.
				// This will return null if requesting children containers of
				// items in the primary list, or unrealized items in the overflow popup.
				// However this should not happen.
				return GetContainerForIndexPath(container, ip, lastVisible, forceRealize);
			}
		}
		return null;
	}


	private NavigationViewItemBase GetContainerForIndexPath(UIElement firstContainer, IndexPath ip, bool lastVisible, bool forceRealize = false)
	{
		var container = firstContainer;
		if (ip.GetSize() > 2)
		{
			for (int i = 2; i < ip.GetSize(); i++)
			{
				bool succeededGettingNextContainer = false;
				if (container is NavigationViewItem nvi)
				{
					if (lastVisible && nvi.IsExpanded == false)
					{
						return nvi;
					}

					var nviRepeater = nvi.GetRepeater();
					if (nviRepeater != null)
					{
						var index = ip.GetAt(i);
						if ((forceRealize ? nviRepeater.GetOrCreateElement(index) : nviRepeater.TryGetElement(index)) is { } nextContainer)
						{
							container = nextContainer;
							succeededGettingNextContainer = true;
						}
					}
				}
				// If any of the above checks failed, it means something went wrong and we have an index for a non-existent repeater.
				if (!succeededGettingNextContainer)
				{
					return null;
				}
			}
		}
		return container as NavigationViewItemBase;
	}

	private bool IsContainerTheSelectedItemInTheSelectionModel(NavigationViewItemBase nvib)
	{
		var selectedItem = m_selectionModel.SelectedItem;
		if (selectedItem != null)
		{
			var selectedItemContainer = selectedItem as NavigationViewItemBase;
			if (selectedItemContainer == null)
			{
				selectedItemContainer = GetContainerForIndexPath(m_selectionModel.SelectedIndex);
			}

			return selectedItemContainer == nvib;
		}
		return false;
	}

	//private ItemsRepeater LeftNavRepeater => m_leftNavRepeater;

	internal NavigationViewItem GetSelectedContainer()
	{
		var selectedItem = SelectedItem;
		if (selectedItem != null)
		{
			if (selectedItem is NavigationViewItem selectedItemContainer)
			{
				return selectedItemContainer;
			}
			else
			{
				return NavigationViewItemOrSettingsContentFromData(selectedItem);
			}
		}
		return null;
	}

	public void Expand(NavigationViewItem item)
	{
		ChangeIsExpandedNavigationViewItem(item, true /*isExpanded*/);
	}

	public void Collapse(NavigationViewItem item)
	{
		ChangeIsExpandedNavigationViewItem(item, false /*isExpanded*/);
	}

	private bool DoesNavigationViewItemHaveChildren(NavigationViewItem nvi)
	{
		if (nvi.MenuItemsSource != null)
		{
			var sourceView = new InspectingDataSource(nvi.MenuItemsSource);
			return sourceView.Count > 0;
		}

		return nvi.MenuItems.Count > 0 || nvi.HasUnrealizedChildren;
	}

	private void ToggleIsExpandedNavigationViewItem(NavigationViewItem nvi)
	{
		ChangeIsExpandedNavigationViewItem(nvi, !nvi.IsExpanded);
	}

	private void ChangeIsExpandedNavigationViewItem(NavigationViewItem nvi, bool isExpanded)
	{
		if (DoesNavigationViewItemHaveChildren(nvi))
		{
			nvi.IsExpanded = isExpanded;
		}
	}

	private NavigationViewItem FindLowestLevelContainerToDisplaySelectionIndicator()
	{
		var indexIntoIndex = 1;
		var selectedIndex = m_selectionModel.SelectedIndex;
		if (selectedIndex != null && selectedIndex.GetSize() > 1)
		{
			var container = GetContainerForIndex(selectedIndex.GetAt(indexIntoIndex), selectedIndex.GetAt(0) == c_footerMenuBlockIndex /* inFooter */);
			if (container != null)
			{
				if (container is NavigationViewItem nvi)
				{
					var nviImpl = nvi;
					var isRepeaterVisible = nviImpl.IsRepeaterVisible();
					while (nvi != null && isRepeaterVisible && !nvi.IsSelected && nvi.IsChildSelected)
					{
						indexIntoIndex++;
						isRepeaterVisible = false;
						var repeater = nviImpl.GetRepeater();
						if (repeater != null)
						{
							var childContainer = repeater.TryGetElement(selectedIndex.GetAt(indexIntoIndex));
							if (childContainer != null)
							{
								nvi = childContainer as NavigationViewItem;
								nviImpl = nvi;
								isRepeaterVisible = nviImpl.IsRepeaterVisible();
							}
						}
					}
					return nvi;
				}
			}
		}
		return null;
	}

	private void ShowHideChildrenItemsRepeater(NavigationViewItem nvi)
	{
		var nviImpl = nvi;

		nviImpl.ShowHideChildren();

		if (nviImpl.ShouldRepeaterShowInFlyout())
		{
			if (nvi.IsExpanded)
			{
				m_lastItemExpandedIntoFlyout = nvi;
			}
			else
			{
				m_lastItemExpandedIntoFlyout = null;
			}
		}

		// If SelectedItem is being hidden/shown, animate SelectionIndicator
		if (!nvi.IsSelected && nvi.IsChildSelected)
		{
			if (!nviImpl.IsRepeaterVisible() && nvi.IsChildSelected)
			{
				AnimateSelectionChanged(nvi);
			}
			else
			{
				AnimateSelectionChanged(FindLowestLevelContainerToDisplaySelectionIndicator());
			}
		}

		nviImpl.RotateExpandCollapseChevron(nvi.IsExpanded);
	}

	private object GetChildren(NavigationViewItem nvi)
	{
		if (nvi.MenuItems.Count > 0)
		{
			return nvi.MenuItems;
		}
		return nvi.MenuItemsSource;
	}

#if false
	private ItemsRepeater GetChildRepeaterForIndexPath(IndexPath ip)
	{
		var container = GetContainerForIndexPath(ip) as NavigationViewItem;
		if (container != null)
		{
			return container.GetRepeater();
		}
		return null;
	}
#endif

	private object GetChildrenForItemInIndexPath(IndexPath ip, bool forceRealize)
	{
		if (ip != null && ip.GetSize() > 1)
		{
			var container = GetContainerForIndex(ip.GetAt(1), ip.GetAt(0) == c_footerMenuBlockIndex /*inFooter*/);
			if (container != null)
			{
				return GetChildrenForItemInIndexPath(container, ip, forceRealize);
			}
		}
		return null;
	}

	private object GetChildrenForItemInIndexPath(UIElement firstContainer, IndexPath ip, bool forceRealize)
	{
		var container = firstContainer;
		bool shouldRecycleContainer = false;
		if (ip.GetSize() > 2)
		{
			for (int i = 2; i < ip.GetSize(); i++)
			{
				bool succeededGettingNextContainer = false;
				if (container is NavigationViewItem nvi)
				{
					var nextContainerIndex = ip.GetAt(i);
					var nviRepeater = nvi.GetRepeater();
					if (nviRepeater != null && DoesRepeaterHaveRealizedContainers(nviRepeater))
					{
						var nextContainer = nviRepeater.TryGetElement(nextContainerIndex);
						if (nextContainer != null)
						{
							container = nextContainer;
							succeededGettingNextContainer = true;
						}
					}
					else if (forceRealize)
					{
						var childrenData = GetChildren(nvi);
						if (childrenData != null)
						{
							if (shouldRecycleContainer)
							{
								RecycleContainer(nvi);
								shouldRecycleContainer = false;
							}

							// Get children data in an enumarable form
							var newDataSource = childrenData as ItemsSourceView;
							if (childrenData != null && newDataSource == null)
							{
								newDataSource = new InspectingDataSource(childrenData);
							}

							var data = newDataSource.GetAt(nextContainerIndex);
							if (data != null)
							{
								// Resolve databinding for item and search through that item's children
								var nvib = ResolveContainerForItem(data, nextContainerIndex);
								if (nvib != null)
								{
									var nextContainer = nvib as NavigationViewItem;
									if (nextContainer != null)
									{
										// Process x:bind
										var extension = CachedVisualTreeHelpers.GetDataTemplateComponent(nextContainer);
										if (extension != null)
										{
											// Clear out old data.
											extension.Recycle();
											int nextPhase = VirtualizationInfo.PhaseReachedEnd;
											// Run Phase 0
											extension.ProcessBindings(data, nextContainerIndex, 0 /* currentPhase */, out nextPhase); //TODO: This is not implemented yet

											// TODO: If nextPhase is not -1, ProcessBinding for all the phases
										}

										container = nextContainer;
										shouldRecycleContainer = true;
										succeededGettingNextContainer = true;
									}
								}
							}
						}
					}

				}
				// If any of the above checks failed, it means something went wrong and we have an index for a non-existent repeater.
				if (!succeededGettingNextContainer)
				{
					return null;
				}
			}
		}

		var nvi2 = container as NavigationViewItem;
		if (nvi2 != null)
		{
			var children = GetChildren(nvi2);
			if (shouldRecycleContainer)
			{
				RecycleContainer(nvi2);
			}
			return children;
		}

		return null;
	}

	private void CollapseTopLevelMenuItems(NavigationViewPaneDisplayMode oldDisplayMode)
	{
		// We want to make sure only top level items are visible when switching pane modes
		if (oldDisplayMode == NavigationViewPaneDisplayMode.Top)
		{
			CollapseMenuItemsInRepeater(m_topNavRepeater);
			CollapseMenuItemsInRepeater(m_topNavRepeaterOverflowView);
		}
		else
		{
			CollapseMenuItemsInRepeater(m_leftNavRepeater);
		}
	}

	private void CollapseMenuItemsInRepeater(ItemsRepeater ir)
	{
		for (int index = 0; index < GetContainerCountInRepeater(ir); index++)
		{
			var element = ir.TryGetElement(index);
			if (element != null)
			{
				if (element is NavigationViewItem nvi)
				{
					ChangeIsExpandedNavigationViewItem(nvi, false /*isExpanded*/);
				}
			}
		}
	}

	private void RaiseExpandingEvent(NavigationViewItemBase container)
	{
		var eventArgs = new NavigationViewItemExpandingEventArgs(this);
		eventArgs.ExpandingItemContainer = container;
		Expanding?.Invoke(this, eventArgs);
	}

	private void RaiseCollapsedEvent(NavigationViewItemBase container)
	{
		var eventArgs = new NavigationViewItemCollapsedEventArgs(this);
		eventArgs.CollapsedItemContainer = container;
		Collapsed?.Invoke(this, eventArgs);
	}

	private bool IsTopLevelItem(NavigationViewItemBase nvib)
	{
		return IsRootItemsRepeater(GetParentItemsRepeaterForContainer(nvib));
	}

	private bool IsVisible(DependencyObject obj)
	{
		// We'll go up the visual tree until we find this NavigationView.
		// If everything up the tree was visible, then this object was visible.
		DependencyObject current = obj;
		NavigationView navView = this;

		while (current is not null && current != navView)
		{
			if (current is UIElement currentAsUIE)
			{
				if (currentAsUIE.Visibility != Visibility.Visible)
				{
					return false;
				}
			}

			current = VisualTreeHelper.GetParent(current);
		}

		// If we found this NavigationView, then this is both in the visual tree and visible.
		// Otherwise, it's not in the visual tree, and thus is not visible.
		return current == navView;
	}

	private void OnSelectedItemLayoutUpdated(object sender, object args)
	{
		if (m_isSelectionChangedPending)
		{
			m_isSelectionChangedPending = false;

			var item = m_pendingSelectionChangedItem;
			var direction = m_pendingSelectionChangedDirection;

			m_pendingSelectionChangedItem = null;
			m_pendingSelectionChangedDirection = NavigationRecommendedTransitionDirection.Default;

			m_selectedItemLayoutUpdatedRevoker.Disposable = null;

			if (NavigationViewItemOrSettingsContentFromData(item) is { } nvi)
			{
				AnimateSelectionChanged(nvi);
			}

			RaiseSelectionChangedEvent(item, IsSettingsItem(item), direction);
		}
	}
}
