// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewItem.cpp file from WinUI controls.
//

using System;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationViewItem : NavigationViewItemBase
	{
		const string c_navigationViewItemPresenterName = "NavigationViewItemPresenter";

		private long m_splitViewIsPaneOpenChangedRevoker;
		private long m_splitViewDisplayModeChangedRevoker;
		private long m_splitViewCompactPaneLengthChangedRevoker;

		private SerialDisposable _splitViewSubscriptions = new SerialDisposable();

		// tracker_ref<ToolTip> m_toolTip { this };
		NavigationViewItemHelper<NavigationViewItem> m_helper = new NavigationViewItemHelper<NavigationViewItem>();
		NavigationViewItemPresenter m_navigationViewItemPresenter;
		object m_suggestedToolTipContent;

		bool m_isClosedCompact = false;

		bool m_appliedTemplate = false;
		bool m_hasKeyboardFocus = false;
		bool m_isContentChangeHandlingDelayedForTopNav = false;

		internal void UpdateVisualStateNoTransition()
		{
			UpdateVisualState(false /*useTransition*/);
		}

		internal bool IsContentChangeHandlingDelayedForTopNav() { return m_isContentChangeHandlingDelayedForTopNav; }
		internal void ClearIsContentChangeHandlingDelayedForTopNavFlag() { m_isContentChangeHandlingDelayedForTopNav = false; }

		protected override void OnNavigationViewListPositionChanged()
		{
			UpdateVisualStateNoTransition();
		}

		public NavigationViewItem()
		{
			DefaultStyleKey = GetType();

			Loaded += NavigationViewItem_Loaded;
		}

		private void NavigationViewItem_Loaded(object sender, RoutedEventArgs e)
		{
			var splitView = GetSplitView();
			if (splitView != null)
			{
				_splitViewSubscriptions.Disposable = null;

				var disposable = new CompositeDisposable();
				_splitViewSubscriptions.Disposable = disposable;

				m_splitViewIsPaneOpenChangedRevoker = splitView.RegisterPropertyChangedCallback(SplitView.IsPaneOpenProperty, OnSplitViewPropertyChanged);
				disposable.Add(() => splitView.UnregisterPropertyChangedCallback(SplitView.IsPaneOpenProperty, m_splitViewIsPaneOpenChangedRevoker));

				m_splitViewDisplayModeChangedRevoker = splitView.RegisterPropertyChangedCallback(SplitView.DisplayModeProperty, OnSplitViewPropertyChanged);
				disposable.Add(() => splitView.UnregisterPropertyChangedCallback(SplitView.DisplayModeProperty, m_splitViewDisplayModeChangedRevoker));

				m_splitViewCompactPaneLengthChangedRevoker = splitView.RegisterPropertyChangedCallback(SplitView.CompactPaneLengthProperty, OnSplitViewPropertyChanged);
				disposable.Add(() => splitView.UnregisterPropertyChangedCallback(SplitView.CompactPaneLengthProperty, m_splitViewCompactPaneLengthChangedRevoker));

				UpdateCompactPaneLength();
				UpdateIsClosedCompact();
			}
		}

		~NavigationViewItem()
		{
		}

		protected override void OnApplyTemplate()
		{
			// Stop UpdateVisualState before template is applied. Otherwise the visual may not the same as we expect
			m_appliedTemplate = false;
 
			base.OnApplyTemplate();

			// Find selection indicator
			// Retrieve pointers to stable controls 
			m_helper.Init(this);
			m_navigationViewItemPresenter = GetTemplateChild(c_navigationViewItemPresenterName) as NavigationViewItemPresenter;

			// m_toolTip = GetTemplateChildT<ToolTip>("ToolTip"sv, controlProtected));

			m_appliedTemplate = true;
			UpdateVisualStateNoTransition();

			var visual = ElementCompositionPreview.GetElementVisual(this);
			NavigationView.CreateAndAttachHeaderAnimation(visual);
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
			}
		}

		void UpdateCompactPaneLength()
		{
			var splitView = GetSplitView();
			if (splitView != null)
			{
				SetValue(CompactPaneLengthProperty, splitView.CompactPaneLength);
			}
		}

		void UpdateIsClosedCompact()
		{
			var splitView = GetSplitView();
			if (splitView != null)
			{
				// Check if the pane is closed and if the splitview is in either compact mode.
				m_isClosedCompact = !splitView.IsPaneOpen && (splitView.DisplayMode == SplitViewDisplayMode.CompactOverlay || splitView.DisplayMode == SplitViewDisplayMode.CompactInline);
				UpdateVisualState(true /*useTransitions*/);
			}
		}

		void UpdateNavigationViewItemToolTip()
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

		void SuggestedToolTipChanged(object newContent)
		{
			var potentialString = newContent;
			bool stringableToolTip = potentialString is string;

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

		void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			var property = args.Property;
			if (property == IconProperty)
			{
				UpdateVisualStateNoTransition();
			}
		}

		void UpdateVisualStateForIconAndContent(bool showIcon, bool showContent)
		{
			var stateName = showIcon ? (showContent ? "IconOnLeft": "IconOnly") : "ContentOnly";
			VisualStateManager.GoToState(this, stateName, false /*useTransitions*/);
		}

		void UpdateVisualStateForNavigationViewListPositionChange()
		{
			var position = Position();
			var stateName = NavigationViewItemHelper.c_OnLeftNavigation;

			bool handled = false;
			if (position == NavigationViewListPosition.LeftNav)
			{
#if !IS_UNO
				if (SharedHelpers.IsRS4OrHigher() && Application.Current.FocusVisualKind == FocusVisualKind.Reveal)
				{
					// OnLeftNavigationReveal is introduced in RS6. 
					// Will fallback to stateName for the customer who re-template rs5 NavigationViewItem
					if (VisualStateManager.GoToState(this, NavigationViewItemHelper.c_OnLeftNavigationReveal, false /*useTransitions*/))
					{
						handled = true;
					}
				}
#endif
			}
			else if (position == NavigationViewListPosition.TopPrimary)
			{
#if !IS_UNO
				if (SharedHelpers.IsRS4OrHigher() && Application.Current.FocusVisualKind == FocusVisualKind.Reveal)
				{
					stateName = NavigationViewItemHelper.c_OnTopNavigationPrimaryReveal;
				}
				else
#endif
				{
					stateName = NavigationViewItemHelper.c_OnTopNavigationPrimary;
				}
			}
			else if (position == NavigationViewListPosition.TopOverflow)
			{
				stateName = NavigationViewItemHelper.c_OnTopNavigationOverflow;
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
#if !IS_UNO
			// Since RS5, ToolTip apply to NavigationViewItem directly to make Keyboard focus has tooltip too.
			// If ToolTip TemplatePart is detected, fallback to old logic and apply ToolTip on TemplatePart.
			var toolTip = m_toolTip;
			if (toolTip != null)
			{
				var shouldEnableToolTip = ShouldEnableToolTip();
				var toolTipContent = m_suggestedToolTipContent;
				if (shouldEnableToolTip && toolTipContent)
				{
					toolTip.Content(toolTipContent);
					toolTip.IsEnabled(true);
				}
				else
				{
					toolTip.Content(null);
					toolTip.IsEnabled(false);
				}
			}
			else
			{
				UpdateNavigationViewItemToolTip();
			}
#endif
		}

		void UpdateVisualState(bool useTransitions)
		{
			if (!m_appliedTemplate)
				return;

			UpdateVisualStateForNavigationViewListPositionChange();

			bool shouldShowIcon = ShouldShowIcon();
			bool shouldShowContent = ShouldShowContent();
  
			if (IsOnLeftNav())
			{
				VisualStateManager.GoToState(this, m_isClosedCompact ? "ClosedCompact" : "NotClosedCompact", useTransitions); 

				// Backward Compatibility with RS4-, new implementation prefer IconOnLeft/IconOnly/ContentOnly
				VisualStateManager.GoToState(this, shouldShowIcon ? "IconVisible" : "IconCollapsed", useTransitions);
			} 
   
			UpdateVisualStateForToolTip();

			UpdateVisualStateForIconAndContent(shouldShowIcon, shouldShowContent);

			// visual state for focus state. top navigation use it to provide different visual for selected and selected+focused
			UpdateVisualStateForKeyboardFocusedState();
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

		bool IsOnLeftNav()
		{
			return Position() == NavigationViewListPosition.LeftNav;
		}

		bool IsOnTopPrimary()
		{
			return Position() == NavigationViewListPosition.TopPrimary;
		}

		NavigationViewItemPresenter GetPresenter()
		{
			NavigationViewItemPresenter presenter = null;
			if (m_navigationViewItemPresenter != null)
			{
				presenter = m_navigationViewItemPresenter;
			}
			return presenter;
		}

		// IUIElement / IUIElementOverridesHelper
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

			// Two ways are used to notify the content change on TopNav and asking for a layout update:
			//  1. The NavigationViewItem can't find its parent NavigationView, just mark it. Possibly NavigationViewItem is moved to overflow but menu is not opened.
			//  2. NavigationViewItem request update by NavigationView.TopNavigationViewItemContentChanged.
			if (!IsOnLeftNav())
			{
				var navView = GetNavigationView();
				if (navView != null)
				{
					navView.TopNavigationViewItemContentChanged();
				} 
				else
				{
					m_isContentChangeHandlingDelayedForTopNav = true;
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
				// For example, we can move the SelectionIndicator 3px up when focused and selected to make sure focus rectangle doesn't override SelectionIndicator. 
				// If it's a pointer or programmatic, no focus rectangle, so no action
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
	}
}
