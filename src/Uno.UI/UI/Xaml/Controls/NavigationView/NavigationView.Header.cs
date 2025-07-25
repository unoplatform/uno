// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationView.h, commit 9f7c129

using System.Collections.Generic;
using Uno.Disposables;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls;

public partial class NavigationView
{
	private enum TopNavigationViewLayoutState
	{
		Uninitialized = 0,
		Initialized
	}

	private enum NavigationRecommendedTransitionDirection
	{
		FromOverflow, // mapping to SlideNavigationTransitionInfo FromLeft
		FromLeft, // SlideNavigationTransitionInfo
		FromRight, // SlideNavigationTransitionInfo
		Default // Currently it's mapping to EntranceNavigationTransitionInfo and is subject to change.
	}

	internal TopNavigationViewDataProvider GetTopDataProvider() { return m_topDataProvider; }

	internal NavigationViewItemsFactory GetNavigationViewItemsFactory() { return m_navigationViewItemsFactory; }

	private bool m_InitialNonForcedModeUpdate = true;

	// Cache these objects for the view as they are expensive to query via GetForCurrentView() calls.
	private ApplicationView m_applicationView;
	private UIViewSettings m_uiViewSettings;

	private NavigationViewItemsFactory m_navigationViewItemsFactory;

	// Visual components

	private Button m_paneToggleButton;
	private SplitView m_rootSplitView;
	private NavigationViewItem m_settingsItem;
	private RowDefinition m_itemsContainerRow;
	private FrameworkElement m_menuItemsScrollViewer;
	private FrameworkElement m_footerItemsScrollViewer;
	private UIElement m_paneContentGrid;
	//private ColumnDefinition m_paneToggleButtonIconGridColumn;
	private FrameworkElement m_paneTitleHolderFrameworkElement;
	private FrameworkElement m_paneTitleFrameworkElement;
	private Button m_paneSearchButton;
	private Button m_backButton;
	private Button m_closeButton;
	private ItemsRepeater m_leftNavRepeater;
	private ItemsRepeater m_topNavRepeater;
	private ItemsRepeater m_leftNavFooterMenuRepeater;
	private ItemsRepeater m_topNavFooterMenuRepeater;
	private Button m_topNavOverflowButton;
	private ItemsRepeater m_topNavRepeaterOverflowView;
	private Grid m_topNavGrid;
	private Border m_topNavContentOverlayAreaGrid;
	private Grid m_shadowCaster;
	private Storyboard m_shadowCasterEaseOutStoryboard;

	// Indicator animations
	private UIElement m_prevIndicator;
	private UIElement m_nextIndicator;
	private UIElement m_activeIndicator;
	private object m_lastSelectedItemPendingAnimationInTopNav;

	private FrameworkElement m_togglePaneTopPadding;
	private FrameworkElement m_contentPaneTopPadding;
	private FrameworkElement m_contentLeftPadding;

	private CoreApplicationViewTitleBar m_coreTitleBar;

	private ContentControl m_leftNavPaneAutoSuggestBoxPresenter;
	private ContentControl m_topNavPaneAutoSuggestBoxPresenter;

	private ContentControl m_leftNavPaneHeaderContentBorder;
	private ContentControl m_leftNavPaneCustomContentBorder;
	private ContentControl m_leftNavFooterContentBorder;

	private ContentControl m_paneHeaderOnTopPane;
	private ContentControl m_paneTitleOnTopPane;
	private ContentControl m_paneCustomContentOnTopPane;
	private ContentControl m_paneFooterOnTopPane;
	private ContentControl m_paneTitlePresenter;

	private ColumnDefinition m_paneHeaderCloseButtonColumn;
	private ColumnDefinition m_paneHeaderToggleButtonColumn;
	private RowDefinition m_paneHeaderContentBorderRow;
	private FrameworkElement m_itemsContainer;

	private NavigationViewItem m_lastItemExpandedIntoFlyout;

