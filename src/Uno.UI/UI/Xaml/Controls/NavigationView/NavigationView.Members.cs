// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationView.cpp file from WinUI controls.
//

using System.Collections.Generic;
using Uno.Disposables;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationView : ContentControl
	{
		private enum TopNavigationViewLayoutState
		{
			InitStep1 = 0, // Move all data to primary
			InitStep2, // Realized virtualization items
			InitStep3, // Waiting for moving data to overflow
			Normal,
			Overflow,
			OverflowNoChange // InvalidateMeasure but not move any items. It happens when we have enough information 
							 // to swap an navigationviewitem to overflow, InvalidateMeasure is only used to update
							 // SelectionIndicate. Otherwise FindSelectionIndicator may be nullptr for the overflow item
		};

		private enum NavigationRecommendedTransitionDirection
		{
			FromOverflow, // mapping to SlideNavigationTransitionInfo FromLeft
			FromLeft, // SlideNavigationTransitionInfo
			FromRight, // SlideNavigationTransitionInfo
			Default // Currently it's mapping to EntranceNavigationTransitionInfo and is subject to change.
		};

		// Cache these objects for the view as they are expensive to query via GetForCurrentView() calls.
		private ApplicationView m_applicationView = null;
		private UIViewSettings m_uiViewSettings = null;


		// Visual components
		private Button m_paneToggleButton;
		private SplitView m_rootSplitView;
		private NavigationViewItem m_settingsItem;
		private UIElement m_paneContentGrid;
		private Button m_paneSearchButton;
		private Button m_backButton;
		private TextBlock m_paneTitleTextBlock;
		private Grid m_buttonHolderGrid = null;
		private ListView m_leftNavListView;
		private ListView m_topNavListView;
		private Button m_topNavOverflowButton;
		private ListView m_topNavListOverflowView;
		private Grid m_topNavGrid;
		private Border m_topNavContentOverlayAreaGrid;

		private UIElement m_prevIndicator;
		private UIElement m_nextIndicator;

		private FrameworkElement m_togglePaneTopPadding;

		private FrameworkElement m_contentPaneTopPadding;
		// FrameworkElement m_topPadding;
		private FrameworkElement m_headerContent;

		private CoreApplicationViewTitleBar m_coreTitleBar;

		private ContentControl m_leftNavPaneAutoSuggestBoxPresenter;
		private ContentControl m_topNavPaneAutoSuggestBoxPresenter;

		private ContentControl m_leftNavPaneHeaderContentBorder;
		private ContentControl m_leftNavPaneCustomContentBorder;
		private ContentControl m_leftNavFooterContentBorder;

		private ContentControl m_paneHeaderOnTopPane;
		private ContentControl m_paneCustomContentOnTopPane;
		private ContentControl m_paneFooterOnTopPane;

		private int m_indexOfLastSelectedItemInTopNav = 0;
		private object m_lastSelectedItemPendingAnimationInTopNav;
		private List<int> m_itemsRemovedFromMenuFlyout = new List<int>();

		//winrt::ListView::ItemClick_revoker m_leftNavListViewItemClickRevoker =;
		//winrt::ListView::Loaded_revoker m_leftNavListViewLoadedRevoker =;
		//winrt::ListView::SelectionChanged_revoker m_leftNavListViewSelectionChangedRevoker =;
		//winrt::ListView::ItemClick_revoker m_topNavListViewItemClickRevoker =;
		//winrt::ListView::Loaded_revoker m_topNavListViewLoadedRevoker =;
		//winrt::ListView::SelectionChanged_revoker m_topNavListViewSelectionChangedRevoker =;
		//winrt::ListView::SelectionChanged_revoker m_topNavListOverflowViewSelectionChangedRevoker =;
		//PropertyChanged_revoker m_splitViewIsPaneOpenChangedRevoker =;
		//PropertyChanged_revoker m_splitViewDisplayModeChangedRevoker =;
		//winrt::SplitView::PaneClosed_revoker m_splitViewPaneClosedRevoker =;
		//winrt::SplitView::PaneClosing_revoker m_splitViewPaneClosingRevoker =;
		//winrt::SplitView::PaneOpened_revoker m_splitViewPaneOpenedRevoker =;
		//winrt::SplitView::PaneOpening_revoker m_splitViewPaneOpeningRevoker =;
		private SerialDisposable m_layoutUpdatedToken = new SerialDisposable();
		//winrt::UIElement::AccessKeyInvoked_revoker m_accessKeyInvokedRevoker =;

		private bool m_wasForceClosed = false;
		private bool m_isClosedCompact = false;
		private bool m_blockNextClosingEvent = false;
		private bool m_initialListSizeStateSet = false;

		private TopNavigationViewDataProvider m_topDataProvider;

		private bool m_appliedTemplate = false;

		// flag is used to stop recursive call. eg:
		// Customer select an item from SelectedItem property->ChangeSelection update ListView->LIstView raise OnSelectChange(we want stop here)->change property do do animation again.
		// Customer clicked listview->listview raised OnSelectChange->SelectedItem property changed->ChangeSelection->Undo the selection by SelectedItem(prevItem) (we want it stop here)->ChangeSelection again ->...
		private bool m_shouldIgnoreNextSelectionChange = false;

		// If SelectedItem is set by API, ItemInvoked should not be raised. 
		private bool m_shouldRaiseInvokeItemInSelectionChange = false;

		// Because virtualization for ItemsStackPanel, not all containers are realized. It request another round of MeasureOverride
		private bool m_shouldInvalidateMeasureOnNextLayoutUpdate = false;

		// during measuring, we should ignore SelectChange in overflow, otherwise it enters deadloop.
		private bool m_shouldIgnoreOverflowItemSelectionChange = false;

		// when exchanging items between overflow and primary, it cause selectionchange. and then item invoked, and may cause MeasureOverride like customer changed something.
		private bool m_shouldIgnoreNextMeasureOverride = false;

		// A flag to track that the selectionchange is caused by selection a item in topnav overflow menu
		private bool m_selectionChangeFromOverflowMenu = false;

		private TopNavigationViewLayoutState m_topNavigationMode = TopNavigationViewLayoutState.InitStep1;

		// A threshold to stop recovery from overflow to normal happens immediately on resize.
		private double m_topNavigationRecoveryGracePeriodWidth = 5.0;

		// Avoid layout cycle on InitStep2
		private int m_measureOnInitStep2Count = 0;

		// There are three ways to change IsPaneOpen:
		// 1, customer call IsPaneOpen=true/false directly or nav.IsPaneOpen is binding with a variable and the value is changed.
		// 2, customer click ToggleButton or splitView.IsPaneOpen->nav.IsPaneOpen changed because of window resize
		// 3, customer changed PaneDisplayMode.
		// 2 and 3 are internal implementation and will call by ClosePane/OpenPane. the flag is to indicate 1 if it's false
		private bool m_isOpenPaneForInteraction = false;
	}
}
