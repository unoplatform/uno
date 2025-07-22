// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewItemPresenter.cpp, commit 65718e2813

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Represents the visual elements of a NavigationViewItem.
/// </summary>
public partial class NavigationViewItemPresenter : ContentControl
{
	private const string c_contentGrid = "PresenterContentRootGrid";
	private const string c_infoBadgePresenter = "InfoBadgePresenter";
	private const string c_expandCollapseChevron = "ExpandCollapseChevron";
	private const string c_expandCollapseRotateExpandedStoryboard = "ExpandCollapseRotateExpandedStoryboard";
	private const string c_expandCollapseRotateCollapsedStoryboard = "ExpandCollapseRotateCollapsedStoryboard";

	//private const string c_iconBoxColumnDefinitionName = "IconColumn";

	public NavigationViewItemPresenter()
	{
		SetValue(TemplateSettingsProperty, new NavigationViewItemPresenterTemplateSettings());
		DefaultStyleKey = typeof(NavigationViewItemPresenter);
	}

#if HAS_UNO // In WinUI this is directly in UnhookEventsAndClearFields, but we need to call this part separately.
	private void UnhookEvents()
	{
		m_expandCollapseChevronPointerPressedRevoker.Disposable = null;
		m_expandCollapseChevronPointerReleasedRevoker.Disposable = null;
		m_expandCollapseChevronPointerExitedRevoker.Disposable = null;
		m_expandCollapseChevronPointerCanceledRevoker.Disposable = null;
		m_expandCollapseChevronPointerCaptureLostRevoker.Disposable = null;
	}
#endif

	private void UnhookEventsAndClearFields()
	{
		UnhookEvents();

		m_contentGrid = null;
		m_infoBadgePresenter = null;
		m_expandCollapseChevron = null;
		m_chevronExpandedStoryboard = null;
		m_chevronCollapsedStoryboard = null;

		m_isChevronPressed = false;
	}

	protected override void OnApplyTemplate()
	{
		//IControlProtected controlProtected = this;

		// Retrieve pointers to stable controls
		m_helper = new NavigationViewItemHelper<NavigationViewItemPresenter>(this);
		m_helper.Init(this);

		UnhookEventsAndClearFields();

		var contentGrid = GetTemplateChild(c_contentGrid) as Grid;
		if (contentGrid != null)
		{
			m_contentGrid = contentGrid;
		}

		m_infoBadgePresenter = GetTemplateChild(c_infoBadgePresenter) as ContentPresenter;

		var navigationViewItem = GetNavigationViewItem();
		if (navigationViewItem != null)
		{
#if IS_UNO
			// TODO: Uno specific: We may be reapplying the template, in which case
			// we need to unsubscribe the previous Tapped event handler.
			// Can be removed when #4689.
			if (m_expandCollapseChevron != null)
			{
				UnhookEvents();
			}
#endif
			if (navigationViewItem.HasPotentialChildren())
			{
				LoadChevron();
			}

			navigationViewItem.UpdateVisualStateNoTransition();


			// We probably switched displaymode, so restore width now, otherwise the next time we will restore is when the CompactPaneLength changes
			var navigationView = navigationViewItem.GetNavigationView();
			if (navigationView != null)
			{
				if (navigationView.PaneDisplayMode != NavigationViewPaneDisplayMode.Top)
				{
					UpdateCompactPaneLength(m_compactPaneLengthValue, true);
				}
			}
		}

		m_chevronExpandedStoryboard = (Storyboard)GetTemplateChild(c_expandCollapseRotateExpandedStoryboard);
		m_chevronCollapsedStoryboard = (Storyboard)GetTemplateChild(c_expandCollapseRotateCollapsedStoryboard);

		UpdateMargin();
	}

