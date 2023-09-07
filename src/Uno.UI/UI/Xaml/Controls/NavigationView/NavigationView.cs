// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationView.cpp file from WinUI controls.
//

#pragma warning disable CS0414
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.ViewManagement;
using Uno.UI;
using Windows.UI.Core;
using Windows.UI.Composition;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using WindowsWindow = Windows.UI.Xaml.Window;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationView : ContentControl
	{
		static string c_togglePaneButtonName = "TogglePaneButton";
		static string c_paneTitleTextBlock = "PaneTitleTextBlock";
		static string c_rootSplitViewName = "RootSplitView";
		static string c_menuItemsHost = "MenuItemsHost";
		static string c_settingsName = "SettingsNavPaneItem";
		static string c_settingsNameTopNav = "SettingsTopNavPaneItem";
		// static string c_selectionIndicatorName = "SelectionIndicator"; //
		static string c_paneContentGridName = "PaneContentGrid";
		static string c_rootGridName = "RootGrid";
		static string c_contentGridName = "ContentGrid";
		static string c_searchButtonName = "PaneAutoSuggestButton";
		static string c_togglePaneTopPadding = "TogglePaneTopPadding";
		static string c_contentPaneTopPadding = "ContentPaneTopPadding";
		static string c_headerContent = "HeaderContent";
		static string c_navViewBackButton = "NavigationViewBackButton";
		static string c_navViewBackButtonToolTip = "NavigationViewBackButtonToolTip";
		static string c_buttonHolderGrid = "ButtonHolderGrid";

		static string c_topNavMenuItemsHost = "TopNavMenuItemsHost";
		static string c_topNavOverflowButton = "TopNavOverflowButton";
		static string c_topNavMenuItemsOverflowHost = "TopNavMenuItemsOverflowHost";
		static string c_topNavGrid = "TopNavGrid";
		static string c_topNavContentOverlayAreaGrid = "TopNavContentOverlayAreaGrid";
		static string c_leftNavPaneAutoSuggestBoxPresenter = "PaneAutoSuggestBoxPresenter";
		static string c_topNavPaneAutoSuggestBoxPresenter = "TopPaneAutoSuggestBoxPresenter";

		static string c_leftNavFooterContentBorder = "FooterContentBorder";
		static string c_leftNavPaneHeaderContentBorder = "PaneHeaderContentBorder";
		static string c_leftNavPaneCustomContentBorder = "PaneCustomContentBorder";

		static string c_paneHeaderOnTopPane = "PaneHeaderOnTopPane";
		static string c_paneCustomContentOnTopPane = "PaneCustomContentOnTopPane";
		static string c_paneFooterOnTopPane = "PaneFooterOnTopPane";

		static int c_backButtonHeight = 44;
		static int c_backButtonWidth = 48;
		// static int c_backButtonPaneButtonMargin = 8; // Unused
		static int c_paneToggleButtonWidth = 48;
		static int c_toggleButtonHeightWhenShouldPreserveNavigationViewRS3Behavior = 56;
		static int c_backButtonRowDefinition = 1;

		// A tricky to help to stop layout cycle. As we know, we may have this:
		// 1 .. first time invalid measure, normal case because of virtualization
		// 2 .. data update before next invalid measure
		// 3 .. possible layout cycle. a buffer
		// so 4 is selected for threshold.
		int s_measureOnInitStep2CountThreshold = 4;

		static Size c_infSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

		private SerialDisposable _settingsItemSubscriptions = new SerialDisposable();

		void UnhookEventsAndClearFields(bool isFromDestructor = false)
		{
		}

		public NavigationView()
		{
			SetValue(TemplateSettingsProperty, new NavigationViewTemplateSettings());
			DefaultStyleKey = typeof(NavigationView);

			m_topDataProvider = new TopNavigationViewDataProvider(this);

			SizeChanged += OnSizeChanged;
			var items = new ObservableVector<object>();
			SetValue(MenuItemsProperty, items);

			m_topDataProvider.OnRawDataChanged(
				(NotifyCollectionChangedEventArgs args) =>
				{
					OnTopNavDataSourceChanged(args);
				}
			);

			Unloaded += OnUnloaded;
		}

		protected override void OnApplyTemplate()
		{
			// Stop update anything because of PropertyChange during OnApplyTemplate. Update them all together at the end of this function
			m_appliedTemplate = false;

			UnhookEventsAndClearFields();

			var d = new CompositeDisposable();

			// Register for changes in title bar layout
			CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
			if (coreTitleBar != null)
			{
				m_coreTitleBar = coreTitleBar;
				coreTitleBar.LayoutMetricsChanged += OnTitleBarMetricsChanged;
				coreTitleBar.IsVisibleChanged += OnTitleBarIsVisibleChanged;
				m_headerContent = GetTemplateChild(c_headerContent) as FrameworkElement;

				if (ShouldPreserveNavigationViewRS4Behavior())
				{
					m_togglePaneTopPadding = GetTemplateChild(c_togglePaneTopPadding) as FrameworkElement;
					m_contentPaneTopPadding = GetTemplateChild(c_contentPaneTopPadding) as FrameworkElement;
				}
			}
			else
			{
				m_coreTitleBar = null;
				m_headerContent = null;
				m_togglePaneTopPadding = null;
				m_contentPaneTopPadding = null;
			}

			// Set up the pane toggle button click handler
			if (GetTemplateChild(c_togglePaneButtonName) is Button paneToggleButton)
			{
				m_paneToggleButton = paneToggleButton;
				paneToggleButton.Click += OnPaneToggleButtonClick;

				SetPaneToggleButtonAutomationName();

				if (SharedHelpers.IsRS3OrHigher())
				{
					KeyboardAccelerator keyboardAccelerator = new KeyboardAccelerator();
					keyboardAccelerator.Key = VirtualKey.Back;
					keyboardAccelerator.Modifiers = VirtualKeyModifiers.Windows;
					paneToggleButton.KeyboardAccelerators.Add(keyboardAccelerator);
				}
			}

			if (GetTemplateChild(c_leftNavPaneHeaderContentBorder) is ContentControl leftNavPaneHeaderContentBorder)
			{
				m_leftNavPaneHeaderContentBorder = leftNavPaneHeaderContentBorder;
			}

			if (GetTemplateChild(c_leftNavPaneCustomContentBorder) is ContentControl leftNavPaneCustomContentBorder)
			{
				m_leftNavPaneCustomContentBorder = leftNavPaneCustomContentBorder;
			}

			if (GetTemplateChild(c_leftNavFooterContentBorder) is ContentControl leftNavFooterContentBorder)
			{
				m_leftNavFooterContentBorder = leftNavFooterContentBorder;
			}

			if (GetTemplateChild(c_paneHeaderOnTopPane) is ContentControl paneHeaderOnTopPane)
			{
				m_paneHeaderOnTopPane = paneHeaderOnTopPane;
			}

			if (GetTemplateChild(c_paneCustomContentOnTopPane) is ContentControl paneCustomContentOnTopPane)
			{
				m_paneCustomContentOnTopPane = paneCustomContentOnTopPane;
			}

			if (GetTemplateChild(c_paneFooterOnTopPane) is ContentControl paneFooterOnTopPane)
			{
				m_paneFooterOnTopPane = paneFooterOnTopPane;
			}

			if (GetTemplateChild(c_paneTitleTextBlock) is TextBlock textBlock)
			{
				m_paneTitleTextBlock = textBlock;
				UpdatePaneTitleMargins();
			}

			// Get a pointer to the root SplitView
			if (GetTemplateChild(c_rootSplitViewName) is SplitView splitView)
			{
				m_rootSplitView = splitView;
				var splitViewIsPaneOpenChangedRevoker = splitView.RegisterPropertyChangedCallback(
					SplitView.IsPaneOpenProperty,
					OnSplitViewClosedCompactChanged);

				var splitViewDisplayModeChangedRevoker = splitView.RegisterPropertyChangedCallback(
					SplitView.DisplayModeProperty,
					OnSplitViewClosedCompactChanged);

				if (SharedHelpers.IsRS3OrHigher()) // These events are new to RS3/v5 API
				{
					splitView.PaneClosed += OnSplitViewPaneClosed;
					splitView.PaneClosing += OnSplitViewPaneClosing;
					splitView.PaneOpened += OnSplitViewPaneOpened;
					splitView.PaneOpening += OnSplitViewPaneOpening;
				}

				UpdateIsClosedCompact();
			}

			// Change code to NOT do this if we're in top nav mode, to prevent it from being realized:
			if (GetTemplateChild(c_menuItemsHost) is ListView leftNavListView)
			{
				m_leftNavListView = leftNavListView;

				leftNavListView.Loaded += OnLoaded;

				leftNavListView.SelectionChanged += OnSelectionChanged;
				leftNavListView.ItemClick += OnItemClick;

				SetNavigationViewListPosition(leftNavListView, NavigationViewListPosition.LeftNav);
			}

			// Change code to NOT do this if we're in left nav mode, to prevent it from being realized:
			if (GetTemplateChild(c_topNavMenuItemsHost) is ListView topNavListView)
			{
				m_topNavListView = topNavListView;

				topNavListView.Loaded += OnLoaded;

				topNavListView.SelectionChanged += OnSelectionChanged;
				topNavListView.ItemClick += OnItemClick;

				SetNavigationViewListPosition(topNavListView, NavigationViewListPosition.TopPrimary);
			}

			// Change code to NOT do this if we're in left nav mode, to prevent it from being realized:
			if (GetTemplateChild(c_topNavMenuItemsOverflowHost) is ListView topNavListOverflowView)
			{
				m_topNavListOverflowView = topNavListOverflowView;
				topNavListOverflowView.SelectionChanged += OnOverflowItemSelectionChanged;

				SetNavigationViewListPosition(topNavListOverflowView, NavigationViewListPosition.TopOverflow);
			}

			if (GetTemplateChild(c_topNavOverflowButton) is Button topNavOverflowButton)
			{
				m_topNavOverflowButton = topNavOverflowButton;
				AutomationProperties.SetName(topNavOverflowButton, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationOverflowButtonText));
				topNavOverflowButton.Content = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationOverflowButtonText);
				var visual = ElementCompositionPreview.GetElementVisual(topNavOverflowButton);
				CreateAndAttachHeaderAnimation(visual);
			}

			if (GetTemplateChild(c_topNavGrid) is Grid topNavGrid)
			{
				m_topNavGrid = topNavGrid;
			}

			if (GetTemplateChild(c_topNavContentOverlayAreaGrid) is Border topNavContentOverlayAreaGrid)
			{
				m_topNavContentOverlayAreaGrid = topNavContentOverlayAreaGrid;
			}

			if (GetTemplateChild(c_leftNavPaneAutoSuggestBoxPresenter) is ContentControl leftNavSearchContentControl)
			{
				m_leftNavPaneAutoSuggestBoxPresenter = leftNavSearchContentControl;
			}

			if (GetTemplateChild(c_topNavPaneAutoSuggestBoxPresenter) is ContentControl topNavSearchContentControl)
			{
				m_topNavPaneAutoSuggestBoxPresenter = topNavSearchContentControl;
			}

			// Get pointer to the pane content area, for use in the selection indicator animation
			m_paneContentGrid = GetTemplateChild(c_paneContentGridName) as UIElement;

			// Set automation name on search button
			if (GetTemplateChild(c_searchButtonName) is Button button)
			{
				m_paneSearchButton = button;
				button.Click += OnPaneSearchButtonClick;

				var searchButtonName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationViewSearchButtonName);
				AutomationProperties.SetName(button, searchButtonName);

