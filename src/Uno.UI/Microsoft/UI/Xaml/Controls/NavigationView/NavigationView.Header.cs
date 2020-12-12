// MUX reference NavigationView.h, commit acf949a

using System.Collections.Generic;
using Uno.Disposables;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
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
		private ApplicationView m_applicationView = null;
		private UIViewSettings m_uiViewSettings = null;

		private NavigationViewItemsFactory m_navigationViewItemsFactory = null;

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
		private FrameworkElement m_visualItemsSeparator;
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

		private NavigationViewItem m_lastItemExpandedIntoFlyout;

		// Event Tokens
		private readonly SerialDisposable m_paneToggleButtonClickRevoker = new SerialDisposable();
		private readonly SerialDisposable m_paneSearchButtonClickRevoker = new SerialDisposable();
		private readonly SerialDisposable m_titleBarMetricsChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_titleBarIsVisibleChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_backButtonClickedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_closeButtonClickedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_splitViewIsPaneOpenChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_splitViewDisplayModeChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_splitViewPaneClosedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_splitViewPaneClosingRevoker = new SerialDisposable();
		private readonly SerialDisposable m_splitViewPaneOpenedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_splitViewPaneOpeningRevoker = new SerialDisposable();
		private readonly SerialDisposable m_layoutUpdatedToken = new SerialDisposable();
		private readonly SerialDisposable m_accessKeyInvokedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_paneTitleHolderFrameworkElementSizeChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_itemsContainerSizeChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_autoSuggestBoxSuggestionChosenRevoker = new SerialDisposable();

		private readonly SerialDisposable m_leftNavItemsRepeaterElementPreparedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_leftNavItemsRepeaterElementClearingRevoker = new SerialDisposable();
		private readonly SerialDisposable m_leftNavRepeaterLoadedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_leftNavRepeaterGettingFocusRevoker = new SerialDisposable();

		private readonly SerialDisposable m_topNavItemsRepeaterElementPreparedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_topNavItemsRepeaterElementClearingRevoker = new SerialDisposable();
		private readonly SerialDisposable m_topNavRepeaterLoadedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_topNavRepeaterGettingFocusRevoker = new SerialDisposable();

		private readonly SerialDisposable m_leftNavFooterMenuItemsRepeaterElementPreparedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_leftNavFooterMenuItemsRepeaterElementClearingRevoker = new SerialDisposable();
		private readonly SerialDisposable m_leftNavFooterMenuRepeaterLoadedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_leftNavFooterMenuRepeaterGettingFocusRevoker = new SerialDisposable();

		private readonly SerialDisposable m_topNavFooterMenuItemsRepeaterElementPreparedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_topNavFooterMenuItemsRepeaterElementClearingRevoker = new SerialDisposable();
		private readonly SerialDisposable m_topNavFooterMenuRepeaterLoadedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_topNavFooterMenuRepeaterGettingFocusRevoker = new SerialDisposable();

		private readonly SerialDisposable m_topNavOverflowItemsRepeaterElementPreparedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_topNavOverflowItemsRepeaterElementClearingRevoker = new SerialDisposable();

		private readonly SerialDisposable m_selectionChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_childrenRequestedRevoker = new SerialDisposable();

		private readonly SerialDisposable m_menuItemsCollectionChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_footerItemsCollectionChangedRevoker = new SerialDisposable();

		private readonly SerialDisposable m_topNavOverflowItemsCollectionChangedRevoker = new SerialDisposable();

		private readonly SerialDisposable m_flyoutClosingRevoker = new SerialDisposable();

		private bool m_wasForceClosed = false;
		private bool m_isClosedCompact = false;
		private bool m_blockNextClosingEvent = false;
		private bool m_initialListSizeStateSet = false;

		private TopNavigationViewDataProvider m_topDataProvider;

		private SelectionModel m_selectionModel = new SelectionModel();
		private IList<object> m_selectionModelSource = null;

		private ItemsSourceView m_menuItemsSource = null;
		private ItemsSourceView m_footerItemsSource = null;

		private bool m_appliedTemplate = false;

		// Identifies whenever a call is the result of OnApplyTemplate
		private bool m_fromOnApplyTemplate = false;

		// Used to defer updating the SplitView displaymode property
		private bool m_updateVisualStateForDisplayModeFromOnLoaded = false;

		// flag is used to stop recursive call. eg:
		// Customer select an item from SelectedItem property->ChangeSelection update ListView->LIstView raise OnSelectChange(we want stop here)->change property do do animation again.
		// Customer clicked listview->listview raised OnSelectChange->SelectedItem property changed->ChangeSelection->Undo the selection by SelectedItem(prevItem) (we want it stop here)->ChangeSelection again ->...
		private bool m_shouldIgnoreNextSelectionChange = false;
		// A flag to track that the selectionchange is caused by selection a item in topnav overflow menu
		private bool m_selectionChangeFromOverflowMenu = false;
		// Flag indicating whether selection change should raise item invoked. This is needed to be able to raise ItemInvoked before SelectionChanged while SelectedItem should point to the clicked item
		private bool m_shouldRaiseItemInvokedAfterSelection = false;

		private TopNavigationViewLayoutState m_topNavigationMode = TopNavigationViewLayoutState.Uninitialized;

		// A threshold to stop recovery from overflow to normal happens immediately on resize.
		private float m_topNavigationRecoveryGracePeriodWidth = 5.0f;

		// There are three ways to change IsPaneOpen:
		// 1, customer call IsPaneOpen=true/false directly or nav.IsPaneOpen is binding with a variable and the value is changed.
		// 2, customer click ToggleButton or splitView.IsPaneOpen->nav.IsPaneOpen changed because of window resize
		// 3, customer changed PaneDisplayMode.
		// 2 and 3 are internal implementation and will call by ClosePane/OpenPane. the flag is to indicate 1 if it's false
		private bool m_isOpenPaneForInteraction = false;

		private bool m_moveTopNavOverflowItemOnFlyoutClose = false;

		private bool m_shouldIgnoreUIASelectionRaiseAsExpandCollapseWillRaise = false;

		private bool m_OrientationChangedPendingAnimation = false;

		private bool m_TabKeyPrecedesFocusChange = false;

		#region Uno specific

		//TODO: Uno specific - remove when #4689 is fixed
		private readonly SerialDisposable m_leftNavItemsRepeaterUnoBeforeElementPreparedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_topNavItemsRepeaterUnoBeforeElementPreparedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_leftNavFooterMenuItemsRepeaterUnoBeforeElementPreparedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_topNavFooterMenuItemsRepeaterUnoBeforeElementPreparedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_topNavOverflowItemsRepeaterUnoBeforeElementPreparedRevoker = new SerialDisposable();

		#endregion
	}
}