	internal void LoadChevron()
	{
		if (m_expandCollapseChevron is null)
		{
			if (GetNavigationViewItem() is { } navigationViewItem)
			{
				if (GetTemplateChild<Grid>(c_expandCollapseChevron) is { } expandCollapseChevron)
				{
					m_expandCollapseChevron = expandCollapseChevron;

					expandCollapseChevron.PointerPressed += OnExpandCollapseChevronPointerPressed;
					m_expandCollapseChevronPointerPressedRevoker.Disposable = Disposable.Create(() =>
					{
						expandCollapseChevron.PointerPressed -= OnExpandCollapseChevronPointerPressed;
					});

					expandCollapseChevron.PointerReleased += OnExpandCollapseChevronPointerReleased;
					m_expandCollapseChevronPointerReleasedRevoker.Disposable = Disposable.Create(() =>
					{
						expandCollapseChevron.PointerReleased -= OnExpandCollapseChevronPointerReleased;
					});

					var pointerCanceledHandler = new PointerEventHandler(OnExpandCollapseChevronPointerCanceled);
					this.AddHandler(PointerCanceledEvent, pointerCanceledHandler, handledEventsToo: true);
					m_expandCollapseChevronPointerCanceledRevoker.Disposable = Disposable.Create(() =>
					{
						this.RemoveHandler(PointerCanceledEvent, pointerCanceledHandler);
					});

					var pointerExitedHandler = new PointerEventHandler(OnExpandCollapseChevronPointerExited);
					this.AddHandler(PointerExitedEvent, pointerExitedHandler, handledEventsToo: true);
					m_expandCollapseChevronPointerExitedRevoker.Disposable = Disposable.Create(() =>
					{
						this.RemoveHandler(PointerExitedEvent, pointerExitedHandler);
					});

					var pointerCaptureLostHandler = new PointerEventHandler(OnExpandCollapseChevronPointerCaptureLost);
					this.AddHandler(PointerCaptureLostEvent, pointerCaptureLostHandler, handledEventsToo: true);
					m_expandCollapseChevronPointerCaptureLostRevoker.Disposable = Disposable.Create(() =>
					{
						this.RemoveHandler(PointerCaptureLostEvent, pointerCaptureLostHandler);
					});
				}
			}
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

	private void OnExpandCollapseChevronPointerPressed(object sender, PointerRoutedEventArgs args)
	{
		bool ignorePointerId = IgnorePointerId(args);
		var pointerProperties = args.GetCurrentPoint(this).Properties;
		if (ignorePointerId ||
			!pointerProperties.IsLeftButtonPressed ||
			args.Handled)
		{
			// We are only interested in the primary action of the pointer device 
			// (e.g. left click of a mouse)
			// Despite the name, IsLeftButtonPressed covers the primary action regardless of device.
			return;
		}

		m_isChevronPressed = true;
		args.Handled = true;
	}

	private void OnExpandCollapseChevronPointerReleased(object sender, PointerRoutedEventArgs args)
	{
		if (IgnorePointerId(args))
		{
			return;
		}

		var navigationViewItem = GetNavigationViewItem();
		var pointerProperties = args.GetCurrentPoint(this).Properties;
		if (!args.Handled &&
			m_isChevronPressed &&
			pointerProperties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased &&
			navigationViewItem is not null)
		{
			navigationViewItem.OnExpandCollapseChevronPointerReleased();
			args.Handled = true;
		}

		m_isChevronPressed = false;
		ResetTrackedPointerId();
	}

	private void OnExpandCollapseChevronPointerCanceled(object sender, PointerRoutedEventArgs args)
	{
		if (IgnorePointerId(args))
		{
			return;
		}

		m_isChevronPressed = false;
		ResetTrackedPointerId();
	}

	private void OnExpandCollapseChevronPointerExited(object sender, PointerRoutedEventArgs args)
	{
		if (IgnorePointerId(args))
		{
			return;
		}

		m_isChevronPressed = false;
		ResetTrackedPointerId();
	}

	private void OnExpandCollapseChevronPointerCaptureLost(object sender, PointerRoutedEventArgs args)
	{
		if (IgnorePointerId(args))
		{
			return;
		}

		m_isChevronPressed = false;
		ResetTrackedPointerId();
	}

	internal void RotateExpandCollapseChevron(bool isExpanded)
	{
		if (isExpanded)
		{
			var openStoryboard = m_chevronExpandedStoryboard;
			if (openStoryboard != null)
			{
				openStoryboard.Begin();
			}
		}
		else
		{
			var closedStoryboard = m_chevronCollapsedStoryboard;
			if (closedStoryboard != null)
			{
				closedStoryboard.Begin();
			}
		}
	}

	internal UIElement GetSelectionIndicator()
	{
#if IS_UNO
		// TODO: Uno specific: This is done to ensure that the presenter
		// was initialized properly - if helper is not null, but content grid
		// is null, it means the presenter was not initialized correctly.
		// Can be removed when #4809 is fixed.
		if (m_contentGrid == null && m_helper != null)
		{
			// Reinitialize
			OnApplyTemplate();
		}
#endif
		// m_helper could be null here, if template was not yet applied
		return m_helper?.GetSelectionIndicator();
	}

	protected override bool GoToElementStateCore(string state, bool useTransitions)
	{
		// GoToElementStateCore: Update visualstate for itself.
		// VisualStateManager.GoToState: update visualstate for it's first child.

		// If NavigationViewItemPresenter is used, two sets of VisualStateGroups are supported. One set is help to switch the style and it's NavigationViewItemPresenter itself and defined in NavigationViewItem
		// Another set is defined in style for NavigationViewItemPresenter.
		// OnLeftNavigation, OnTopNavigationPrimary, OnTopNavigationOverflow only apply to itself.
		if (state == NavigationViewItemHelper.c_OnLeftNavigation || state == NavigationViewItemHelper.c_OnLeftNavigationReveal || state == NavigationViewItemHelper.c_OnTopNavigationPrimary
			|| state == NavigationViewItemHelper.c_OnTopNavigationPrimaryReveal || state == NavigationViewItemHelper.c_OnTopNavigationOverflow)
		{
			if (m_infoBadgePresenter is { } infoBadgePresenter)
			{
				infoBadgePresenter.Content = null;
			}
			return base.GoToElementStateCore(state, useTransitions);
		}
		return VisualStateManager.GoToState(this, state, useTransitions);
	}

	private NavigationViewItem GetNavigationViewItem()
	{
		NavigationViewItem navigationViewItem = null;

		DependencyObject obj = this;

		var item = SharedHelpers.GetAncestorOfType<NavigationViewItem>(VisualTreeHelper.GetParent(obj));
		if (item != null)
		{
			navigationViewItem = item;
		}
		return navigationViewItem;
	}

	internal void UpdateContentLeftIndentation(double leftIndentation)
	{
		m_leftIndentation = leftIndentation;
		UpdateMargin();
	}

	private void UpdateMargin()
	{
		var grid = m_contentGrid;
		if (grid != null)
		{
			var oldGridMargin = grid.Margin;
			grid.Margin = new Thickness(m_leftIndentation, oldGridMargin.Top, oldGridMargin.Right, oldGridMargin.Bottom);
		}
	}

	internal void UpdateCompactPaneLength(double compactPaneLength, bool shouldUpdate)
	{
		m_compactPaneLengthValue = compactPaneLength;
		if (shouldUpdate)
		{
			var templateSettings = TemplateSettings;
			var gridLength = compactPaneLength;

			templateSettings.IconWidth = gridLength;
			templateSettings.SmallerIconWidth = Math.Max(0.0, gridLength - 8);
		}
	}

	internal void UpdateClosedCompactVisualState(bool isTopLevelItem, bool isClosedCompact)
	{
		// We increased the ContentPresenter margin to align it visually with the expand/collapse chevron. This updated margin is even applied when the
		// NavigationView is in a visual state where no expand/collapse chevrons are shown, leading to more content being cut off than necessary.
		// This is the case for top-level items when the NavigationView is in a compact mode and the NavigationView pane is closed. To keep the original
		// cutoff visual experience intact, we restore  the original ContentPresenter margin for such top-level items only (children shown in a flyout
		// will use the updated margin).
		var stateName = isClosedCompact && isTopLevelItem
		   ? "ClosedCompactAndTopLevelItem"
		   : "NotClosedCompactAndTopLevelItem";

		VisualStateManager.GoToState(this, stateName, false /*useTransitions*/);
	}
}