#if !IS_UNO // UNO TODO Missing Tooltop
				var toolTip = ToolTip;
				toolTip.Content = searchButtonName;
				ToolTipService.SetToolTip(button, toolTip);
#endif
			}

			if (GetTemplateChild(c_navViewBackButton) is Button backButton)
			{
				m_backButton = backButton;
				backButton.Click += OnBackButtonClicked;

				string navigationName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationBackButtonName);
				AutomationProperties.SetName(backButton, navigationName);
			}

			if (GetTemplateChild(c_navViewBackButtonToolTip) is ToolTip backButtonToolTip)
			{
				string navigationBackButtonToolTip = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationBackButtonToolTip);
				backButtonToolTip.Content = navigationBackButtonToolTip;
			}

			if (GetTemplateChild(c_buttonHolderGrid) is Grid buttonHolderGrid)
			{
				// TrySetNewFocusedElement call in OnButtonHolderGridGettingFocus is RS4 only
				// if (buttonHolderGrid.try_as<IUIElement8>())
				{
					buttonHolderGrid.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
					buttonHolderGrid.TabFocusNavigation = KeyboardNavigationMode.Once;
					buttonHolderGrid.GettingFocus += OnButtonHolderGridGettingFocus;
				}
			}

			if (SharedHelpers.IsRS2OrHigher())
			{
				// Get hold of the outermost grid and enable XYKeyboardNavigationMode
				// However, we only want this to work in the content pane + the hamburger button (which is not inside the splitview)
				// so disable it on the grid in the content area of the SplitView
				if (GetTemplateChild(c_rootGridName) is Grid rootGrid)
				{
					rootGrid.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
				}

				if (GetTemplateChild(c_contentGridName) is Grid contentGrid)
				{
					contentGrid.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Disabled;
				}
			}

			// Since RS5, SingleSelectionFollowsFocus is set by XAML other than by code
			if (SharedHelpers.IsRS1OrHigher() && ShouldPreserveNavigationViewRS4Behavior() && m_leftNavListView != null)
			{
				m_leftNavListView.SingleSelectionFollowsFocus = false;
			}

			AccessKeyInvoked += OnAccessKeyInvoked;

			m_appliedTemplate = true;

			// Do initial setup
			UpdatePaneDisplayMode();
			UpdateHeaderVisibility();
			UpdateTitleBarPadding();
			UpdatePaneTabFocusNavigation();
			UpdateBackButtonVisibility();
			UpdateSingleSelectionFollowsFocusTemplateSetting();
			UpdateNavigationViewUseSystemVisual();
			PropagateNavigationViewAsParent();
			UpdateLocalVisualState();
		}

		// Hook up the Settings Item Invoked event listener
		void CreateAndHookEventsToSettings(string settingsName)
		{
			var settingsItem = GetTemplateChild(settingsName) as NavigationViewItem;
			if (settingsItem != null && settingsItem != m_settingsItem)
			{
				// If the old settings item is selected, move the selection to the new one.
				var selectedItem = SelectedItem;
				bool shouldSelectSetting = selectedItem != null && IsSettingsItem(selectedItem);

				if (shouldSelectSetting)
				{
					SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(null);
				}

				_settingsItemSubscriptions.Disposable = null;
				var d = new CompositeDisposable();

				m_settingsItem = settingsItem;

				settingsItem.Tapped += OnSettingsTapped;
				d.Add(() => settingsItem.Tapped -= OnSettingsTapped);

				settingsItem.KeyDown += OnSettingsKeyDown;
				d.Add(() => settingsItem.KeyDown -= OnSettingsKeyDown);

				settingsItem.KeyUp += OnSettingsKeyUp;
				d.Add(() => settingsItem.KeyUp -= OnSettingsKeyUp);

				// Do localization for settings item label and Automation Name
				var localizedSettingsName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_SettingsButtonName);
				AutomationProperties.SetName(settingsItem, localizedSettingsName);
				UpdateSettingsItemToolTip();

				// Add the name only in case of horizontal nav
				if (!IsTopNavigationView())
				{
					settingsItem.Content = localizedSettingsName;
				}

				// hook up SettingsItem
				SetValue(SettingsItemProperty, settingsItem);

				if (shouldSelectSetting)
				{
					SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(m_settingsItem);
				}
			}
		}

		// Unlike other control, NavigationView only move items into/out of overflow on MeasureOverride.
		// and the actual measure is done by base.MeasureOverride.
		// We can't move items in LayoutUpdated or OnLoaded, otherwise it would trig another MeasureOverride.
		// Because of Items Container restriction, apps may crash if we move the same item out of overflow,
		// and then move it back to overflow in the same measureoveride(busy, unlink failure, in transition...).
		// TopNavigationViewLayoutState is used to guarantee above will not happen
		//
		// Because of ItemsStackPanel and overflow, we need to run MeasureOverride multiple times. RequestInvalidateMeasureOnNextLayoutUpdate is helping with this.
		// Here is a typical scenario:
		//  MeasureOverride(RequestInvalidateMeasureOnNextLayoutUpdate and register LayoutUpdated) . LayoutUpdated(unregister LayoutUpdated) . InvalidMeasure
		//   . Another MeasureOverride(register LayoutUpdated) . LayoutUpdated(unregister LayoutUpdated) . Done
		protected override Size MeasureOverride(Size availableSize)
		{
			if (!ShouldIgnoreMeasureOverride())
			{
				try
				{
					m_shouldIgnoreOverflowItemSelectionChange = true;
					m_shouldIgnoreNextSelectionChange = true;

					if (IsTopNavigationView() && IsTopPrimaryListVisible())
					{
						if (double.IsInfinity(availableSize.Width))
						{
							// We have infinite space, so move all items to primary list
							m_topDataProvider.MoveAllItemsToPrimaryList();
						}
						else
						{
							HandleTopNavigationMeasureOverride(availableSize);

							if (m_topNavigationMode != TopNavigationViewLayoutState.Normal && m_topNavigationMode != TopNavigationViewLayoutState.Overflow)
							{
								RequestInvalidateMeasureOnNextLayoutUpdate();
							}
#if DEBUG
							if (m_topDataProvider.Size() > 0)
							{
								// We should always have at least one item in primary.
								global::System.Diagnostics.Debug.Assert(m_topDataProvider.GetPrimaryItems().Count > 0);
							}
#endif // DEBUG
						}
					}

					m_layoutUpdatedToken.Disposable = null;
					LayoutUpdated += OnLayoutUpdated;
					m_layoutUpdatedToken.Disposable = Disposable.Create(() => LayoutUpdated -= OnLayoutUpdated); ;
				}
				finally
				{
					m_shouldIgnoreOverflowItemSelectionChange = false;
					m_shouldIgnoreNextSelectionChange = false;
				}
			}
			else
			{
				RequestInvalidateMeasureOnNextLayoutUpdate();
			}
			return base.MeasureOverride(availableSize);
		}

		void OnLayoutUpdated(object sender, object e)
		{
			// We only need to handle once after MeasureOverride, so revoke the token.
			m_layoutUpdatedToken.Disposable = null;

			if (m_shouldInvalidateMeasureOnNextLayoutUpdate)
			{
				m_shouldInvalidateMeasureOnNextLayoutUpdate = false;
				InvalidateMeasure();
			}
			else
			{
				// For some unknown reason, ListView may not always selected a item on the first time when we update the datasource.
				// If it's not selected, we re-selected it.
				var selectedItem = SelectedItem;
				if (selectedItem != null)
				{
					var container = NavigationViewItemOrSettingsContentFromData(selectedItem);
					if (container != null && !container.IsSelected && container.SelectsOnInvoked)
					{
						container.IsSelected = true;

					}
				}

				// In topnav, when an item in overflow menu is clicked, the animation is delayed because that item is not move to primary list yet.
				// And it depends on LayoutUpdated to re-play the animation. m_lastSelectedItemPendingAnimationInTopNav is the last selected overflow item.
				if (m_lastSelectedItemPendingAnimationInTopNav != null)
				{
					AnimateSelectionChanged(m_lastSelectedItemPendingAnimationInTopNav, selectedItem);
				}
				else
				{
					AnimateSelectionChanged(null, selectedItem);
				}
			}
		}

		void OnSizeChanged(object sender, SizeChangedEventArgs args)
		{
			var width = args.NewSize.Width;
			UpdateAdaptiveLayout(width);
			UpdateTitleBarPadding();
			UpdateBackButtonVisibility();
			UpdatePaneTitleMargins();
		}

		// forceSetDisplayMode: On first call to SetDisplayMode, force setting to initial values
		void UpdateAdaptiveLayout(double width, bool forceSetDisplayMode = false)
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
				else if (width < CompactModeThresholdWidth)
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
				throw new InvalidOperationException();
			}


			SetDisplayMode(displayMode, forceSetDisplayMode);

			if (displayMode == NavigationViewDisplayMode.Expanded)
			{
				if (!m_wasForceClosed)
				{
					OpenPane();
				}
			}
		}

		void OnPaneToggleButtonClick(object sender, RoutedEventArgs args)
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

		void OnPaneSearchButtonClick(object sender, RoutedEventArgs args)
		{
			m_wasForceClosed = false;
			OpenPane();

			AutoSuggestBox?.Focus(FocusState.Keyboard);
		}

		void OnButtonHolderGridGettingFocus(UIElement sender, GettingFocusEventArgs args)
		{
			var backButton = m_backButton;
			if (backButton != null)
			{
				var paneButton = m_paneToggleButton;
				if (paneButton != null && paneButton.Visibility == Visibility.Visible)
				{
					// We want the back button to only be able to receive focus from
					// arrowing from the pane toggle button, not from tabbing there.
					if (args.NewFocusedElement == backButton &&
						(args.Direction == FocusNavigationDirection.Previous || args.Direction == FocusNavigationDirection.Next))
					{
						args.TrySetNewFocusedElement(paneButton);
					}
				}
			}
		}

		void OpenPane()
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
		void ClosePane()
		{
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
		bool AttemptClosePaneLightly()
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

		void OnSplitViewClosedCompactChanged(DependencyObject sender, DependencyProperty args)
		{
			if (args == SplitView.IsPaneOpenProperty ||
				args == SplitView.DisplayModeProperty)
			{
				UpdateIsClosedCompact();
			}
		}

		void OnSplitViewPaneClosed(DependencyObject sender, object obj)
		{
			PaneClosed?.Invoke(this, null);
		}

		void OnSplitViewPaneClosing(DependencyObject sender, SplitViewPaneClosingEventArgs args)
		{
			bool pendingPaneClosingCancel = false;
			if (PaneClosing != null)
			{
				if (!m_blockNextClosingEvent) // If this is true, we already sent one out "manually" and don't need to forward SplitView's event
				{
					var eventArgs = new NavigationViewPaneClosingEventArgs();
					eventArgs.SplitViewClosingArgs = args;
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
					var paneList = m_leftNavListView;
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

		void OnSplitViewPaneOpened(DependencyObject sender, object obj)
		{
			PaneOpened?.Invoke(this, null);
		}

		void OnSplitViewPaneOpening(DependencyObject sender, object obj)
		{
			if (m_leftNavListView != null)
			{
				// See UpdateIsClosedCompact 'RS3+ animation timing enhancement' for explanation:
				VisualStateManager.GoToState(this, "ListSizeFull", true /*useTransitions*/);
			}

			PaneOpening?.Invoke(this, null);
		}

		void UpdateIsClosedCompact()
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
				UpdateBackButtonVisibility();
				UpdatePaneTitleMargins();
				UpdatePaneToggleSize();
			}
		}

		void OnBackButtonClicked(object sender, RoutedEventArgs args)
		{
			BackRequested?.Invoke(this, new NavigationViewBackRequestedEventArgs());
		}

		bool IsOverlay()
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

		bool IsLightDismissible()
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

		bool ShouldShowBackButton()
		{
			if (m_backButton != null && !ShouldPreserveNavigationViewRS3Behavior())
			{
				if (DisplayMode == NavigationViewDisplayMode.Minimal && IsPaneOpen)
				{
					return false;
				}

				var visibility = IsBackButtonVisible;
				return (visibility == NavigationViewBackButtonVisible.Visible || (visibility == NavigationViewBackButtonVisible.Auto && !SharedHelpers.IsOnXbox()));
			}

			return false;
		}

		// The automation name and tooltip for the pane toggle button changes depending on whether it is open or closed
		// put the logic here as it will be called in a couple places
		void SetPaneToggleButtonAutomationName()
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
#if !IS_UNO
				var toolTip = ToolTip;
				toolTip.Content = navigationName;
				ToolTipService.SetToolTip(paneToggleButton, toolTip);
#endif
			}
		}

		void UpdateSettingsItemToolTip()
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

