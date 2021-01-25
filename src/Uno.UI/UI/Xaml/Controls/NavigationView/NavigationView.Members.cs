// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationView.cpp file from WinUI controls.
//

using System.Collections.Generic;
using Uno.Disposables;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationView : ContentControl
	{
		enum TopNavigationViewLayoutState
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

		enum NavigationRecommendedTransitionDirection
		{
			FromOverflow, // mapping to SlideNavigationTransitionInfo FromLeft
			FromLeft, // SlideNavigationTransitionInfo
			FromRight, // SlideNavigationTransitionInfo
			Default // Currently it's mapping to EntranceNavigationTransitionInfo and is subject to change.
		};

		// Cache these objects for the view as they are expensive to query via GetForCurrentView() calls.
		ApplicationView m_applicationView = null;
		UIViewSettings m_uiViewSettings = null;


		// Visual components
		Button m_paneToggleButton;
		SplitView m_rootSplitView;
		NavigationViewItem m_settingsItem;
		UIElement m_paneContentGrid;
		Button m_paneSearchButton;
		Button m_backButton;
		TextBlock m_paneTitleTextBlock;
		Grid m_buttonHolderGrid = null;
		ListView m_leftNavListView;
		ListView m_topNavListView;
		Button m_topNavOverflowButton;
		ListView m_topNavListOverflowView;
		Grid m_topNavGrid;
		Border m_topNavContentOverlayAreaGrid;

		UIElement m_prevIndicator;
		UIElement m_nextIndicator;

		FrameworkElement m_togglePaneTopPadding;
		FrameworkElement m_contentPaneTopPadding;
		// FrameworkElement m_topPadding;
		FrameworkElement m_headerContent;

		CoreApplicationViewTitleBar m_coreTitleBar;

		ContentControl m_leftNavPaneAutoSuggestBoxPresenter;
		ContentControl m_topNavPaneAutoSuggestBoxPresenter;

		ContentControl m_leftNavPaneHeaderContentBorder;
		ContentControl m_leftNavPaneCustomContentBorder;
		ContentControl m_leftNavFooterContentBorder;

		ContentControl m_paneHeaderOnTopPane;
		ContentControl m_paneCustomContentOnTopPane;
		ContentControl m_paneFooterOnTopPane;

		int m_indexOfLastSelectedItemInTopNav = 0;
		object m_lastSelectedItemPendingAnimationInTopNav;
		List<int> m_itemsRemovedFromMenuFlyout = new List<int>();

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
		SerialDisposable m_layoutUpdatedToken = new SerialDisposable();
		//winrt::UIElement::AccessKeyInvoked_revoker m_accessKeyInvokedRevoker =;

		bool m_wasForceClosed = false;
		bool m_isClosedCompact = false;
		bool m_blockNextClosingEvent = false;
		bool m_initialListSizeStateSet = false;

		TopNavigationViewDataProvider m_topDataProvider;

		bool m_appliedTemplate = false;

		// flag is used to stop recursive call. eg:
		// Customer select an item from SelectedItem property->ChangeSelection update ListView->LIstView raise OnSelectChange(we want stop here)->change property do do animation again.
		// Customer clicked listview->listview raised OnSelectChange->SelectedItem property changed->ChangeSelection->Undo the selection by SelectedItem(prevItem) (we want it stop here)->ChangeSelection again ->...
		bool m_shouldIgnoreNextSelectionChange = false;

		// If SelectedItem is set by API, ItemInvoked should not be raised. 
		bool m_shouldRaiseInvokeItemInSelectionChange = false;

		// Because virtualization for ItemsStackPanel, not all containers are realized. It request another round of MeasureOverride
		bool m_shouldInvalidateMeasureOnNextLayoutUpdate = false;

		// during measuring, we should ignore SelectChange in overflow, otherwise it enters deadloop.
		bool m_shouldIgnoreOverflowItemSelectionChange = false;

		// when exchanging items between overflow and primary, it cause selectionchange. and then item invoked, and may cause MeasureOverride like customer changed something.
		bool m_shouldIgnoreNextMeasureOverride = false;

		// A flag to track that the selectionchange is caused by selection a item in topnav overflow menu
		bool m_selectionChangeFromOverflowMenu = false;

		TopNavigationViewLayoutState m_topNavigationMode = TopNavigationViewLayoutState.InitStep1;

		// A threshold to stop recovery from overflow to normal happens immediately on resize.
		double m_topNavigationRecoveryGracePeriodWidth = 5.0;

		// Avoid layout cycle on InitStep2
		int m_measureOnInitStep2Count = 0;

		// There are three ways to change IsPaneOpen:
		// 1, customer call IsPaneOpen=true/false directly or nav.IsPaneOpen is binding with a variable and the value is changed.
		// 2, customer click ToggleButton or splitView.IsPaneOpen->nav.IsPaneOpen changed because of window resize
		// 3, customer changed PaneDisplayMode.
		// 2 and 3 are internal implementation and will call by ClosePane/OpenPane. the flag is to indicate 1 if it's false
		bool m_isOpenPaneForInteraction = false;
	}
}