	// Event Tokens
	private readonly SerialDisposable m_paneToggleButtonClickRevoker = new();
	private readonly SerialDisposable m_paneSearchButtonClickRevoker = new();
	private readonly SerialDisposable m_titleBarMetricsChangedRevoker = new();
	private readonly SerialDisposable m_titleBarIsVisibleChangedRevoker = new();
	private readonly SerialDisposable m_backButtonClickedRevoker = new();
	private readonly SerialDisposable m_closeButtonClickedRevoker = new();
	private readonly SerialDisposable m_splitViewIsPaneOpenChangedRevoker = new();
	private readonly SerialDisposable m_splitViewDisplayModeChangedRevoker = new();
	private readonly SerialDisposable m_splitViewPaneClosedRevoker = new();
	private readonly SerialDisposable m_splitViewPaneClosingRevoker = new();
	private readonly SerialDisposable m_splitViewPaneOpenedRevoker = new();
	private readonly SerialDisposable m_splitViewPaneOpeningRevoker = new();
	private readonly SerialDisposable m_layoutUpdatedToken = new();
	private readonly SerialDisposable m_accessKeyInvokedRevoker = new();
	private readonly SerialDisposable m_paneTitleHolderFrameworkElementSizeChangedRevoker = new();
	private readonly SerialDisposable m_itemsContainerSizeChangedRevoker = new();
	private readonly SerialDisposable m_autoSuggestBoxQuerySubmittedRevoker = new();

	private readonly SerialDisposable m_leftNavItemsRepeaterElementPreparedRevoker = new();
	private readonly SerialDisposable m_leftNavItemsRepeaterElementClearingRevoker = new();
	private readonly SerialDisposable m_leftNavRepeaterLoadedRevoker = new();
	private readonly SerialDisposable m_leftNavRepeaterGettingFocusRevoker = new();

	private readonly SerialDisposable m_topNavItemsRepeaterElementPreparedRevoker = new();
	private readonly SerialDisposable m_topNavItemsRepeaterElementClearingRevoker = new();
	private readonly SerialDisposable m_topNavRepeaterLoadedRevoker = new();
	private readonly SerialDisposable m_topNavRepeaterGettingFocusRevoker = new();

	private readonly SerialDisposable m_leftNavFooterMenuItemsRepeaterElementPreparedRevoker = new();
	private readonly SerialDisposable m_leftNavFooterMenuItemsRepeaterElementClearingRevoker = new();
	private readonly SerialDisposable m_leftNavFooterMenuRepeaterLoadedRevoker = new();
	private readonly SerialDisposable m_leftNavFooterMenuRepeaterGettingFocusRevoker = new();

	private readonly SerialDisposable m_topNavFooterMenuItemsRepeaterElementPreparedRevoker = new();
	private readonly SerialDisposable m_topNavFooterMenuItemsRepeaterElementClearingRevoker = new();
	private readonly SerialDisposable m_topNavFooterMenuRepeaterLoadedRevoker = new();
	private readonly SerialDisposable m_topNavFooterMenuRepeaterGettingFocusRevoker = new();

	private readonly SerialDisposable m_topNavOverflowItemsRepeaterElementPreparedRevoker = new();
	private readonly SerialDisposable m_topNavOverflowItemsRepeaterElementClearingRevoker = new();

	private readonly SerialDisposable m_selectionChangedRevoker = new();
	private readonly SerialDisposable m_childrenRequestedRevoker = new();

	private readonly SerialDisposable m_menuItemsCollectionChangedRevoker = new();
	private readonly SerialDisposable m_footerItemsCollectionChangedRevoker = new();

	private readonly SerialDisposable m_topNavOverflowItemsCollectionChangedRevoker = new();

	private readonly SerialDisposable m_flyoutClosingRevoker = new();

	private readonly SerialDisposable m_shadowCasterEaseOutStoryboardRevoker = new();