#if !IS_UNO
					var localizedSettingsName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_SettingsButtonName);
					var toolTip = ToolTip;
					toolTip.Content = localizedSettingsName;
					ToolTipService.SetToolTip(settingsItem, toolTip);
#endif
				}
			}
		}

		void OnSettingsTapped(object sender, TappedRoutedEventArgs args)
		{
			OnSettingsInvoked();
		}

		void OnSettingsKeyDown(object sender, KeyRoutedEventArgs args)
		{
			var key = args.Key;

			// Because ListViewItem eats the events, we only get these keys on KeyDown.
			if (key == VirtualKey.Space ||
				key == VirtualKey.Enter)
			{
				args.Handled = true;
				OnSettingsInvoked();
			}
		}

		void OnSettingsKeyUp(object sender, KeyRoutedEventArgs args)
		{
			if (!args.Handled)
			{
				// Because ListViewItem eats the events, we only get these keys on KeyUp.
				if (args.OriginalKey == VirtualKey.GamepadA)
				{
					args.Handled = true;
					OnSettingsInvoked();
				}
			}
		}

		void OnSettingsInvoked()
		{
			var prevItem = SelectedItem;
			var settingsItem = m_settingsItem;
			if (IsSettingsItem(prevItem))
			{
				RaiseItemInvoked(settingsItem, true /*isSettings*/);
			}
			else if (settingsItem != null)
			{
				SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(settingsItem);
			}
		}

#if !HAS_UNO
		Vector2 c_frame1point1 = new Vector2(0.9f, 0.1f);
		Vector2 c_frame1point2 = new Vector2(1.0f, 0.2f);
		Vector2 c_frame2point1 = new Vector2(0.1f, 0.9f);
		Vector2 c_frame2point2 = new Vector2(0.2f, 1.0f);
#endif

		void AnimateSelectionChangedToItem(object selectedItem)
		{
			if (selectedItem != null && !IsSelectionSuppressed(selectedItem))
			{
				AnimateSelectionChanged(null /* prevItem */, selectedItem);
			}
		}

		// Please clear the field m_lastSelectedItemPendingAnimationInTopNav when calling this method to prevent garbage value and incorrect animation
		// when the layout is invalidated as it's called in OnLayoutUpdated.
		void AnimateSelectionChanged(object prevItem, object nextItem)
		{
			UIElement prevIndicator = FindSelectionIndicator(prevItem);
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

				if ((prevItem != nextItem) && paneContentGrid != null && prevIndicator != null && nextIndicator != null && SharedHelpers.IsAnimationsEnabled())
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

					if (IsTopNavigationView())
					{
						prevPos = prevPosPoint.X;
						nextPos = nextPosPoint.X;
					}
					else
					{
						prevPos = prevPosPoint.Y;
						nextPos = nextPosPoint.Y;
					}

#if !IS_UNO // disable animations for now
					Visual visual = ElementCompositionPreview.GetElementVisual(this);
					CompositionScopedBatch scopedBatch = visual.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

					// Play the animation on both the previous and next indicators
					PlayIndicatorAnimations(prevIndicator, 0, (float)(nextPos - prevPos), true);
					PlayIndicatorAnimations(nextIndicator, (float)(prevPos - nextPos), 0, false);

					scopedBatch.End();
#endif

					m_prevIndicator = prevIndicator;
					m_nextIndicator = nextIndicator;

#if !IS_UNO // disable animations for now
					scopedBatch.Completed +=
						(object sender, CompositionBatchCompletedEventArgs args) =>
						{
							OnAnimationComplete(sender, args);
						};
#else
					OnAnimationComplete(null, null);
#endif
				}
				else
				{
					// if all else fails, or if animations are turned off, attempt to correctly set the positions and opacities of the indicators.
					ResetElementAnimationProperties(prevIndicator, 0.0f);
					ResetElementAnimationProperties(nextIndicator, 1.0f);
				}

				if (m_lastSelectedItemPendingAnimationInTopNav != null)
				{
					// if nextItem && !nextIndicator, that means a item from topnav flyout is selected, and we delay the animation to LayoutUpdated.
					// nextIndicator is null because we have problem to get the selectionindicator since it's not in primary list yet.
					// Otherwise we already done the animation and clear m_lastSelectedItemPendingAnimationInTopNav.
					if (!(nextItem != null && nextIndicator == null))
					{
						m_lastSelectedItemPendingAnimationInTopNav = null;
					}
				}
			}
		}

