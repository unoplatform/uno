// MUX reference NavigationViewItem.cpp, commit 4fe1fd5

#if __ANDROID__
// For performance considerations, we prefer to delay pressed and over state in order to avoid
// visual state updates when starting scroll start or while scrolling, especially with touch.
// This has a great impact on Android where ScrollViewer does not capture pointer while scrolling.
#define UNO_USE_DEFERRED_VISUAL_STATES
#endif

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Devices.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using static Microsoft.UI.Xaml.Controls._Tracing;
using FlyoutBase = Windows.UI.Xaml.Controls.Primitives.FlyoutBase;
using FlyoutBaseClosingEventArgs = Windows.UI.Xaml.Controls.Primitives.FlyoutBaseClosingEventArgs;
using NavigationViewItemAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.NavigationViewItemAutomationPeer;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NavigationViewItem : NavigationViewItemBase
	{
		private const string c_navigationViewItemPresenterName = "NavigationViewItemPresenter";
		private const string c_repeater = "NavigationViewItemMenuItemsHost";
		private const string c_rootGrid = "NVIRootGrid";
		private const string c_flyoutContentGrid = "FlyoutContentGrid";

		// Visual States
		private const string c_pressedSelected = "PressedSelected";
		private const string c_pointerOverSelected = "PointerOverSelected";
		private const string c_selected = "Selected";
		private const string c_pressed = "Pressed";
		private const string c_pointerOver = "PointerOver";
		private const string c_disabled = "Disabled";
		private const string c_enabled = "Enabled";
		private const string c_normal = "Normal";
		private const string c_chevronHidden = "ChevronHidden";
		private const string c_chevronVisibleOpen = "ChevronVisibleOpen";
		private const string c_chevronVisibleClosed = "ChevronVisibleClosed";

		public NavigationViewItem()
		{
			DefaultStyleKey = typeof(NavigationViewItem);
			SetValue(MenuItemsProperty, new List<object>());
		}

		internal void UpdateVisualStateNoTransition()
		{
			UpdateVisualState(false /*useTransition*/);
		}

		protected override void OnNavigationViewItemBaseDepthChanged()
		{
			UpdateItemIndentation();
			PropagateDepthToChildren(Depth + 1);
		}

		protected override void OnNavigationViewItemBaseIsSelectedChanged()
		{
			UpdateVisualStateForPointer();
		}

		protected override void OnNavigationViewItemBasePositionChanged()
		{
			UpdateVisualStateNoTransition();
			ReparentRepeater();
		}

		protected override void OnApplyTemplate()
		{
			// TODO: Uno specific: NavigationView may not be set yet, wait for later #4689
			if (GetNavigationView() is null)
			{
				// Postpone template application for later
				return;
			}

			// Stop UpdateVisualState before template is applied. Otherwise the visuals may be unexpected
			m_appliedTemplate = false;

			UnhookEventsAndClearFields();

			base.OnApplyTemplate();

			// Find selection indicator
			// Retrieve pointers to stable controls 
			//IControlProtected controlProtected = this;
			m_helper.Init(this);

			var rootGrid = GetTemplateChild(c_rootGrid) as Grid;
			if (rootGrid != null)
			{
				m_rootGrid = rootGrid;

				var flyoutBase = FlyoutBase.GetAttachedFlyout(rootGrid);
				if (flyoutBase != null)
				{
					flyoutBase.Closing += OnFlyoutClosing;
					m_flyoutClosingRevoker.Disposable = Disposable.Create(() => flyoutBase.Closing -= OnFlyoutClosing);
				}
			}

			HookInputEvents();

			IsEnabledChanged += OnIsEnabledChanged;
			m_isEnabledChangedRevoker.Disposable = Disposable.Create(() => IsEnabledChanged -= OnIsEnabledChanged);

			m_toolTip = (ToolTip)GetTemplateChild("ToolTip");

			var splitView = GetSplitView();
			if (splitView != null)
			{
				var splitViewIsPaneOpenChangedSubscription = splitView.RegisterPropertyChangedCallback(
					SplitView.IsPaneOpenProperty, OnSplitViewPropertyChanged);
				m_splitViewIsPaneOpenChangedRevoker.Disposable = Disposable.Create(
					() => splitView.UnregisterPropertyChangedCallback(SplitView.IsPaneOpenProperty, splitViewIsPaneOpenChangedSubscription));
				var splitViewDisplayModeChangedSubscription = splitView.RegisterPropertyChangedCallback(
					SplitView.DisplayModeProperty, OnSplitViewPropertyChanged);
				m_splitViewDisplayModeChangedRevoker.Disposable = Disposable.Create(
					() => splitView.UnregisterPropertyChangedCallback(SplitView.DisplayModeProperty, splitViewDisplayModeChangedSubscription));
				var splitViewCompactPaneLengthSubsctiption = splitView.RegisterPropertyChangedCallback(
					SplitView.CompactPaneLengthProperty, OnSplitViewPropertyChanged);
				m_splitViewCompactPaneLengthChangedRevoker.Disposable = Disposable.Create(
					() => splitView.UnregisterPropertyChangedCallback(SplitView.CompactPaneLengthProperty, splitViewCompactPaneLengthSubsctiption));

				UpdateCompactPaneLength();
				UpdateIsClosedCompact();
			}

			// Retrieve reference to NavigationView
			var nvImpl = GetNavigationView();
			if (nvImpl != null)
			{
				var repeater = GetTemplateChild(c_repeater) as ItemsRepeater;
				if (repeater != null)
				{
					m_repeater = repeater;

					// Primary element setup happens in NavigationView
					repeater.ElementPrepared += nvImpl.OnRepeaterElementPrepared;
					m_repeaterElementPreparedRevoker.Disposable = Disposable.Create(() => repeater.ElementPrepared -= nvImpl.OnRepeaterElementPrepared);
					repeater.ElementClearing += nvImpl.OnRepeaterElementClearing;
					m_repeaterElementClearingRevoker.Disposable = Disposable.Create(() => repeater.ElementClearing -= nvImpl.OnRepeaterElementClearing);

					repeater.ItemTemplate = nvImpl.GetNavigationViewItemsFactory();
				}

				UpdateRepeaterItemsSource();
			}

			m_flyoutContentGrid = (Grid)GetTemplateChild(c_flyoutContentGrid);

			m_appliedTemplate = true;

			UpdateItemIndentation();
			UpdateVisualStateNoTransition();
			ReparentRepeater();
			// We dont want to update the repeater visibilty during OnApplyTemplate if NavigationView is in a mode when items are shown in a flyout
			if (!ShouldRepeaterShowInFlyout())
			{
				ShowHideChildren();
			}

			var visual = ElementCompositionPreview.GetElementVisual(this);
			NavigationView.CreateAndAttachHeaderAnimation(visual);

			_fullyInitialized = true;
		}

		private void UpdateRepeaterItemsSource()
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				object GetItemsSource()
				{
					var menuItemsSource = MenuItemsSource;
					if (menuItemsSource != null)
					{
						return menuItemsSource;
					}
					return MenuItems;
				}
				var itemsSource = GetItemsSource();
				m_itemsSourceViewCollectionChangedRevoker.Disposable = null;
				repeater.ItemsSource = itemsSource;
				repeater.ItemsSourceView.CollectionChanged += OnItemsSourceViewChanged;
				m_itemsSourceViewCollectionChangedRevoker.Disposable = Disposable.Create(() => repeater.ItemsSourceView.CollectionChanged -= OnItemsSourceViewChanged);
			}
		}

		private void OnItemsSourceViewChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			UpdateVisualStateForChevron();
		}

		internal UIElement GetSelectionIndicator()
		{
			var selectIndicator = m_helper.GetSelectionIndicator();
			var presenter = GetPresenter();
			if (presenter != null)
			{
				selectIndicator = presenter.GetSelectionIndicator();
			}
			return selectIndicator;
		}

		private void OnSplitViewPropertyChanged(DependencyObject sender, DependencyProperty args)
		{
			if (args == SplitView.CompactPaneLengthProperty)
			{
				UpdateCompactPaneLength();
			}
			else if (args == SplitView.IsPaneOpenProperty ||
				args == SplitView.DisplayModeProperty)
			{
				UpdateIsClosedCompact();
				ReparentRepeater();
			}
		}

		private void UpdateCompactPaneLength()
		{
			var splitView = GetSplitView();
			if (splitView != null)
			{
				SetValue(CompactPaneLengthProperty, splitView.CompactPaneLength); //PropertyValue.CreateDouble(splitView.CompactPaneLength));

				// Only update when on left
				var presenter = GetPresenter();
				if (presenter != null)
				{
					presenter.UpdateCompactPaneLength(splitView.CompactPaneLength, IsOnLeftNav());
				}
			}
		}

		private void UpdateIsClosedCompact()
		{
			var splitView = GetSplitView(); if (splitView != null)
			{
				// Check if the pane is closed and if the splitview is in either compact mode.
				m_isClosedCompact = !splitView.IsPaneOpen
					&& (splitView.DisplayMode == SplitViewDisplayMode.CompactOverlay || splitView.DisplayMode == SplitViewDisplayMode.CompactInline);

				UpdateVisualState(true /*useTransitions*/);

				var presenter = GetPresenter(); if (presenter != null)
				{
					presenter.UpdateClosedCompactVisualState(IsTopLevelItem, m_isClosedCompact);
				}
			}
		}

		private void UpdateNavigationViewItemToolTip()
		{
			var toolTipContent = ToolTipService.GetToolTip(this);

			// no custom tooltip, then use suggested tooltip
			if (toolTipContent == null || toolTipContent == m_suggestedToolTipContent)
			{
				if (ShouldEnableToolTip())
				{
					ToolTipService.SetToolTip(this, m_suggestedToolTipContent);
				}
				else
				{
					ToolTipService.SetToolTip(this, null);
				}
			}
		}

		private void SuggestedToolTipChanged(object newContent)
		{
			var potentialString = newContent as IPropertyValue;
			bool stringableToolTip = (potentialString != null && potentialString.Type == PropertyType.String);

			object newToolTipContent = null;
			if (stringableToolTip)
			{
				newToolTipContent = newContent;
			}

			// Both customer and NavigationViewItem can update ToolTipContent by ToolTipService.SetToolTip or XAML
			// If the ToolTipContent is not the same as m_suggestedToolTipContent, then it's set by customer.
			// Customer's ToolTip take high priority, and we never override Customer's ToolTip.
			var toolTipContent = ToolTipService.GetToolTip(this);
			var oldToolTipContent = m_suggestedToolTipContent;
			if (oldToolTipContent != null)
			{
				if (oldToolTipContent == toolTipContent)
				{
					ToolTipService.SetToolTip(this, null);
				}
			}

			m_suggestedToolTipContent = newToolTipContent;
		}

		private void OnIsExpandedPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			AutomationPeer peer = FrameworkElementAutomationPeer.FromElement(this);
			if (peer != null)
			{
				var navViewItemPeer = (NavigationViewItemAutomationPeer)peer;
				navViewItemPeer.RaiseExpandCollapseAutomationEvent(
					IsExpanded ?
						ExpandCollapseState.Expanded :
						ExpandCollapseState.Collapsed
				);
			}
		}

		private void OnIconPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateVisualStateNoTransition();
		}

		private void OnMenuItemsPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateRepeaterItemsSource();
			UpdateVisualStateForChevron();
		}

		private void OnMenuItemsSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateRepeaterItemsSource();
			UpdateVisualStateForChevron();
		}

		private void OnHasUnrealizedChildrenPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateVisualStateForChevron();
		}

		private void ShowSelectionIndicator(bool visible)
		{
			var selectionIndicator = GetSelectionIndicator();
			if (selectionIndicator != null)
			{
				selectionIndicator.Opacity = visible ? 1.0 : 0.0;
			}
		}

		private void UpdateVisualStateForIconAndContent(bool showIcon, bool showContent)
		{
			var presenter = m_navigationViewItemPresenter;
			if (presenter != null)
			{
				var stateName = showIcon ? (showContent ? "IconOnLeft" : "IconOnly") : "ContentOnly";
				VisualStateManager.GoToState(presenter, stateName, false /*useTransitions*/);
			}
		}

		private void UpdateVisualStateForNavigationViewPositionChange()
		{
			var position = Position;
			var stateName = NavigationViewItemHelper.c_OnLeftNavigation;

			bool handled = false;

			switch (position)
			{
				case NavigationViewRepeaterPosition.LeftNav:
				case NavigationViewRepeaterPosition.LeftFooter:
					if (SharedHelpers.IsRS4OrHigher() && Application.Current.FocusVisualKind == FocusVisualKind.Reveal)
					{
						// OnLeftNavigationReveal is introduced in RS6. 
						// Will fallback to stateName for the customer who re-template rs5 NavigationViewItem
						if (VisualStateManager.GoToState(this, NavigationViewItemHelper.c_OnLeftNavigationReveal, false /*useTransitions*/))
						{
							handled = true;
						}
					}
					break;
				case NavigationViewRepeaterPosition.TopPrimary:
				case NavigationViewRepeaterPosition.TopFooter:
					if (SharedHelpers.IsRS4OrHigher() && Application.Current.FocusVisualKind == FocusVisualKind.Reveal)
					{
						stateName = NavigationViewItemHelper.c_OnTopNavigationPrimaryReveal;
					}
					else
					{
						stateName = NavigationViewItemHelper.c_OnTopNavigationPrimary;
					}
					break;
				case NavigationViewRepeaterPosition.TopOverflow:
					stateName = NavigationViewItemHelper.c_OnTopNavigationOverflow;
					break;
			}

			if (!handled)
			{
				VisualStateManager.GoToState(this, stateName, false /*useTransitions*/);
			}
		}

		private void UpdateVisualStateForKeyboardFocusedState()
		{
			var focusState = "KeyboardNormal";
			if (m_hasKeyboardFocus)
			{
				focusState = "KeyboardFocused";
			}

			VisualStateManager.GoToState(this, focusState, false /*useTransitions*/);
		}

		private void UpdateVisualStateForToolTip()
		{
			// Since RS5, ToolTip apply to NavigationViewItem directly to make Keyboard focus has tooltip too.
			// If ToolTip TemplatePart is detected, fallback to old logic and apply ToolTip on TemplatePart.
			var toolTip = m_toolTip; if (toolTip != null)
			{
				var shouldEnableToolTip = ShouldEnableToolTip();
				var toolTipContent = m_suggestedToolTipContent;
				if (shouldEnableToolTip && toolTipContent != null)
				{
					toolTip.Content = toolTipContent;
					toolTip.IsEnabled = true;
				}
				else
				{
					toolTip.Content = null;
					toolTip.IsEnabled = false;
				}
			}

			else
			{
				UpdateNavigationViewItemToolTip();
			}
		}

		private void UpdateVisualStateForPointer()
		{
			var isEnabled = IsEnabled;
			var enabledStateValue = isEnabled ? c_enabled : c_disabled;

			string GetSelectedStateValue(bool isEnabled, bool isSelected)
			{
				if (isEnabled)
				{
					if (isSelected)
					{
						if (m_isPressed && !_uno_isDefferingPressedState)
						{
							return c_pressedSelected;
						}
						else if (m_isPointerOver && !_uno_isDefferingOverState)
						{
							return c_pointerOverSelected;
						}
						else
						{
							return c_selected;
						}
					}
					else if (m_isPointerOver && !_uno_isDefferingOverState)
					{
						if (m_isPressed && !_uno_isDefferingPressedState)
						{
							return c_pressed;
						}
						else
						{
							return c_pointerOver;
						}
					}
					else if (m_isPressed && !_uno_isDefferingPressedState)
					{
						return c_pressed;
					}
				}
				else
				{
					if (isSelected)
					{
						return c_selected;
					}
				}
				return c_normal;
			}
			// DisabledStates and CommonStates
			var selectedStateValue = GetSelectedStateValue(isEnabled, IsSelected);

			// There are scenarios where the presenter may not exist.
			// For example, the top nav settings item. In that case,
			// update the states for the item itself.
			var presenter = m_navigationViewItemPresenter;
			if (presenter != null)
			{
				VisualStateManager.GoToState(presenter, enabledStateValue, true);
				VisualStateManager.GoToState(presenter, selectedStateValue, true);
			}

			else
			{
				VisualStateManager.GoToState(this, enabledStateValue, true);
				VisualStateManager.GoToState(this, selectedStateValue, true);
			}
		}

		// TODO: Override?
		private new void UpdateVisualState(bool useTransitions)
		{
			if (!m_appliedTemplate)
				return;

			UpdateVisualStateForPointer();

			UpdateVisualStateForNavigationViewPositionChange();

			bool shouldShowIcon = ShouldShowIcon();
			bool shouldShowContent = ShouldShowContent();

			if (IsOnLeftNav())
			{
				var presenter = m_navigationViewItemPresenter;
				if (presenter != null)
				{
					// Backward Compatibility with RS4-, new implementation prefer IconOnLeft/IconOnly/ContentOnly
					VisualStateManager.GoToState(presenter, shouldShowIcon ? "IconVisible" : "IconCollapsed", useTransitions);
				}
			}

			UpdateVisualStateForToolTip();

			UpdateVisualStateForIconAndContent(shouldShowIcon, shouldShowContent);

			// visual state for focus state. top navigation use it to provide different visual for selected and selected+focused
			UpdateVisualStateForKeyboardFocusedState();

			UpdateVisualStateForChevron();
		}

		private void UpdateVisualStateForChevron()
		{
			var presenter = m_navigationViewItemPresenter;
			if (presenter != null)
			{
				var chevronState = HasChildren() && !(m_isClosedCompact && ShouldRepeaterShowInFlyout()) ? (IsExpanded ? c_chevronVisibleOpen : c_chevronVisibleClosed) : c_chevronHidden;
				VisualStateManager.GoToState(presenter, chevronState, true);
			}
		}

		internal bool HasChildren()
		{
			return MenuItems.Count > 0
				|| (MenuItemsSource != null && m_repeater != null && m_repeater.ItemsSourceView.Count > 0)
				|| HasUnrealizedChildren;
		}

		private bool ShouldShowIcon()
		{
			return Icon != null;
		}

		private bool ShouldEnableToolTip()
		{
			// We may enable Tooltip for IconOnly in the future, but not now
			return IsOnLeftNav() && m_isClosedCompact;
		}

		private bool ShouldShowContent()
		{
			return Content != null;
		}

		public bool IsOnLeftNav()
		{
			var position = Position;
			return position == NavigationViewRepeaterPosition.LeftNav || position == NavigationViewRepeaterPosition.LeftFooter;
		}

		private bool IsOnTopPrimary()
		{
			return Position == NavigationViewRepeaterPosition.TopPrimary;
		}

		private UIElement GetPresenterOrItem()
		{
			var presenter = m_navigationViewItemPresenter;
			if (presenter != null)
			{
				return presenter as UIElement;
			}

			else
			{
				return this as UIElement;
			}
		}

		private NavigationViewItemPresenter GetPresenter()
		{
			NavigationViewItemPresenter presenter = null;
			if (m_navigationViewItemPresenter != null)
			{
				presenter = m_navigationViewItemPresenter;
			}
			return presenter;
		}

		internal void ShowHideChildren()
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				bool shouldShowChildren = IsExpanded;
				var visibility = shouldShowChildren ? Visibility.Visible : Visibility.Collapsed;
				repeater.Visibility = visibility;

				if (ShouldRepeaterShowInFlyout())
				{
					if (shouldShowChildren)
					{
						// Verify that repeater is parented correctly
						if (!m_isRepeaterParentedToFlyout)
						{
							ReparentRepeater();
						}

						// There seems to be a race condition happening which sometimes
						// prevents the opening of the flyout. Queue callback as a workaround.

						// TODO: Uno specific - Queue callback for composition rendering is not implemented yet - #4690
						//SharedHelpers.QueueCallbackForCompositionRendering(() =>
						//{
							FlyoutBase.ShowAttachedFlyout(m_rootGrid);
						//});
					}
					else
					{
						var flyout = FlyoutBase.GetAttachedFlyout(m_rootGrid);
						if (flyout != null)
						{
							flyout.Hide();
						}
					}
				}
			}
		}

		private void ReparentRepeater()
		{
			if (HasChildren())
			{
				var repeater = m_repeater; if (repeater != null)
				{
					if (ShouldRepeaterShowInFlyout() && !m_isRepeaterParentedToFlyout)
					{
						// Reparent repeater to flyout
						m_rootGrid.Children.RemoveAt(m_rootGrid.Children.Count - 1);
						m_flyoutContentGrid.Children.Add(repeater);
						m_isRepeaterParentedToFlyout = true;

						PropagateDepthToChildren(0);
					}
					else if (!ShouldRepeaterShowInFlyout() && m_isRepeaterParentedToFlyout)
					{
						m_flyoutContentGrid.Children.RemoveAt(m_flyoutContentGrid.Children.Count - 1);
						m_rootGrid.Children.Add(repeater);
						m_isRepeaterParentedToFlyout = false;

						PropagateDepthToChildren(1);
					}
				}
			}
		}

		internal bool ShouldRepeaterShowInFlyout()
		{
			return (m_isClosedCompact && IsTopLevelItem) || IsOnTopPrimary();
		}

		internal bool IsRepeaterVisible()
		{
			var repeater = m_repeater; if (repeater != null)
			{
				return repeater.Visibility == Visibility.Visible;
			}
			return false;
		}

		private void UpdateItemIndentation()
		{
			// Update item indentation based on its depth
			var presenter = m_navigationViewItemPresenter;
			if (presenter != null)
			{
				var newLeftMargin = Depth * c_itemIndentation;
				presenter.UpdateContentLeftIndentation((double)(newLeftMargin));
			}
		}

		internal void PropagateDepthToChildren(int depth)
		{
			var repeater = m_repeater; if (repeater != null)
			{
				var itemsCount = repeater.ItemsSourceView.Count;
				for (int index = 0; index < itemsCount; index++)
				{
					var element = repeater.TryGetElement(index);
					if (element != null)
					{
						var nvib = element as NavigationViewItemBase;
						if (nvib != null)
						{
							nvib.Depth = depth;
						}
					}
				}
			}
		}

		internal void OnExpandCollapseChevronTapped(object sender, TappedRoutedEventArgs args)
		{
			IsExpanded = !IsExpanded;
			args.Handled = true;
		}

		private void OnFlyoutClosing(object sender, FlyoutBaseClosingEventArgs args)
		{
			IsExpanded = false;
		}

		// UIElement / UIElementOverridesHelper
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new NavigationViewItemAutomationPeer(this);
		}

		// IContentControlOverrides / IContentControlOverridesHelper
		protected override void OnContentChanged(object oldContent, object newContent)
		{
			base.OnContentChanged(oldContent, newContent);
			SuggestedToolTipChanged(newContent);
			UpdateVisualStateNoTransition();

			if (!IsOnLeftNav())
			{
				// Content has changed for the item, so we want to trigger a re-measure
				var navView = GetNavigationView();
				if (navView != null)
				{
					navView.TopNavigationViewItemContentChanged();
				}
			}
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			var originalSource = e.OriginalSource as Control;
			if (originalSource != null)
			{
				// It's used to support bluebar have difference appearance between focused and focused+selection. 
				// For example, we can move the SelectionIndicator 3px up when focused and selected to make sure focus rectange doesn't override SelectionIndicator. 
				// If it's a pointer or programatic, no focus rectangle, so no action
				var focusState = originalSource.FocusState;
				if (focusState == FocusState.Keyboard)
				{
					m_hasKeyboardFocus = true;
					UpdateVisualStateNoTransition();
				}
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			if (m_hasKeyboardFocus)
			{
				m_hasKeyboardFocus = false;
				UpdateVisualStateNoTransition();
			}
		}

		private void ResetTrackedPointerId()
		{
			m_trackedPointerId = 0;
		}

		// Returns False when the provided pointer Id matches the currently tracked Id.
		// When there is no currently tracked Id, sets the tracked Id to the provided Id and returns False.
		// Returns True when the provided pointer Id does not match the currently tracked Id.
		private bool IgnorePointerId(PointerRoutedEventArgs args)
		{
			uint pointerId = args.Pointer.PointerId;

			if (m_trackedPointerId == 0)
			{
				m_trackedPointerId = pointerId;
			}
			else if (m_trackedPointerId != pointerId)
			{
				return true;
			}
			return false;
		}

		private void OnPresenterPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			if (IgnorePointerId(args))
			{
				return;
			}

			MUX_ASSERT(!m_isPressed);
			MUX_ASSERT(m_capturedPointer == null);

			// WinUI TODO: Update to look at presenter instead
			var pointerProperties = args.GetCurrentPoint(this).Properties;
			m_isPressed = pointerProperties.IsLeftButtonPressed || pointerProperties.IsRightButtonPressed;

			var pointer = args.Pointer;
			var presenter = GetPresenterOrItem();

			MUX_ASSERT(presenter != null);

			if (presenter.CapturePointer(pointer))
			{
				m_capturedPointer = pointer;
			}

#if UNO_USE_DEFERRED_VISUAL_STATES
			_uno_isDefferingPressedState = true;
			DeferUpdateVisualStateForPointer();
#endif

			UpdateVisualState(true);
		}

		private void OnPresenterPointerReleased(object sender, PointerRoutedEventArgs args)
		{
			if (IgnorePointerId(args))
			{
				return;
			}

			if (m_isPressed)
			{
				m_isPressed = false;

				if (m_capturedPointer != null)
				{
					var presenter = GetPresenterOrItem();

					MUX_ASSERT(presenter != null);

					presenter.ReleasePointerCapture(m_capturedPointer);
				}

				UpdateVisualState(true);
			}
		}

		private void OnPresenterPointerEntered(object sender, PointerRoutedEventArgs args)
		{
#if UNO_USE_DEFERRED_VISUAL_STATES
			_uno_isDefferingOverState = args.Pointer.PointerDeviceType != PointerDeviceType.Mouse;
			DeferUpdateVisualStateForPointer();
#endif

			ProcessPointerOver(args);
		}

		private void OnPresenterPointerMoved(object sender, PointerRoutedEventArgs args)
		{
			ProcessPointerOver(args);
		}

		private void OnPresenterPointerExited(object sender, PointerRoutedEventArgs args)
		{
			if (IgnorePointerId(args))
			{
				return;
			}

			m_isPointerOver = false;

			if (m_capturedPointer == null)
			{
				ResetTrackedPointerId();
			}

			UpdateVisualState(true);
		}

		private void OnPresenterPointerCanceled(object sender, PointerRoutedEventArgs args)
		{
			ProcessPointerCanceled(args);
		}

		private void OnPresenterPointerCaptureLost(object sender, PointerRoutedEventArgs args)
		{
			ProcessPointerCanceled(args);
		}

		private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			if (!IsEnabled)
			{
				m_isPressed = false;
				m_isPointerOver = false;

				if (m_capturedPointer != null)
				{
					var presenter = GetPresenterOrItem();

					MUX_ASSERT(presenter != null);

					presenter.ReleasePointerCapture(m_capturedPointer);
					m_capturedPointer = null;
				}

				ResetTrackedPointerId();
			}

			UpdateVisualState(true);
		}

		internal void RotateExpandCollapseChevron(bool isExpanded)
		{
			var presenter = GetPresenter(); if (presenter != null)
			{
				presenter.RotateExpandCollapseChevron(isExpanded);
			}
		}

		private void ProcessPointerCanceled(PointerRoutedEventArgs args)
		{
			if (IgnorePointerId(args))
			{
				return;
			}

			_uno_isDefferingPressedState = false;
			_uno_isDefferingOverState = false;
			_uno_pointerDeferring?.Stop();

			m_isPressed = false;
			m_isPointerOver = false;
			m_capturedPointer = null;
			ResetTrackedPointerId();
			UpdateVisualState(true);
		}

		private void ProcessPointerOver(PointerRoutedEventArgs args)
		{
			if (IgnorePointerId(args))
			{
				return;
			}

			if (!m_isPointerOver)
			{
				m_isPointerOver = true;
				UpdateVisualState(true);
			}
		}

		private void HookInputEvents()
		{
			UIElement GetPresenter()
			{
				var presenter = GetTemplateChild(c_navigationViewItemPresenterName) as NavigationViewItemPresenter;
				if (presenter != null)
				{
					m_navigationViewItemPresenter = presenter;
					return presenter as UIElement;
				}
				// We don't have a presenter, so we are our own presenter.
				return this as UIElement;
			}
			UIElement presenter = GetPresenter();

			MUX_ASSERT(presenter != null);

			// Handlers that set flags are skipped when args.Handled is already True.
			presenter.PointerPressed += OnPresenterPointerPressed;
			m_presenterPointerPressedRevoker.Disposable = Disposable.Create(() => presenter.PointerPressed -= OnPresenterPointerPressed);
			presenter.PointerEntered += OnPresenterPointerEntered;
			m_presenterPointerEnteredRevoker.Disposable = Disposable.Create(() => presenter.PointerEntered -= OnPresenterPointerEntered);
			presenter.PointerMoved += OnPresenterPointerMoved;
			m_presenterPointerMovedRevoker.Disposable = Disposable.Create(() => presenter.PointerMoved -= OnPresenterPointerMoved);

			// Handlers that reset flags are not skipped when args.Handled is already True to avoid broken states.
			var pointerReleasedHandler = new PointerEventHandler(OnPresenterPointerReleased);
			presenter.AddHandler(
				UIElement.PointerReleasedEvent,
				pointerReleasedHandler,
				true /*handledEventsToo*/);
			m_presenterPointerReleasedRevoker.Disposable = Disposable.Create(
				() => presenter.RemoveHandler(UIElement.PointerReleasedEvent, pointerReleasedHandler));

			var pointerExitedHandler = new PointerEventHandler(OnPresenterPointerExited);
			presenter.AddHandler(
				UIElement.PointerExitedEvent,
				pointerExitedHandler,
				true /*handledEventsToo*/);
			m_presenterPointerExitedRevoker.Disposable = Disposable.Create(
				() => presenter.RemoveHandler(UIElement.PointerExitedEvent, pointerExitedHandler));

			var pointerCanceledHandler = new PointerEventHandler(OnPresenterPointerCanceled);
			presenter.AddHandler(
				UIElement.PointerCanceledEvent,
				pointerCanceledHandler,
				true /*handledEventsToo*/);
			m_presenterPointerCanceledRevoker.Disposable = Disposable.Create(
				() => presenter.RemoveHandler(UIElement.PointerCanceledEvent, pointerCanceledHandler));

			var pointerCaptureLostHandler = new PointerEventHandler(OnPresenterPointerCaptureLost);
			presenter.AddHandler(
				UIElement.PointerCaptureLostEvent,
				pointerCaptureLostHandler,
				true /*handledEventsToo*/);
			m_presenterPointerCaptureLostRevoker.Disposable = Disposable.Create(
				() => presenter.RemoveHandler(UIElement.PointerCaptureLostEvent, pointerCaptureLostHandler));
		}

		private void UnhookInputEvents()
		{
			m_presenterPointerPressedRevoker.Disposable = null;
			m_presenterPointerEnteredRevoker.Disposable = null;
			m_presenterPointerMovedRevoker.Disposable = null;
			m_presenterPointerReleasedRevoker.Disposable = null;
			m_presenterPointerExitedRevoker.Disposable = null;
			m_presenterPointerCanceledRevoker.Disposable = null;
			m_presenterPointerCaptureLostRevoker.Disposable = null;
		}

		private void UnhookEventsAndClearFields()
		{
			UnhookInputEvents();

			m_flyoutClosingRevoker.Disposable = null;
			m_splitViewIsPaneOpenChangedRevoker.Disposable = null;
			m_splitViewDisplayModeChangedRevoker.Disposable = null;
			m_splitViewCompactPaneLengthChangedRevoker.Disposable = null;
			m_repeaterElementPreparedRevoker.Disposable = null;
			m_repeaterElementClearingRevoker.Disposable = null;
			m_isEnabledChangedRevoker.Disposable = null;
			m_itemsSourceViewCollectionChangedRevoker.Disposable = null;

			m_rootGrid = null;
			m_navigationViewItemPresenter = null;
			m_toolTip = null;
			m_repeater = null;
			m_flyoutContentGrid = null;
		}
	}
}