	private readonly SerialDisposable m_selectedItemLayoutUpdatedRevoker = new();

	private bool m_wasForceClosed;
	private bool m_isClosedCompact;
	private bool m_blockNextClosingEvent;
	private bool m_initialListSizeStateSet;

	private TopNavigationViewDataProvider m_topDataProvider;

	private SelectionModel m_selectionModel = new();
	private IList<object> m_selectionModelSource;

	private ItemsSourceView m_menuItemsSource;
	private ItemsSourceView m_footerItemsSource;

	private bool m_isSelectionChangedPending;
	private object m_pendingSelectionChangedItem;
	private NavigationRecommendedTransitionDirection m_pendingSelectionChangedDirection = NavigationRecommendedTransitionDirection.Default;

	private bool m_appliedTemplate;

	// Identifies whenever a call is the result of OnApplyTemplate
	private bool m_fromOnApplyTemplate;

	// Used to defer updating the SplitView displaymode property
	private bool m_updateVisualStateForDisplayModeFromOnLoaded;

	// flag is used to stop recursive call. eg:
	// Customer select an item from SelectedItem property->ChangeSelection update ListView->LIstView raise OnSelectChange(we want stop here)->change property do do animation again.
	// Customer clicked listview->listview raised OnSelectChange->SelectedItem property changed->ChangeSelection->Undo the selection by SelectedItem(prevItem) (we want it stop here)->ChangeSelection again ->...
	private bool m_shouldIgnoreNextSelectionChange;
	// A flag to track that the selectionchange is caused by selection a item in topnav overflow menu
	private bool m_selectionChangeFromOverflowMenu;
	// Flag indicating whether selection change should raise item invoked. This is needed to be able to raise ItemInvoked before SelectionChanged while SelectedItem should point to the clicked item
	private bool m_shouldRaiseItemInvokedAfterSelection;

	private TopNavigationViewLayoutState m_topNavigationMode = TopNavigationViewLayoutState.Uninitialized;

	private readonly List<NavigationViewItemBase> m_itemsWithRevokerObjects = new List<NavigationViewItem>();

	// A threshold to stop recovery from overflow to normal happens immediately on resize.
	private float m_topNavigationRecoveryGracePeriodWidth = 5.0f;

	// There are three ways to change IsPaneOpen:
	// 1, customer call IsPaneOpen=true/false directly or nav.IsPaneOpen is binding with a variable and the value is changed.
	// 2, customer click ToggleButton or splitView.IsPaneOpen->nav.IsPaneOpen changed because of window resize
	// 3, customer changed PaneDisplayMode.
	// 2 and 3 are internal implementation and will call by ClosePane/OpenPane. the flag is to indicate 1 if it's false
	private bool m_isOpenPaneForInteraction;

	private bool m_moveTopNavOverflowItemOnFlyoutClose;

	private bool m_shouldIgnoreUIASelectionRaiseAsExpandCollapseWillRaise;

	private bool m_OrientationChangedPendingAnimation;

	private bool m_TabKeyPrecedesFocusChange;

	private bool m_isLeftPaneTitleEmpty;

	private double m_openPaneLength = 320.0;

	#region Uno specific

	//TODO: Uno specific - remove when #4689 is fixed
	private readonly SerialDisposable m_leftNavItemsRepeaterUnoBeforeElementPreparedRevoker = new();
	private readonly SerialDisposable m_topNavItemsRepeaterUnoBeforeElementPreparedRevoker = new();
	private readonly SerialDisposable m_leftNavFooterMenuItemsRepeaterUnoBeforeElementPreparedRevoker = new();
	private readonly SerialDisposable m_topNavFooterMenuItemsRepeaterUnoBeforeElementPreparedRevoker = new();
	private readonly SerialDisposable m_topNavOverflowItemsRepeaterUnoBeforeElementPreparedRevoker = new();

	#endregion
}
