using System.Collections.Generic;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;

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

		private void UpdateVisualStateNoTransition()
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
			// Stop UpdateVisualState before template is applied. Otherwise the visuals may be unexpected
			m_appliedTemplate = false;

			UnhookEventsAndClearFields();

			NavigationViewItemBase.OnApplyTemplate();

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
					m_flyoutClosingRevoker = flyoutBase.Closing(auto_revoke, { this, OnFlyoutClosing });
				}
			}

			HookInputEvents(controlProtected);

			m_isEnabledChangedRevoker = IsEnabledChanged(auto_revoke, { this,  OnIsEnabledChanged });

			m_toolTip = (ToolTip)GetTemplateChild("ToolTip");

			var splitView = GetSplitView(); if (splitView != null)
			{
				m_splitViewIsPaneOpenChangedRevoker = RegisterPropertyChanged(splitView,
					SplitView.IsPaneOpenProperty, { this, OnSplitViewPropertyChanged });
				m_splitViewDisplayModeChangedRevoker = RegisterPropertyChanged(splitView,
					SplitView.DisplayModeProperty, { this, OnSplitViewPropertyChanged });
				m_splitViewCompactPaneLengthChangedRevoker = RegisterPropertyChanged(splitView,
					SplitView.CompactPaneLengthProperty, { this, OnSplitViewPropertyChanged });

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
					m_repeaterElementPreparedRevoker = repeater.ElementPrepared(auto_revoke, { nvImpl,  &NavigationView.OnRepeaterElementPrepared });
					m_repeaterElementClearingRevoker = repeater.ElementClearing(auto_revoke, { nvImpl, &NavigationView.OnRepeaterElementClearing });

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
		}

		private void UpdateRepeaterItemsSource()
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				var itemsSource = [this]()

		{
					if (var menuItemsSource = MenuItemsSource)
            {
						return menuItemsSource;
					}
					return MenuItems;
				} ();
				m_itemsSourceViewCollectionChangedRevoker.revoke();
				repeater.ItemsSource(itemsSource);
				m_itemsSourceViewCollectionChangedRevoker = repeater.ItemsSourceView.CollectionChanged(auto_revoke, { this, OnItemsSourceViewChanged });
			}
		}

		void OnItemsSourceViewChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			UpdateVisualStateForChevron();
		}

		UIElement GetSelectionIndicator()
		{
			var selectIndicator = m_helper.GetSelectionIndicator();
			var presenter = GetPresenter();
			if (presenter != null)
			{
				selectIndicator = presenter.GetSelectionIndicator();
			}
			return selectIndicator;
		}

		void OnSplitViewPropertyChanged(DependencyObject sender, DependencyProperty args)
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
				SetValue(CompactPaneLengthProperty, PropertyValue.CreateDouble(splitView.CompactPaneLength));

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
			if (!toolTipContent || toolTipContent == m_suggestedToolTipContent)
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
				var navViewItemPeer = peer.as< NavigationViewItemAutomationPeer > ();
				get_self<NavigationViewItemAutomationPeer>(navViewItemPeer).RaiseExpandCollapseAutomationEvent(
					IsExpanded ?
						ExpandCollapseState.Expanded :
						ExpandCollapseState.Collapsed
				);
			}
		}

		void OnIconPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateVisualStateNoTransition();
		}

		void OnMenuItemsPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateRepeaterItemsSource();
			UpdateVisualStateForChevron();
		}

		void OnMenuItemsSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateRepeaterItemsSource();
			UpdateVisualStateForChevron();
		}

		void OnHasUnrealizedChildrenPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateVisualStateForChevron();
		}

		void ShowSelectionIndicator(bool visible)
		{
			var selectionIndicator = GetSelectionIndicator();
			if (selectionIndicator != null)
			{
				selectionIndicator.Opacity = visible ? 1.0 : 0.0;
			}
		}

		void UpdateVisualStateForIconAndContent(bool showIcon, bool showContent)
		{
			var presenter = m_navigationViewItemPresenter;
			if (presenter != null)
			{
				var stateName = showIcon ? (showContent ? "IconOnLeft" : "IconOnly") : "ContentOnly";
				VisualStateManager.GoToState(presenter, stateName, false /*useTransitions*/);
			}
		}

		void UpdateVisualStateForNavigationViewPositionChange()
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

		void UpdateVisualStateForKeyboardFocusedState()
		{
			var focusState = "KeyboardNormal";
			if (m_hasKeyboardFocus)
			{
				focusState = "KeyboardFocused";
			}

			VisualStateManager.GoToState(this, focusState, false /*useTransitions*/);
		}

		void UpdateVisualStateForToolTip()
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

		void UpdateVisualStateForPointer()
		{
			var isEnabled = IsEnabled;
			var enabledStateValue = isEnabled ? c_enabled : c_disabled;
			// DisabledStates and CommonStates
			var selectedStateValue = [this, isEnabled, isSelected = IsSelected]()


	{
				if (isEnabled)
				{
					if (isSelected)
					{
						if (m_isPressed)
						{
							return c_pressedSelected;
						}
						else if (m_isPointerOver)
						{
							return c_pointerOverSelected;
						}
						else
						{
							return c_selected;
						}
					}
					else if (m_isPointerOver)
					{
						if (m_isPressed)
						{
							return c_pressed;
						}
						else
						{
							return c_pointerOver;
						}
					}
					else if (m_isPressed)
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
			} ();

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

		void UpdateVisualState(bool useTransitions)
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

		bool ShouldShowIcon()
		{
			return Icon != null;
		}

		bool ShouldEnableToolTip()
		{
			// We may enable Tooltip for IconOnly in the future, but not now
			return IsOnLeftNav() && m_isClosedCompact;
		}

		bool ShouldShowContent()
		{
			return Content != null;
		}

		public bool IsOnLeftNav()
		{
			var position = Position;
			return position == NavigationViewRepeaterPosition.LeftNav || position == NavigationViewRepeaterPosition.LeftFooter;
		}

		bool IsOnTopPrimary()
		{
			return Position == NavigationViewRepeaterPosition.TopPrimary;
		}

		UIElement GetPresenterOrItem()
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
			var repeater = m_repeater; if (repeater != null)
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
						SharedHelpers.QueueCallbackForCompositionRendering(() =>
						{
							FlyoutBase.ShowAttachedFlyout(m_rootGrid);
						});
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

		void ReparentRepeater()
		{
			if (HasChildren())
			{
				var repeater = m_repeater; if (repeater != null)
				{
					if (ShouldRepeaterShowInFlyout() && !m_isRepeaterParentedToFlyout)
					{
						// Reparent repeater to flyout
						// TODO: Replace removeatend with something more specific
						m_rootGrid.Children.RemoveAtEnd();
						m_flyoutContentGrid.Children.Append(repeater);
						m_isRepeaterParentedToFlyout = true;

						PropagateDepthToChildren(0);
					}
					else if (!ShouldRepeaterShowInFlyout() && m_isRepeaterParentedToFlyout)
					{
						m_flyoutContentGrid.Children.RemoveAtEnd();
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
			if (var presenter = m_navigationViewItemPresenter)
    {
				var newLeftMargin = Depth * c_itemIndentation;
				get_self<NavigationViewItemPresenter>(presenter).UpdateContentLeftIndentation((double)(newLeftMargin));
			}
		}

		void PropagateDepthToChildren(int depth)
		{
			var repeater = m_repeater; if (repeater != null)
			{
				var itemsCount = repeater.ItemsSourceView.Count;
				for (int index = 0; index < itemsCount; index++)
				{
					if (var element = repeater.TryGetElement(index))
            {
					var nvib = element as NavigationViewItemBase;
					if (nvib != null)
                {
						get_self<NavigationViewItemBase>(nvib).Depth(depth);
					}
				}
			}
		}
	}

	void OnExpandCollapseChevronTapped(object sender, TappedRoutedEventArgs args)
	{
		IsExpanded = !IsExpanded;
		args.Handled = true;
	}

	void OnFlyoutClosing(object sender, FlyoutBaseClosingEventArgs args)
	{
		IsExpanded = false;
	}

	// UIElement / UIElementOverridesHelper
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new NavigationViewItemAutomationPeer(this);
	}

	// IContentControlOverrides / IContentControlOverridesHelper
	void OnContentChanged(object oldContent, object newContent)
	{
		NavigationViewItemBase.OnContentChanged(oldContent, newContent);
		SuggestedToolTipChanged(newContent);
		UpdateVisualStateNoTransition();

		if (!IsOnLeftNav())
		{
			// Content has changed for the item, so we want to trigger a re-measure
			if (var navView = GetNavigationView())
        {
				get_self<NavigationView>(navView).TopNavigationViewItemContentChanged();
			}
		}
	}

	private void OnGotFocus(RoutedEventArgs e)
	{
		NavigationViewItemBase.OnGotFocus(e);
		var originalSource = e.OriginalSource as Control;
		if (originalSource)
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

	private void OnLostFocus(RoutedEventArgs e)
	{
		NavigationViewItemBase.OnLostFocus(e);
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
	bool IgnorePointerId(PointerRoutedEventArgs args)
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

	void OnPresenterPointerPressed(object sender, PointerRoutedEventArgs args)
	{
		if (IgnorePointerId(args))
		{
			return;
		}

		MUX_ASSERT(!m_isPressed);
		MUX_ASSERT(!m_capturedPointer);

		// TODO: Update to look at presenter instead
		var pointerProperties = args.GetCurrentPoint(this).Properties();
		m_isPressed = pointerProperties.IsLeftButtonPressed() || pointerProperties.IsRightButtonPressed();

		var pointer = args.Pointer;
		var presenter = GetPresenterOrItem();

		MUX_ASSERT(presenter);

		if (presenter.CapturePointer(pointer))
		{
			m_capturedPointer = pointer;
		}

		UpdateVisualState(true);
	}

	void OnPresenterPointerReleased(object sender, PointerRoutedEventArgs args)
	{
		if (IgnorePointerId(args))
		{
			return;
		}

		if (m_isPressed)
		{
			m_isPressed = false;

			if (m_capturedPointer)
			{
				var presenter = GetPresenterOrItem();

				MUX_ASSERT(presenter);

				presenter.ReleasePointerCapture(m_capturedPointer);
			}

			UpdateVisualState(true);
		}
	}

	void OnPresenterPointerEntered(object sender, PointerRoutedEventArgs args)
	{
		ProcessPointerOver(args);
	}

	void OnPresenterPointerMoved(object sender, PointerRoutedEventArgs args)
	{
		ProcessPointerOver(args);
	}

	void OnPresenterPointerExited(object sender, PointerRoutedEventArgs args)
	{
		if (IgnorePointerId(args))
		{
			return;
		}

		m_isPointerOver = false;

		if (!m_capturedPointer)
		{
			ResetTrackedPointerId;
		}

		UpdateVisualState(true);
	}

	void OnPresenterPointerCanceled(object sender, PointerRoutedEventArgs args)
	{
		ProcessPointerCanceled(args);
	}

	void OnPresenterPointerCaptureLost(object sender, PointerRoutedEventArgs args)
	{
		ProcessPointerCanceled(args);
	}

	void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs)
	{
		if (!IsEnabled)
		{
			m_isPressed = false;
			m_isPointerOver = false;

			if (m_capturedPointer)
			{
				var presenter = GetPresenterOrItem();

				MUX_ASSERT(presenter);

				presenter.ReleasePointerCapture(m_capturedPointer);
				m_capturedPointer = null;
			}

			ResetTrackedPointerId;
		}

		UpdateVisualState(true);
	}

	void RotateExpandCollapseChevron(bool isExpanded)
	{
		var presenter = GetPresenter(); if (presenter != null)
		{
			presenter.RotateExpandCollapseChevron(isExpanded);
		}
	}

	void ProcessPointerCanceled(PointerRoutedEventArgs args)
	{
		if (IgnorePointerId(args))
		{
			return;
		}

		m_isPressed = false;
		m_isPointerOver = false;
		m_capturedPointer = null;
		ResetTrackedPointerId;
		UpdateVisualState(true);
	}

	void ProcessPointerOver(PointerRoutedEventArgs args)
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

	void HookInputEvents(IControlProtected& controlProtected)
	{
		UIElement presenter = [this, controlProtected]()


	{
			if (var presenter = GetTemplateChildT<NavigationViewItemPresenter>(c_navigationViewItemPresenterName, controlProtected))
        {
				m_navigationViewItemPresenter = presenter;
				return presenter as UIElement;
			}
			// We don't have a presenter, so we are our own presenter.
			return this as UIElement;
		} ();

		MUX_ASSERT(presenter);

		// Handlers that set flags are skipped when args.Handled is already True.
		m_presenterPointerPressedRevoker = presenter.PointerPressed(auto_revoke, { this, OnPresenterPointerPressed });
		m_presenterPointerEnteredRevoker = presenter.PointerEntered(auto_revoke, { this, OnPresenterPointerEntered });
		m_presenterPointerMovedRevoker = presenter.PointerMoved(auto_revoke, { this, OnPresenterPointerMoved });

		// Handlers that reset flags are not skipped when args.Handled is already True to avoid broken states.
		m_presenterPointerReleasedRevoker = AddRoutedEventHandler<RoutedEventType.PointerReleased>(
			presenter,


		{ this, OnPresenterPointerReleased },
        true /*handledEventsToo*/);
		m_presenterPointerExitedRevoker = AddRoutedEventHandler<RoutedEventType.PointerExited>(
			presenter,


		{ this, OnPresenterPointerExited },
        true /*handledEventsToo*/);
		m_presenterPointerCanceledRevoker = AddRoutedEventHandler<RoutedEventType.PointerCanceled>(
			presenter,


		{ this, OnPresenterPointerCanceled },
        true /*handledEventsToo*/);
		m_presenterPointerCaptureLostRevoker = AddRoutedEventHandler<RoutedEventType.PointerCaptureLost>(
			presenter,


		{ this, OnPresenterPointerCaptureLost },
        true /*handledEventsToo*/);
	}

	void UnhookInputEvents()
	{
		m_presenterPointerPressedRevoker.revoke();
		m_presenterPointerEnteredRevoker.revoke();
		m_presenterPointerMovedRevoker.revoke();
		m_presenterPointerReleasedRevoker.revoke();
		m_presenterPointerExitedRevoker.revoke();
		m_presenterPointerCanceledRevoker.revoke();
		m_presenterPointerCaptureLostRevoker.revoke();
	}

	void UnhookEventsAndClearFields()
	{
		UnhookInputEvents();

		m_flyoutClosingRevoker.revoke();
		m_splitViewIsPaneOpenChangedRevoker.revoke();
		m_splitViewDisplayModeChangedRevoker.revoke();
		m_splitViewCompactPaneLengthChangedRevoker.revoke();
		m_repeaterElementPreparedRevoker.revoke();
		m_repeaterElementClearingRevoker.revoke();
		m_isEnabledChangedRevoker.revoke();
		m_itemsSourceViewCollectionChangedRevoker.revoke();

		m_rootGrid = null;
		m_navigationViewItemPresenter = null;
		m_toolTip = null;
		m_repeater = null;
		m_flyoutContentGrid = null;
	}

}
}