#if !IS_UNO
		void PlayIndicatorAnimations(UIElement indicator, float from, float to, bool isOutgoing)
		{
			Visual visual = ElementCompositionPreview.GetElementVisual(indicator);
			Compositor comp = visual.Compositor;

			Size size = indicator.RenderSize;
			double dimension = IsTopNavigationView() ? size.Width : size.Height;

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
			posAnim.InsertKeyFrame(0.0f, from);
			posAnim.InsertKeyFrame(0.333f, to, singleStep);
			posAnim.Duration = TimeSpan.FromMilliseconds(600);

			ScalarKeyFrameAnimation scaleAnim = comp.CreateScalarKeyFrameAnimation();
			scaleAnim.InsertKeyFrame(0.0f, 1);
			scaleAnim.InsertKeyFrame(0.333f, (float)(Math.Abs(to - from) / dimension + 1), comp.CreateCubicBezierEasingFunction(c_frame1point1, c_frame1point2));
			scaleAnim.InsertKeyFrame(1.0f, 1, comp.CreateCubicBezierEasingFunction(c_frame2point1, c_frame2point2));
			scaleAnim.Duration = TimeSpan.FromMilliseconds(600);

			ScalarKeyFrameAnimation centerAnim = comp.CreateScalarKeyFrameAnimation();
			centerAnim.InsertKeyFrame(0.0f, (float)(from < to ? 0.0f : dimension));
			centerAnim.InsertKeyFrame(1.0f, (float)(from < to ? dimension : 0.0f), singleStep);
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

		void OnAnimationComplete(object sender, CompositionBatchCompletedEventArgs args)
		{
			var indicator = m_prevIndicator;
			ResetElementAnimationProperties(indicator, 0.0f);
			m_prevIndicator = null;

			indicator = m_nextIndicator;
			ResetElementAnimationProperties(indicator, 1.0f);
			m_nextIndicator = null;
		}

		void ResetElementAnimationProperties(UIElement element, float desiredOpacity)
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

		NavigationViewItemBase NavigationViewItemBaseOrSettingsContentFromData(object data)
		{
			return GetContainerForData<NavigationViewItemBase>(data);
		}

		NavigationViewItem NavigationViewItemOrSettingsContentFromData(object data)
		{
			return GetContainerForData<NavigationViewItem>(data);
		}

		bool IsSelectionSuppressed(object item)
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

		bool ShouldPreserveNavigationViewRS4Behavior()
		{
			// Since RS5, we support topnav
			return m_topNavGrid == null;
		}

		bool ShouldPreserveNavigationViewRS3Behavior()
		{
			// Since RS4, we support backbutton
			return m_backButton == null;
		}

		UIElement FindSelectionIndicator(object item)
		{
			if (item != null)
			{
				var nvi = NavigationViewItemOrSettingsContentFromData(item);
				if (nvi != null)
				{
					return nvi.GetSelectionIndicator();
				}
			}

			return null;
		}

		//SFF = SelectionFollowsFocus
		//SOI = SelectsOnInvoked
		//
		//                  !SFF&SOI     SFF&SOI     !SFF&&!SOI     SFF&&!SOI
		//ItemInvoke        FIRE         FIRE        FIRE         FIRE
		//SelectionChanged  FIRE         FIRE        DO NOT FIRE  DO NOT FIRE

		//If OnItemClick
		//  If SelectsOnInvoked and previous item == new item, raise OnItemInvoked(same item would not have select change event)
		//  else let SelectionChanged to raise OnItemInvoked event
		//If SelectionChanged, it changes SelectedItem . OnPropertyChange . ChangeSelection. On ChangeSelection:
		//  If !SelectsOnInvoked for new item. Undo the selection.
		//  If SelectsOnInvoked, raise OnItemInvoked(if not from API), then raise SelectionChanged.
		void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			if (!m_shouldIgnoreNextSelectionChange)
			{
				object prevItem = null;
				object nextItem = null;

				if (args.RemovedItems.Count > 0)
				{
					prevItem = args.RemovedItems[0];
				}

				if (args.AddedItems.Count > 0)
				{
					nextItem = args.AddedItems[0];
				}

				if (prevItem != null && nextItem == null && !IsSettingsItem(prevItem)) // try to unselect an item but it's not allowed
				{
					// Always keep one item is selected except Settings

					// So you're wondering - wait if the menu was previously selected, how can
					// the removed item not be a NavigationViewItem? Well, if you say clear a
					// NavigationView of MenuItems() and replace it with MenuItemsSource() full
					// of strings, you may end up in this state which necessitates the following
					// check:
					if (prevItem is NavigationViewItem itemAsNVI)
					{
						itemAsNVI.IsSelected = true;
					}
				}
				else
				{
					SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(nextItem);
				}
			}
		}

		void OnOverflowItemSelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			// SelectOverflowItem is moving data in/out of overflow. it caused another round of OnOverflowItemSelectionChanged
			// also in MeasureOverride, it may raise OnOverflowItemSelectionChanged.
			// Ignore it if it's m_isHandleOverflowItemClick or m_isMeasureOverriding;
			if (!m_shouldIgnoreNextMeasureOverride && !m_shouldIgnoreOverflowItemSelectionChange)
			{
				try
				{
					m_shouldIgnoreNextMeasureOverride = true;
					m_selectionChangeFromOverflowMenu = true;

					if (args.AddedItems.Count > 0)
					{
						var nextItem = args.AddedItems[0];
						if (nextItem != null)
						{
							CloseTopNavigationViewFlyout();

							if (!IsSelectionSuppressed(nextItem))
							{
								SelectOverflowItem(nextItem);
							}
							else
							{
								RaiseItemInvoked(nextItem, false /*isSettings*/);
							}
						}
					}
				}
				finally
				{
					m_shouldIgnoreNextMeasureOverride = false;
					m_selectionChangeFromOverflowMenu = false;
				}
			}
		}

		void RaiseSelectionChangedEvent(object nextItem, bool isSettingsItem, NavigationRecommendedTransitionDirection recommendedDirection)
		{
			var eventArgs = new NavigationViewSelectionChangedEventArgs();
			eventArgs.SelectedItem = nextItem;
			eventArgs.IsSettingsSelected = isSettingsItem;

			var container = NavigationViewItemBaseOrSettingsContentFromData(nextItem);
			if (container != null)
			{
				eventArgs.SelectedItemContainer = container;
			}
			eventArgs.RecommendedNavigationTransitionInfo = CreateNavigationTransitionInfo(recommendedDirection);
			SelectionChanged?.Invoke(this, eventArgs);
		}

		// SelectedItem change can be invoked by API or user's action like clicking. if it's not from API, m_shouldRaiseInvokeItemInSelectionChange would be true
		// If nextItem is selectionsuppressed, we should undo the selection. We didn't undo it OnSelectionChange because we want change by API has the same undo logic.
		void ChangeSelection(object prevItem, object nextItem)
		{
			var nextActualItem = nextItem;
			if (!m_shouldIgnoreNextSelectionChange)
			{
				try
				{
					m_shouldIgnoreNextSelectionChange = true;

					bool isSettingsItem = IsSettingsItem(nextActualItem);

					bool isSelectionSuppressed = IsSelectionSuppressed(nextActualItem);
					if (isSelectionSuppressed)
					{
						UndoSelectionAndRevertSelectionTo(prevItem, nextActualItem);

						// Undo only happened when customer clicked a selectionsuppressed item.
						// To simplify the logic, OnItemClick didn't raise the event and it's been delayed to here.
						RaiseItemInvoked(nextActualItem, isSettingsItem);
					}
					else
					{
						// Other transition other than default only apply to topnav
						// when clicking overflow on topnav, transition is from bottom
						// otherwise if prevItem is on left side of nextActualItem, transition is from left
						//           if prevItem is on right side of nextActualItem, transition is from right
						// click on Settings item is considered Default
						NavigationRecommendedTransitionDirection recommendedDirection = NavigationRecommendedTransitionDirection.Default;
						if (IsTopNavigationView())
						{
							if (m_selectionChangeFromOverflowMenu)
							{
								recommendedDirection = NavigationRecommendedTransitionDirection.FromOverflow;
							}
							else if (!isSettingsItem && prevItem != null && nextActualItem != null)
							{
								recommendedDirection = GetRecommendedTransitionDirection(NavigationViewItemBaseOrSettingsContentFromData(prevItem),
									NavigationViewItemBaseOrSettingsContentFromData(nextActualItem));
							}
						}

						// Bug 17850504, Customer may use NavigationViewItem.IsSelected in ItemInvoke or SelectionChanged Event.
						// To keep the logic the same as RS4, ItemInvoke is before unselect the old item
						// And SelectionChanged is after we selected the new item.
						{
							if (m_shouldRaiseInvokeItemInSelectionChange)
							{
								RaiseItemInvoked(nextActualItem, isSettingsItem, null/*container*/, recommendedDirection);

								// In current implementation, when customer clicked a NavigationViewItem, ListView raised ItemInvoke, and we ignored it
								// then ListView raised SelectionChange event. And NavigationView listen to this event and raise ItemInvoked, and then SelectionChanged.
								// This caused a problem that if customer changed SelectedItem in ItemInvoked, ListView.SelectionChanged event doesn't know about it.
								// So need to see make nextActualItem the same as SelectedItem.
								var selectedItem = SelectedItem;
								if (nextActualItem != selectedItem)
								{
									var invokedItem = nextActualItem;
									nextActualItem = selectedItem;
									isSettingsItem = IsSettingsItem(nextActualItem);
									recommendedDirection = NavigationRecommendedTransitionDirection.Default;

									// Customer set SelectedItem to null in ItemInvoked event, so we unselect the old selectedItem.
									if (invokedItem != null && nextActualItem == null)
									{
										UnselectPrevItem(invokedItem, nextActualItem);
									}
								}
							}
							UnselectPrevItem(prevItem, nextActualItem);

							ChangeSelectStatusForItem(nextActualItem, true /*selected*/);
							RaiseSelectionChangedEvent(nextActualItem, isSettingsItem, recommendedDirection);
						}

						AnimateSelectionChanged(prevItem, nextActualItem);

						if (IsPaneOpen && DisplayMode != NavigationViewDisplayMode.Expanded)
						{
							ClosePane();
						}
					}
				}
				finally
				{
					m_shouldIgnoreNextSelectionChange = false;
				}
			}
		}

		void OnItemClick(object sender, ItemClickEventArgs args)
		{
			var clickedItem = args.ClickedItem;

			var itemContainer = GetContainerForClickedItem(clickedItem);

			var selectedItem = SelectedItem;
			// If SelectsOnInvoked and previous item(selected item) == new item(clicked item), raise OnItemClicked (same item would not have selectchange event)
			// Others would be invoked by SelectionChanged. Please see ChangeSelection for more details.
			//
			// args.ClickedItem itself is the content of ListViewItem, so it can't be compared directly with SelectedItem or do IsSelectionSuppressed
			// We workaround this by compare the selectedItem.content with clickeditem by DoesSelectedItemContainContent.
			// If selecteditem.content == item, selecteditem is used to deduce the selectionsuppressed flag
			if (!m_shouldIgnoreNextSelectionChange && DoesSelectedItemContainContent(clickedItem, itemContainer) && !IsSelectionSuppressed(selectedItem))
			{
				RaiseItemInvoked(selectedItem, false /*isSettings*/, itemContainer);
			}
		}

		void RaiseItemInvoked(object item,
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
					global::System.Diagnostics.Debug.Assert(item != null);
					invokedContainer = item as NavigationViewItemBase;
					global::System.Diagnostics.Debug.Assert(invokedContainer != null);
				}
			}
			eventArgs.InvokedItem = invokedItem;
			eventArgs.InvokedItemContainer = invokedContainer;
			eventArgs.IsSettingsInvoked = isSettings;
			eventArgs.RecommendedNavigationTransitionInfo = CreateNavigationTransitionInfo(recommendedDirection);
			ItemInvoked?.Invoke(this, eventArgs);
		}

		// forceSetDisplayMode: On first call to SetDisplayMode, force setting to initial values
		void SetDisplayMode(NavigationViewDisplayMode displayMode, bool forceSetDisplayMode)
		{
			if (forceSetDisplayMode || DisplayMode != displayMode)
			{
				UpdateVisualStateForDisplayModeGroup(displayMode);

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
		//    PaneDisplayMode.Left || (PaneDisplayMode.var && DisplayMode.Expanded) . Expanded
		//    PaneDisplayMode.LeftCompact || (PaneDisplayMode.var && DisplayMode.Compact) . Compact
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
			if (ShouldShowBackButton())
			{
				return NavigationViewVisualStateDisplayMode.MinimalWithBackButton;
			}
			else
			{
				return NavigationViewVisualStateDisplayMode.Minimal;
			}
		}

		void UpdateVisualStateForDisplayModeGroup(NavigationViewDisplayMode displayMode)
		{
			var splitView = m_rootSplitView;
			if (splitView != null)
			{
				var visualStateDisplayMode = GetVisualStateDisplayMode(displayMode);
				var visualStateName = "";
				var splitViewDisplayMode = SplitViewDisplayMode.CompactOverlay;
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

				var handled = false;
				if (visualStateName == visualStateNameMinimal && IsTopNavigationView())
				{
					// TopNavigationMinimal is introduced since RS6. We need to fallback to Minimal if customer re-template RS5 NavigationView.
					handled = VisualStateManager.GoToState(this, "TopNavigationMinimal", false /*useTransitions*/);
				}
				if (!handled)
				{
					VisualStateManager.GoToState(this, visualStateName, false /*useTransitions*/);
				}
				splitView.DisplayMode = splitViewDisplayMode;
			}
		}

		protected override void OnKeyDown(KeyRoutedEventArgs e)
		{
			var eventArgs = e;
			var key = eventArgs.Key;

			bool handled = false;

			switch (key)
			{
				case VirtualKey.GamepadView:
					if (!IsPaneOpen)
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
				case VirtualKey.Left:
					var altState = KeyboardStateTracker.GetKeyState(VirtualKey.Menu);
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

		bool BumperNavigation(int offset)
		{
			// By passing an offset indicating direction (ideally 1 or -1, meaning right or left respectively)
			// we'll try to move focus to an item. We won't be moving focus to items in the overflow menu and this won't
			// work on left navigation, only dealing with the top primary list here and only with items that don't have
			// !SelectsOnInvoked set to true. If !SelectsOnInvoked is true, we'll skip the item and try focusing on the next one
			// that meets the conditions, in the same direction.
			var shoulderNavigationEnabledParamValue = ShoulderNavigationEnabled;
			var shoulderNavigationForcedDisabled = (shoulderNavigationEnabledParamValue == NavigationViewShoulderNavigationEnabled.Never);

			if (!IsTopNavigationView()
				|| !IsNavigationViewListSingleSelectionFollowsFocus()
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
					var index = m_topDataProvider.IndexOf(item, NavigationViewSplitVectorID.PrimaryList);

					if (index >= 0)
					{
						var topNavListView = m_topNavListView;
						var itemsList = topNavListView.Items;
						var topPrimaryListSize = m_topDataProvider.GetPrimaryListSize();
						index += offset;

						while (index > -1 && index < topPrimaryListSize)
						{
							var newItem = itemsList[index];
							if (newItem is NavigationViewItem newNavViewItem)
							{
								// This is done to skip Separators or other items that are not NavigationViewItems
								if (newNavViewItem.SelectsOnInvoked)
								{
									topNavListView.SelectedItem = newItem;
									return true;
								}
							}

							index += offset;
						}
					}
				}
			}

			return false;
		}

		public object MenuItemFromContainer(DependencyObject container)
		{
			var nvi = container;
			if (nvi != null)
			{
				if (IsTopNavigationView())
				{
					object item = null;
					// Search topnav first, if not found, search overflow
					var lv = m_topNavListView;
					if (lv != null)
					{
						item = lv.ItemFromContainer(nvi);
						if (item != null)
						{
							return item;
						}
					}

					var lvo = m_topNavListOverflowView;
					if (lvo != null)
					{
						item = lvo.ItemFromContainer(nvi);
					}
					return item;
				}
				else
				{
					var lv = m_leftNavListView;
					if (lv != null)
					{
						var item = lv.ItemFromContainer(nvi);
						return item;
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

		void OnTopNavDataSourceChanged(NotifyCollectionChangedEventArgs args)
		{
			CloseTopNavigationViewFlyout();

			// Assume that raw data doesn't change very often for navigationview.
			// So here is a simple implementation and for each data item change, it request a layout change
			// update this in the future if there is performance problem

			// If it's InitStep1, it means that we didn't start the layout yet.
			if (m_topNavigationMode != TopNavigationViewLayoutState.InitStep1)
			{
				{
					try
					{
						m_shouldIgnoreOverflowItemSelectionChange = true;
						m_topDataProvider.MoveAllItemsToPrimaryList();
					}
					finally
					{
						m_shouldIgnoreOverflowItemSelectionChange = false;
					}
				}
				SetTopNavigationViewNextMode(TopNavigationViewLayoutState.InitStep2);
				InvalidateTopNavPrimaryLayout();
			}

			m_indexOfLastSelectedItemInTopNav = 0;
			m_lastSelectedItemPendingAnimationInTopNav = null;
			m_itemsRemovedFromMenuFlyout.Clear();
		}

#if false
		int GetNavigationViewItemCountInPrimaryList()
		{
			return m_topDataProvider.GetNavigationViewItemCountInPrimaryList();
		}

		int GetNavigationViewItemCountInTopNav()
		{
			return m_topDataProvider.GetNavigationViewItemCountInTopNav();
		}
#endif

		internal SplitView GetSplitView()
		{
			return m_rootSplitView;
		}

		internal void TopNavigationViewItemContentChanged()
		{
			if (m_appliedTemplate)
			{
				if (ShouldIgnoreMeasureOverride())
				{
					RequestInvalidateMeasureOnNextLayoutUpdate();
				}
				else
				{
					InvalidateMeasure();
				}
			}
		}

		void OnAccessKeyInvoked(object sender, AccessKeyInvokedEventArgs args)
		{
			if (args.Handled)
			{
				return;
			}

			// For topnav, invoke Morebutton, otherwise togglebutton
			var button = IsTopNavigationView() ? m_topNavOverflowButton : m_paneToggleButton;
			if (button != null)
			{
				if (FrameworkElementAutomationPeer.FromElement(button) is ButtonAutomationPeer peer)
				{
					peer.Invoke();
					args.Handled = true;
				}
			}
		}

		NavigationTransitionInfo CreateNavigationTransitionInfo(NavigationRecommendedTransitionDirection recommendedTransitionDirection)
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
				SlideNavigationTransitionInfo sliderNav = new SlideNavigationTransitionInfo();
				SlideNavigationTransitionEffect effect =
					recommendedTransitionDirection == NavigationRecommendedTransitionDirection.FromRight ?
					SlideNavigationTransitionEffect.FromRight :
					SlideNavigationTransitionEffect.FromLeft;
				// PR 1895355: Bug 17724768: Remove Side-to-Side navigation transition velocity key
				// https://microsoft.visualstudio.com/_git/os/commit/7d58531e69bc8ad1761cff938d8db25f6fb6a841
				// We want to use Effect, but it's not in all os of rs5. as a workaround, we only apply effect to the os which is already remove velocity key.
				//if (var sliderNav2 = sliderNav.try_as<ISlideNavigationTransitionInfo2>())
				//{
				sliderNav.Effect = effect;
				//}
				return sliderNav;
			}
			else
			{
				EntranceNavigationTransitionInfo defaultInfo = new EntranceNavigationTransitionInfo();
				return defaultInfo;
			}
		}

		NavigationRecommendedTransitionDirection GetRecommendedTransitionDirection(DependencyObject prev, DependencyObject next)
		{
			var recommendedTransitionDirection = NavigationRecommendedTransitionDirection.Default;
			var topNavListView = m_topNavListView;
			if (topNavListView != null)
			{
				global::System.Diagnostics.Debug.Assert(prev != null && next != null);
				var prevIndex = topNavListView.IndexFromContainer(prev);
				var nextIndex = topNavListView.IndexFromContainer(next);
				if (prevIndex == -1 || nextIndex == -1)
				{
					// One item is settings, so have problem to get the index
					recommendedTransitionDirection = NavigationRecommendedTransitionDirection.Default;
				}
				else if (prevIndex < nextIndex)
				{
					recommendedTransitionDirection = NavigationRecommendedTransitionDirection.FromRight;
				}
				else if (prevIndex > nextIndex)
				{
					recommendedTransitionDirection = NavigationRecommendedTransitionDirection.FromLeft;
				}
			}
			return recommendedTransitionDirection;
		}

		NavigationViewItemBase GetContainerForClickedItem(object itemData)
		{
			// ListViewBase.OnItemClick raises ItemClicked event, but it doesn't provide the container of a item
			// If it's an virtualized panel like ItemsStackPanel, IsItemItsOwnContainer is called before raise the event in ListViewBase.OnItemClick.
			// Here we assume the LastItemCalledInIsItemItsOwnContainerOverride is the container.
			NavigationViewItemBase container = null;
			var listView = IsTopNavigationView() ? m_topNavListView : m_leftNavListView;
			global::System.Diagnostics.Debug.Assert(listView != null);

			if (listView is NavigationViewList navListView)
			{
				container = navListView.GetLastItemCalledInIsItemItsOwnContainerOverride();
			}

			// Most likely we didn't use ItemStackPanel. but we still try to see if we can find a matched container.
			if (container == null && itemData != null)
			{
				container = listView.ContainerFromItem(itemData) as NavigationViewItemBase;
			}

			// UNO TODO
			// global::System.Diagnostics.Debug.Assert(container != null && container.Content == itemData);
			return container;
		}

		NavigationViewTemplateSettings GetTemplateSettings()
		{
			return TemplateSettings;
		}

		bool IsNavigationViewListSingleSelectionFollowsFocus()
		{
			return (SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Enabled);
		}

		void UpdateSingleSelectionFollowsFocusTemplateSetting()
		{
			GetTemplateSettings().SingleSelectionFollowsFocus = IsNavigationViewListSingleSelectionFollowsFocus();
		}

		void OnSelectedItemPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			var newItem = args.NewValue;
			ChangeSelection(args.OldValue, newItem);

			if (m_appliedTemplate && IsTopNavigationView())
			{
				// In above ChangeSelection function, m_shouldIgnoreNextSelectionChange is set to true first and then set to false when leaving the function scope.
				// When customer select an item by API, SelectionChanged event is raised in ChangeSelection and customer may change the layout.
				// MeasureOverride is executed but it did nothing since m_shouldIgnoreNextSelectionChange is true in ChangeSelection function.
				// InvalidateMeasure to make MeasureOverride happen again
				bool measureOverrideDidNothing = m_shouldInvalidateMeasureOnNextLayoutUpdate && m_layoutUpdatedToken == null;

				if (measureOverrideDidNothing ||
					(newItem != null && m_topDataProvider.IndexOf(newItem) != -1 && m_topDataProvider.IndexOf(newItem, NavigationViewSplitVectorID.PrimaryList) == -1)) // selection is in overflow
				{
					InvalidateTopNavPrimaryLayout();
				}
			}
		}

		void SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(object item)
		{
			// SelectedItem can be set by API or be clicking/selecting ListViewItem or by clicking on settings
			// We should not raise ItemInvoke if SelectedItem is changed by API.
			// If isChangingSelection, this function is called in an inner loop and it should be called from API, so don't change m_shouldRaiseInvokeItemInSelectionChange
			// Otherwise, it's not from API and expect ItemInvoke when selectionchanged.

			bool isChangingSelection = m_shouldIgnoreNextSelectionChange;

			if (!isChangingSelection)
			{
				m_shouldRaiseInvokeItemInSelectionChange = true;
			}

			if (IsTopNavigationView())
			{
				bool shouldAnimateToSelectedItemFromFlyout = true;

				// if the last item selected is going to be removed, i.e. added to the menu flyout, then don't animate.
				foreach (var it in m_itemsRemovedFromMenuFlyout)
				{
					if (it == m_indexOfLastSelectedItemInTopNav)
					{
						shouldAnimateToSelectedItemFromFlyout = false;
						break;
					}
				}

				if (shouldAnimateToSelectedItemFromFlyout)
				{
					m_lastSelectedItemPendingAnimationInTopNav = SelectedItem;
				}
				else
				{
					m_lastSelectedItemPendingAnimationInTopNav = null;
				}

				m_indexOfLastSelectedItemInTopNav = m_topDataProvider.IndexOf(item); // for the next time we animate
			}

			SelectedItem = item;
			if (!isChangingSelection)
			{
				m_shouldRaiseInvokeItemInSelectionChange = false;
			}
		}

		bool DoesSelectedItemContainContent(object item, NavigationViewItemBase itemContainer)
		{
			// If item and selected item has same container, it would be selected item
			bool isSelectedItem = false;
			var selectedItem = SelectedItem;
			if (selectedItem != null && (item != null || itemContainer != null))
			{
				if (item != null && item == selectedItem)
				{
					isSelectedItem = true;
				}
				else if (selectedItem is NavigationViewItemBase selectItemContainer) //SelectedItem itself is a container
				{
					isSelectedItem = selectItemContainer == itemContainer;
				}
				else // selectedItem itself is data
				{
					var selectedItemContainer = NavigationViewItemBaseOrSettingsContentFromData(selectedItem);
					if (selectedItemContainer != null && itemContainer != null)
					{
						isSelectedItem = selectedItemContainer == itemContainer;
					}
				}
			}
			return isSelectedItem;
		}

		void ChangeSelectStatusForItem(object item, bool selected)
		{
			var container = NavigationViewItemOrSettingsContentFromData(item);
			if (container != null)
			{
				// If we unselect an item, ListView doesn't tolerate setting the SelectedItem to null.
				// Instead we remove IsSelected from the item itself, and it make ListView to unselect it.
				// If we select an item, we follow the unselect to simplify the code.
				container.IsSelected = selected;
			}
		}

		bool IsSettingsItem(object item)
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

		void UnselectPrevItem(object prevItem, object nextItem)
		{
			// ListView already handled unselect by itself if ListView raise SelectChanged by itself.
			// We only need to handle unselect when:
			// 1, select from setting to listviewitem or null
			// 2, select from listviewitem to setting
			// 3, select from listviewitem to null from API.
			if (prevItem != null && prevItem != nextItem)
			{
				if (IsSettingsItem(prevItem) || (nextItem != null && IsSettingsItem(nextItem)) || nextItem == null)
				{
					ChangeSelectStatusForItem(prevItem, false /*selected*/);
				}
			}
		}

		void UndoSelectionAndRevertSelectionTo(object prevSelectedItem, object nextItem)
		{
			object selectedItem = null;
			if (prevSelectedItem != null)
			{
				if (IsSelectionSuppressed(prevSelectedItem))
				{
					AnimateSelectionChanged(prevSelectedItem, null);
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

		void CloseTopNavigationViewFlyout()
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

		private void UpdateLocalVisualState(bool useTransitions = false)
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

		void UpdateVisualStateForOverflowButton()
		{
			var state = (OverflowLabelMode == NavigationViewOverflowLabelMode.MoreLabel) ?
				"OverflowButtonWithLabel" :
				"OverflowButtonNoLabel";
			VisualStateManager.GoToState(this, state, false /* useTransitions*/);
		}

		void UpdateLeftNavigationOnlyVisualState(bool useTransitions)
		{
			bool isToggleButtonVisible = IsPaneToggleButtonVisible;
			VisualStateManager.GoToState(this, isToggleButtonVisible ? "TogglePaneButtonVisible" : "TogglePaneButtonCollapsed", false /*useTransitions*/);
		}

		void UpdateNavigationViewUseSystemVisual()
		{
			if (SharedHelpers.IsRS1OrHigher() && !ShouldPreserveNavigationViewRS4Behavior() && m_appliedTemplate)
			{
				var showFocusVisual = SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled;

				PropagateChangeToNavigationViewLists(NavigationViewPropagateTarget.LeftListView,
					(NavigationViewList list) =>
					{
						list.SetShowFocusVisual(showFocusVisual);
					}
					);

				PropagateChangeToNavigationViewLists(NavigationViewPropagateTarget.TopListView,
					(NavigationViewList list) =>
					{
						list.SetShowFocusVisual(showFocusVisual);
					}
					);
			}
		}

		void SetNavigationViewListPosition(ListView listView, NavigationViewListPosition position)
		{
			if (listView != null)
			{
				var navigationViewList = listView as NavigationViewList;
				if (navigationViewList != null)
				{
					navigationViewList.SetNavigationViewListPosition(position);
				}
			}
		}

		void PropagateNavigationViewAsParent()
		{
			PropagateChangeToNavigationViewLists(NavigationViewPropagateTarget.All,
				(NavigationViewList list) =>
					{
						list.SetNavigationViewParent(this);
					}
				);
		}

		void PropagateChangeToNavigationViewLists(NavigationViewPropagateTarget target, Action<NavigationViewList> function)
		{
			if (NavigationViewPropagateTarget.LeftListView == target ||
				NavigationViewPropagateTarget.All == target)
			{
				PropagateChangeToNavigationViewList(m_leftNavListView, function);
			}
			if (NavigationViewPropagateTarget.TopListView == target ||
				NavigationViewPropagateTarget.All == target)
			{
				PropagateChangeToNavigationViewList(m_topNavListView, function);
			}
			if (NavigationViewPropagateTarget.OverflowListView == target ||
				NavigationViewPropagateTarget.All == target)
			{
				PropagateChangeToNavigationViewList(m_topNavListOverflowView, function);
			}
		}

		void PropagateChangeToNavigationViewList(ListView listView, Action<NavigationViewList> function)
		{
			if (listView != null)
			{
				if (listView is NavigationViewList navigationViewList)
				{
					var container = navigationViewList;
					function(container);
				}
			}
		}

		void InvalidateTopNavPrimaryLayout()
		{
			if (m_appliedTemplate && IsTopNavigationView())
			{
				InvalidateMeasure();
			}
		}

		double MeasureTopNavigationViewDesiredWidth(Size availableSize)
		{
			double width = 0.0;
			width += LayoutUtils.MeasureAndGetDesiredWidthFor(m_buttonHolderGrid, availableSize);
			width += LayoutUtils.MeasureAndGetDesiredWidthFor(m_topNavGrid, availableSize);
			return width;
		}

		double MeasureTopNavMenuItemsHostDesiredWidth(Size availableSize)
		{
			return LayoutUtils.MeasureAndGetDesiredWidthFor(m_topNavListView, availableSize);
		}

		double GetTopNavigationViewActualWidth()
		{
			double width = 0.0;
			width += LayoutUtils.GetActualWidthFor(m_buttonHolderGrid);
			width += LayoutUtils.GetActualWidthFor(m_topNavGrid);
			global::System.Diagnostics.Debug.Assert(width < double.MaxValue);
			return width;
		}

		bool IsTopNavigationFirstMeasure()
		{
			// ItemsStackPanel have two round of measure. the first measure only measure the first child, then provide a roughly estimation
			// second measure would initialize the containers.
			bool firstMeasure = false;
			var listView = m_topNavListView;
			if (listView != null)
			{
				int size = m_topDataProvider.GetPrimaryListSize();
				if (size > 1)
				{
					var container = listView.ContainerFromIndex(1);
					firstMeasure = container == null;
				}
			}
			return firstMeasure;
		}

		void RequestInvalidateMeasureOnNextLayoutUpdate()
		{
			m_shouldInvalidateMeasureOnNextLayoutUpdate = true;
		}

		bool HasTopNavigationViewItemNotInPrimaryList()
		{
			return m_topDataProvider.GetPrimaryListSize() != m_topDataProvider.Size();
		}

		void HandleTopNavigationMeasureOverride(Size availableSize)
		{
			var mode = m_topNavigationMode; // mode is for debugging because m_topNavigationMode is changing but we don't want to loss it in the stack
			switch (mode)
			{
				case TopNavigationViewLayoutState.InitStep1: // Move all data to primary
					if (HasTopNavigationViewItemNotInPrimaryList())
					{
						m_topDataProvider.MoveAllItemsToPrimaryList();
					}
					else
					{
						ContinueHandleTopNavigationMeasureOverride(TopNavigationViewLayoutState.InitStep2, availableSize);
					}
					break;
				case TopNavigationViewLayoutState.InitStep2: // Realized virtualization items
					{
						// Bug 18196691: For some reason(eg: customer hide topnav grid or it's parent from code directly),
						// The 2nd item may never been realized. and it will enter into a layout_cycle.
						// For performance reason, we don't go through the visualtree to determine if ListView is actually visible or not
						// m_measureOnInitStep2Count is used to avoid the cycle

						// In our test environment, m_measureOnInitStep2Count should <= 2 since we didn't hide anything from code
						// so the assert count is different from s_measureOnInitStep2CountThreshold
						// global::System.Diagnostics.Debug.Assert(m_measureOnInitStep2Count <= 2); // This assert doesn't seem to be relevant on Uno

						if (m_measureOnInitStep2Count >= s_measureOnInitStep2CountThreshold || !IsTopNavigationFirstMeasure())
						{
							m_measureOnInitStep2Count = 0;
							ContinueHandleTopNavigationMeasureOverride(TopNavigationViewLayoutState.InitStep3, availableSize);
						}
						else
						{
							m_measureOnInitStep2Count++;
						}
					}
					break;

				case TopNavigationViewLayoutState.InitStep3: // Waiting for moving data to overflow
					HandleTopNavigationMeasureOverrideStep3(availableSize);
					break;
				case TopNavigationViewLayoutState.Normal:
					HandleTopNavigationMeasureOverrideNormal(availableSize);
					break;
				case TopNavigationViewLayoutState.Overflow:
					HandleTopNavigationMeasureOverrideOverflow(availableSize);
					break;
				case TopNavigationViewLayoutState.OverflowNoChange:
					SetTopNavigationViewNextMode(TopNavigationViewLayoutState.Overflow);
					break;
			}
		}

		void HandleTopNavigationMeasureOverrideNormal(Windows.Foundation.Size availableSize)
		{
			var desiredWidth = MeasureTopNavigationViewDesiredWidth(c_infSize);
			if (desiredWidth > availableSize.Width)
			{
				ContinueHandleTopNavigationMeasureOverride(TopNavigationViewLayoutState.InitStep3, availableSize);
			}
		}

		void HandleTopNavigationMeasureOverrideOverflow(Windows.Foundation.Size availableSize)
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
					ContinueHandleTopNavigationMeasureOverride(TopNavigationViewLayoutState.InitStep1, availableSize);
				}
				else
				{
					m_topDataProvider.InvalidWidthCacheIfOverflowItemContentChanged();

					var movableItems = FindMovableItemsRecoverToPrimaryList(availableSize.Width - desiredWidth, new List<int>()/*includeItems*/);
					m_topDataProvider.MoveItemsToPrimaryList(movableItems);
					if (m_topDataProvider.HasInvalidWidth(movableItems))
					{
						m_topDataProvider.ResetAttachedData(); // allow every item to be recovered in next MeasureOverride
						RequestInvalidateMeasureOnNextLayoutUpdate();
					}
				}
			}
		}

		void ContinueHandleTopNavigationMeasureOverride(TopNavigationViewLayoutState nextMode, Size availableSize)
		{
			SetTopNavigationViewNextMode(nextMode);
			HandleTopNavigationMeasureOverride(availableSize);
		}

		void HandleTopNavigationMeasureOverrideStep3(Size availableSize)
		{
			SetOverflowButtonVisibility(Visibility.Collapsed);
			var desiredWidth = MeasureTopNavigationViewDesiredWidth(c_infSize);
			if (desiredWidth < availableSize.Width)
			{
				ContinueHandleTopNavigationMeasureOverride(TopNavigationViewLayoutState.Normal, availableSize);
			}
			else
			{
				// overflow
				SetOverflowButtonVisibility(Visibility.Visible);
				var desiredWidthForOverflowButton = MeasureTopNavigationViewDesiredWidth(c_infSize);

				global::System.Diagnostics.Debug.Assert(desiredWidthForOverflowButton >= desiredWidth);
				m_topDataProvider.OverflowButtonWidth(desiredWidthForOverflowButton - desiredWidth);

				ShrinkTopNavigationSize(desiredWidthForOverflowButton, availableSize);
			}
		}

		void SetOverflowButtonVisibility(Visibility visibility)
		{
			if (visibility != TemplateSettings.OverflowButtonVisibility)
			{
				GetTemplateSettings().OverflowButtonVisibility = visibility;
			}
		}

		void SetTopNavigationViewNextMode(TopNavigationViewLayoutState nextMode)
		{
			m_topNavigationMode = nextMode;
		}

		void SelectOverflowItem(object item)
		{
			// Calculate selected overflow item size.
			var selectedOverflowItemIndex = m_topDataProvider.IndexOf(item);
			global::System.Diagnostics.Debug.Assert(selectedOverflowItemIndex != -1);
			var selectedOverflowItemWidth = m_topDataProvider.GetWidthForItem(selectedOverflowItemIndex);

			bool needInvalidMeasure = !m_topDataProvider.IsValidWidthForItem(selectedOverflowItemIndex);

			if (!needInvalidMeasure)
			{
				//
				var actualWidth = GetTopNavigationViewActualWidth();
				var desiredWidth = MeasureTopNavigationViewDesiredWidth(c_infSize);
				global::System.Diagnostics.Debug.Assert(desiredWidth <= actualWidth);

				// Calculate selected item size
				var selectedItemIndex = -1;
				var selectedItemWidth = 0.0;
				var selectedItem = SelectedItem;
				if (selectedItem != null)
				{
					selectedItemIndex = m_topDataProvider.IndexOf(selectedItem);
					if (selectedItemIndex != -1)
					{
						selectedItemWidth = m_topDataProvider.GetWidthForItem(selectedItemIndex);
					}
				}

				var widthAtLeastToBeRemoved = desiredWidth + selectedOverflowItemWidth - actualWidth;

				// calculate items to be removed from primary because a overflow item is selected.
				// SelectedItem is assumed to be removed from primary first, then added it back if it should not be removed
				var itemsToBeRemoved = FindMovableItemsToBeRemovedFromPrimaryList(widthAtLeastToBeRemoved, null /*excludeItems*/);
				m_itemsRemovedFromMenuFlyout = itemsToBeRemoved;

				// calculate the size to be removed
				var toBeRemovedItemWidth = m_topDataProvider.CalculateWidthForItems(itemsToBeRemoved);

				var widthAvailableToRecover = toBeRemovedItemWidth - widthAtLeastToBeRemoved;
				var itemsToBeAdded = FindMovableItemsRecoverToPrimaryList(widthAvailableToRecover, new List<int> { selectedOverflowItemIndex }/*includeItems*/);

				if (!itemsToBeAdded.Contains(selectedOverflowItemIndex))
				{
					itemsToBeAdded.Add(selectedOverflowItemIndex);
				}

				if (m_topDataProvider.HasInvalidWidth(itemsToBeAdded))
				{
					needInvalidMeasure = true;
				}
				else
				{
					// Exchange items between Primary and Overflow
					{
						try
						{
							m_shouldIgnoreNextSelectionChange = true;

							m_topDataProvider.MoveItemsToPrimaryList(itemsToBeAdded);
							m_topDataProvider.MoveItemsOutOfPrimaryList(itemsToBeRemoved);
						}
						finally
						{
							m_shouldIgnoreNextSelectionChange = false;
						}
					}
					SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(item);

					SetTopNavigationViewNextMode(TopNavigationViewLayoutState.OverflowNoChange);
					InvalidateMeasure();
				}
			}

			if (needInvalidMeasure || m_shouldInvalidateMeasureOnNextLayoutUpdate)
			{
				// not all items have known width, need to redo the layout
				m_topDataProvider.MoveAllItemsToPrimaryList();
				SetTopNavigationViewNextMode(TopNavigationViewLayoutState.InitStep2);
				SetSelectedItemAndExpectItemInvokeWhenSelectionChangedIfNotInvokedFromAPI(item);
				InvalidateTopNavPrimaryLayout();
			}
		}

		void ShrinkTopNavigationSize(double desiredWidth, Size availableSize)
		{
			UpdateTopNavigationWidthCache();
			SetTopNavigationViewNextMode(TopNavigationViewLayoutState.Overflow);

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
				var itemToBeRemoved = FindMovableItemsToBeRemovedFromPrimaryList(widthAtLeastToBeRemoved, new List<int> { selectedItemIndex });

				// At least one item is kept on primary list
				KeepAtLeastOneItemInPrimaryList(itemToBeRemoved, false/*shouldKeepFirst*/);

				// There should be no item is virtualized in this step
				global::System.Diagnostics.Debug.Assert(!m_topDataProvider.HasInvalidWidth(itemToBeRemoved));
				m_topDataProvider.MoveItemsOutOfPrimaryList(itemToBeRemoved);
			}
		}

		List<int> FindMovableItemsRecoverToPrimaryList(double availableWidth, List<int> includeItems)
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
			if (i == size && !toBeMoved.Empty())
			{
				toBeMoved.RemoveAt(toBeMoved.Count - 1);
			}
			return toBeMoved;
		}

		List<int> FindMovableItemsToBeRemovedFromPrimaryList(double widthAtLeastToBeRemoved, List<int> excludeItems)
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

		List<int> FindMovableItemsBeyondAvailableWidth(double availableWidth)
		{
			List<int> toBeMoved = new List<int>();
			var listView = m_topNavListView;
			if (listView != null)
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
							var container = listView.ContainerFromIndex(i);
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

		void KeepAtLeastOneItemInPrimaryList(List<int> itemInPrimaryToBeRemoved, bool shouldKeepFirst)
		{
			if (!itemInPrimaryToBeRemoved.Empty() && itemInPrimaryToBeRemoved.Count == m_topDataProvider.GetPrimaryListSize())
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

		int GetSelectedItemIndex()
		{
			return m_topDataProvider.IndexOf(SelectedItem);
		}

		void UpdateTopNavigationWidthCache()
		{
			int size = m_topDataProvider.GetPrimaryListSize();
			var topNavigationView = m_topNavListView;
			if (topNavigationView != null)
			{
				for (int i = 0; i < size; i++)
				{
					var container = topNavigationView.ContainerFromIndex(i);
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

		bool IsTopNavigationView()
		{
			return PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
		}

		bool IsTopPrimaryListVisible()
		{
			return m_topNavListView != null && (TemplateSettings.TopPaneVisibility == Visibility.Visible);
		}

		void CoerceToGreaterThanZero(ref double value)
		{
			// Property coercion for OpenPaneLength, CompactPaneLength, CompactModeThresholdWidth, ExpandedModeThresholdWidth
			value = Math.Max(value, 0.0);
		}

		void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			var property = args.Property;

			if (property == IsPaneOpenProperty)
			{
				OnIsPaneOpenChanged();
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
				UpdatePaneToggleSize();
			}
			else if (property == IsBackButtonVisibleProperty)
			{
				UpdateBackButtonVisibility();
				UpdateAdaptiveLayout(ActualWidth);
				if (IsTopNavigationView())
				{
					InvalidateTopNavPrimaryLayout();
				}

				// Telemetry is not used
				//if (g_IsTelemetryProviderEnabled && IsBackButtonVisible == NavigationViewBackButtonVisible.Collapsed)
				//{
				//	//  Explicitly disabling BackUI on NavigationView
				//	TraceLoggingWrite(
				//		g_hTelemetryProvider,
				//		"NavigationView_DisableBackUI",
				//		TraceLoggingDescription("Developer explicitly disables the BackUI on NavigationView"));
				//}
			}
			else if (property == MenuItemsSourceProperty)
			{
				UpdateListViewItemSource();
			}
			else if (property == MenuItemsProperty)
			{
				UpdateListViewItemSource();
			}
			else if (property == PaneDisplayModeProperty)
			{
				// m_wasForceClosed is set to true because ToggleButton is clicked and Pane is closed.
				// When PaneDisplayMode is changed, reset the force flag to make the Pane can be opened automatically again.
				m_wasForceClosed = false;

				UpdatePaneDisplayMode((NavigationViewPaneDisplayMode)args.OldValue, (NavigationViewPaneDisplayMode)args.NewValue);
				UpdatePaneToggleButtonVisibility();
				UpdatePaneVisibility();
				UpdateLocalVisualState();
			}
			else if (property == IsPaneVisibleProperty)
			{
				UpdatePaneVisibility();

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
			}
			else if (property == SelectionFollowsFocusProperty)
			{
				UpdateSingleSelectionFollowsFocusTemplateSetting();
				UpdateNavigationViewUseSystemVisual();
			}
			else if (property == IsPaneToggleButtonVisibleProperty)
			{
				UpdatePaneToggleButtonVisibility();
				UpdateLocalVisualState();
			}
			else if (property == IsSettingsVisibleProperty)
			{
				UpdateLocalVisualState();
			}
		}

		void OnPropertyChanged_CoerceToGreaterThanZero(DependencyPropertyChangedEventArgs args)
		{
			if (args.NewValue is double value)
			{
				var coercedValue = value;
				CoerceToGreaterThanZero(ref coercedValue);
				SetValue(args.Property, coercedValue);

				OnPropertyChanged(args);
			}
		}

		void OnLoaded(object sender, RoutedEventArgs args)
		{
			var item = SelectedItem;
			if (item != null)
			{
				if (!IsSelectionSuppressed(item))
				{
					// Work around for issue where NavigationViewItem doesn't report
					// its initial IsSelected state properly on RS2 and older builds.
					//
					// Without this, the visual state is proper, but the actual
					// IsSelected reported by the NavigationViewItem is not.
					if (!SharedHelpers.IsRS3OrHigher())
					{
						if (item is NavigationViewItem navViewItem)
						{
							navViewItem.IsSelected = true;
						}
					}
				}
				AnimateSelectionChanged(null /* prevItem */, item);
			}
		}

		void OnUnloaded(object sender, RoutedEventArgs args)
		{
			UnhookEventsAndClearFields();
		}

		void OnIsPaneOpenChanged()
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
					// splitview.IsPaneOpen and nav.IsPaneOpen is two way binding. There is possible change that SplitView.IsPaneOpen=false, then
					// nav.IsPaneOpen=false. We don't need to set force flag in this situation
					if (splitView.IsPaneOpen)
					{
						m_wasForceClosed = true;
					}
				}
			}

			SetPaneToggleButtonAutomationName();

			// ***************************** Uno only - begin
			// When the OnSplitViewClosedCompactChanged callback is invoked (registered on SplitView.IsPaneOpen),
			// the two-way binding between SplitView.IsOpen and NavView.IsPaneOpen has not been updated yet,
			// the the local NavView.IsPaneOpen is still == true. (cf. https://github.com/unoplatform/uno/issues/3774)
			// (Yeah ... there is 2 code path to track the IsPaneOpen of the SplitView: callback and 2-way binding :/)
			// So we make sure to request an update of the back button visibility also when the local IsPaneOpen is being updated.
			UpdateBackButtonVisibility();
			// ***************************** Uno only - end

			UpdatePaneTabFocusNavigation();
			UpdateSettingsItemToolTip();
		}

		void UpdatePaneToggleButtonVisibility()
		{
			var visible = IsPaneToggleButtonVisible && !IsTopNavigationView();
			GetTemplateSettings().PaneToggleButtonVisibility = Util.VisibilityFromBool(visible);
		}

		void UpdatePaneDisplayMode()
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

				CreateAndHookEventsToSettings(c_settingsName);

				//if (IUIElement8 thisAsUIElement8 = this)
				{
					var paneToggleButton = m_paneToggleButton;
					if (paneToggleButton != null)
					{
						/*thisAsUIElement8.*/
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

				CreateAndHookEventsToSettings(c_settingsNameTopNav);

				// if (IUIElement8 thisAsUIElement8 = this)
				{
					var topNavOverflowButton = m_topNavOverflowButton;
					if (topNavOverflowButton != null)
					{
						/*thisAsUIElement8.*/
						KeyTipTarget = topNavOverflowButton;
					}
				}
			}

			UpdateContentBindingsForPaneDisplayMode();
			UpdateListViewItemSource();
		}

		void UpdatePaneDisplayMode(NavigationViewPaneDisplayMode oldDisplayMode, NavigationViewPaneDisplayMode newDisplayMode)
		{
			if (!m_appliedTemplate)
			{
				return;
			}

			UpdatePaneDisplayMode();

			// For better user experience, We help customer to Open/Close Pane automatically when we switch between LeftMinimal <. other PaneDisplayMode.
			// From other navigation PaneDisplayMode to LeftMinimal, we expect pane is closed.
			// From LeftMinimal to other left PaneDisplayMode other than var, we expect Pane is opened.
			if (!IsTopNavigationView())
			{
				bool isPaneOpen = IsPaneOpen;
				if (newDisplayMode == NavigationViewPaneDisplayMode.LeftMinimal && isPaneOpen)
				{
					ClosePane();
				}
				else if (oldDisplayMode == NavigationViewPaneDisplayMode.LeftMinimal &&
					!isPaneOpen &&
					newDisplayMode != NavigationViewPaneDisplayMode.Auto)
				{
					OpenPane();
				}
			}
		}

		void UpdatePaneVisibility()
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
			}
			else
			{
				templateSettings.TopPaneVisibility = Visibility.Collapsed;
				templateSettings.LeftPaneVisibility = Visibility.Collapsed;
			}
		}

		void SwapPaneHeaderContent(ContentControl newParentTrackRef, ContentControl oldParentTrackRef, string propertyPathName)
		{
			var newParent = newParentTrackRef;
			if (newParent != null)
			{
				var oldParent = oldParentTrackRef;
				if (oldParent != null)
				{
					oldParent.ClearValue(ContentControl.ContentProperty);
				}

				var binding = new Binding();
				var propertyPath = new PropertyPath(propertyPathName);
				binding.Path = propertyPath;
				binding.Source = this;
				BindingOperations.SetBinding(newParent, ContentControl.ContentProperty, binding);
			}
		}

		void UpdateContentBindingsForPaneDisplayMode()
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

				var binding = new Binding();
				var propertyPath = new PropertyPath("AutoSuggestBox");
				binding.Path = propertyPath;
				binding.Source = this;
				BindingOperations.SetBinding(autoSuggestBoxContentControl, ContentControl.ContentProperty, binding);
			}
		}

		void UpdateHeaderVisibility()
		{
			if (!m_appliedTemplate)
			{
				return;
			}

			UpdateHeaderVisibility(DisplayMode);
		}

		void UpdateHeaderVisibility(NavigationViewDisplayMode displayMode)
		{
			bool showHeader = AlwaysShowHeader || displayMode == NavigationViewDisplayMode.Minimal;
			// Like bug 17517627, Customer like WallPaper Studio 10 expects a HeaderContent visual even if Header() is null.
			// App crashes when they have dependency on that visual, but the crash is not directly state that it's a header problem.
			// NavigationView doesn't use quirk, but we determine the version by themeresource.
			// As a workaround, we 'quirk' it for RS4 or before release. if it's RS4 or before, HeaderVisible is not related to Header().
			// If theme resource is RS5 or later, we will not show header if header is null.
			if (!ShouldPreserveNavigationViewRS4Behavior())
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

		void UpdatePaneToggleSize()
		{
			if (!ShouldPreserveNavigationViewRS3Behavior())
			{
				var splitView = m_rootSplitView;
				if (splitView != null)
				{
					var width = ResourceResolver.ResolveTopLevelResourceDouble(key: "PaneToggleButtonWidth", fallbackValue: c_paneToggleButtonWidth);
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

					if (!m_isClosedCompact && PaneTitle?.Length > 0)
					{
						// if splitView.IsPaneOpen changed directly by SplitView,
						// the two-way binding haven't sync'd up IsPaneOpen yet.
						var isPaneOpen = m_isOpenPaneForInteraction ? IsPaneOpen : splitView.IsPaneOpen;
						if (splitView.DisplayMode == SplitViewDisplayMode.Overlay && isPaneOpen)
						{
							width = OpenPaneLength;
							togglePaneButtonWidth = OpenPaneLength - (ShouldShowBackButton() ? c_backButtonWidth : 0);
						}
						else if (!(splitView.DisplayMode == SplitViewDisplayMode.Overlay && !isPaneOpen))
						{
							width = OpenPaneLength;
							togglePaneButtonWidth = OpenPaneLength;
						}
					}

					var buttonHolderGrid = m_buttonHolderGrid;
					if (buttonHolderGrid != null)
					{
						buttonHolderGrid.Width = width;
					}

					var toggleButton = m_paneToggleButton;
					if (toggleButton != null)
					{
						toggleButton.Width = togglePaneButtonWidth;
					}
				}
			}
		}

		void UpdateBackButtonVisibility()
		{
			if (!m_appliedTemplate)
			{
				return;
			}

			var shouldShowBackButton = ShouldShowBackButton();
			var visibility = Util.VisibilityFromBool(shouldShowBackButton);
			GetTemplateSettings().BackButtonVisibility = visibility;

			var backButton = m_backButton;
			if (backButton != null && ShouldPreserveNavigationViewRS4Behavior())
			{
				backButton.Visibility = visibility;
			}

			var paneContentGridAsUIE = m_paneContentGrid;
			if (paneContentGridAsUIE != null)
			{
				if (paneContentGridAsUIE is Grid paneContentGrid)
				{
					var rowDefs = paneContentGrid.RowDefinitions;
					var rowDef = rowDefs[c_backButtonRowDefinition];

					int backButtonRowHeight = 0;
					if (!IsOverlay() && ShouldShowBackButton())
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

			if (!ShouldPreserveNavigationViewRS4Behavior())
			{
				VisualStateManager.GoToState(this, shouldShowBackButton ? "BackButtonVisible" : "BackButtonCollapsed", false /*useTransitions*/);
			}
			UpdateTitleBarPadding();
		}

		void UpdatePaneTitleMargins()
		{
			var textBlock = m_paneTitleTextBlock;
			if (textBlock != null)
			{
				double width = 0;

				var buttonSize = ResourceResolver.ResolveTopLevelResourceDouble(key: "PaneToggleButtonWidth", fallbackValue: c_paneToggleButtonWidth);

				width += buttonSize;

				if (ShouldShowBackButton() && IsOverlay())
				{
					width += c_backButtonWidth;
				}

				textBlock.Margin = new Thickness(width, 0, 0, 0); // see "Hamburger title" on uni
			}
		}

		void UpdateLeftNavListViewItemSource(object items)
		{
			UpdateListViewItemsSource(m_leftNavListView, items);
		}

		void UpdateTopNavListViewItemSource(IEnumerable items)
		{
			if (m_topDataProvider.ShouldChangeDataSource(items))
			{
				// unbinding Data from ListView
				UpdateListViewItemsSource(m_topNavListView, null);
				UpdateListViewItemsSource(m_topNavListOverflowView, null);

				// Change data source and setup vectors
				m_topDataProvider.SetDataSource(items);

				// rebinding
				if (items != null)
				{
					UpdateListViewItemsSource(m_topNavListView, m_topDataProvider.GetPrimaryItems());
					UpdateListViewItemsSource(m_topNavListOverflowView, m_topDataProvider.GetOverflowItems());
				}
				else
				{
					UpdateListViewItemsSource(m_topNavListView, null);
					UpdateListViewItemsSource(m_topNavListOverflowView, null);
				}
			}
		}

		void UpdateListViewItemSource()
		{
			if (!m_appliedTemplate)
			{
				return;
			}

			var dataSource = MenuItemsSource;
			if (dataSource == null)
			{
				dataSource = MenuItems;
			}

			// Always unset the data source first from old ListView, then set data source for new ListView.
			if (IsTopNavigationView())
			{
				UpdateLeftNavListViewItemSource(null);
				UpdateTopNavListViewItemSource(dataSource as IEnumerable);
			}
			else
			{
				UpdateTopNavListViewItemSource(null);
				UpdateLeftNavListViewItemSource(dataSource as IEnumerable);
			}

			if (IsTopNavigationView())
			{
				InvalidateTopNavPrimaryLayout();
				UpdateSelectedItem();
			}
		}

		void UpdateListViewItemsSource(ListView listView,
			object itemsSource)
		{
			if (listView != null)
			{
				var oldItemsSource = listView.ItemsSource;
				if (oldItemsSource != itemsSource)
				{
					listView.ItemsSource = itemsSource;
				}
			}
		}

		void OnTitleBarMetricsChanged(object sender, object args)
		{
			UpdateTitleBarPadding();
		}

		void OnTitleBarIsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
		{
			UpdateTitleBarPadding();
		}

		bool ShouldIgnoreMeasureOverride()
		{
			return m_shouldIgnoreNextMeasureOverride || m_shouldIgnoreOverflowItemSelectionChange || m_shouldIgnoreNextSelectionChange;
		}

		bool NeedTopPaddingForRS5OrHigher(CoreApplicationViewTitleBar coreTitleBar)
		{
			// Starting on RS5, we will be using the following IsVisible API together with ExtendViewIntoTitleBar
			// to decide whether to try to add top padding or not.
			// We don't add padding when in fullscreen or tablet mode.
			return coreTitleBar.IsVisible && coreTitleBar.ExtendViewIntoTitleBar
				&& !IsFullScreenOrTabletMode();
		}

		void UpdateTitleBarPadding()
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

				if (needsTopPadding)
				{
					topPadding = coreTitleBar.Height;

					// Only add extra padding if the NavView is the "root" of the app,
					// but not if the app is expanding into the titlebar
					if (XamlRoot?.HostWindow?.Content is { } root)
					{
						GeneralTransform gt = TransformToVisual(root);
						Point pos = gt.TransformPoint(new Point(0.0f, 0.0f));
						if (pos.Y != 0.0f)
						{
							topPadding = 0.0;
						}
					}

					var backButtonVisibility = IsBackButtonVisible;
					if (ShouldPreserveNavigationViewRS4Behavior())
					{
						var fe = m_togglePaneTopPadding;
						if (fe != null)
						{
							fe.Height = topPadding;
						}

						var feTop = m_contentPaneTopPadding;
						if (feTop != null)
						{
							feTop.Height = topPadding;
						}
					}
				}

				var paneButton = m_paneToggleButton;
				if (paneButton != null)
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
					paneButton.Margin = thickness;
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

		void UpdateSelectedItem()
		{
			var item = SelectedItem;
			var settingsItem = m_settingsItem;
			if (settingsItem != null && item == settingsItem)
			{
				OnSettingsInvoked();
			}
			else
			{
				var lv = m_leftNavListView;
				if (IsTopNavigationView())
				{
					lv = m_topNavListView;
				}

				if (lv != null)
				{
					lv.SelectedItem = item;
				}
			}
		}

		void RaiseDisplayModeChanged(NavigationViewDisplayMode displayMode)
		{
			SetValue(DisplayModeProperty, displayMode);
			var eventArgs = new NavigationViewDisplayModeChangedEventArgs();
			eventArgs.DisplayMode = displayMode;
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

		bool IsFullScreenOrTabletMode()
		{
			// ApplicationView.GetForCurrentView() is an expensive call - make sure to cache the ApplicationView
			if (m_applicationView == null)
			{
				m_applicationView = ApplicationView.GetForCurrentView();
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


		T GetContainerForData<T>(object data) where T : class
		{
			if (data == null)
			{
				return null;
			}

			if (data is T nvi)
			{
				return nvi;
			}

			var lv = IsTopNavigationView() ? m_topNavListView : m_leftNavListView;
			if (lv != null)
			{
				var itemContainer = lv.ContainerFromItem(data);
				if (itemContainer != null)
				{
					return itemContainer as T;
				}
			}

			var settingsItem = m_settingsItem;
			if (settingsItem != null)
			{
				if (settingsItem == data || settingsItem.Content == data)
				{
					return settingsItem as T;
				}
			}

			return null;
		}
	}
}
