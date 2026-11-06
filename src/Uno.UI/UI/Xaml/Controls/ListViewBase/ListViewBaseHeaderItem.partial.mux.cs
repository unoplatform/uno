// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseHeaderItem_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBaseHeaderItem
{
	internal void SetParent(ListViewBase? pParent) => m_wrParentListViewBase = pParent is null ? null : new WeakReference<ListViewBase>(pParent);

	internal ListViewBase? GetParent()
	{
		ListViewBase? parent = null;
		m_wrParentListViewBase?.TryGetTarget(out parent);
		return parent;
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		ChangeVisualState(false);
	}

	protected override void OnGotFocus(RoutedEventArgs pArgs)
	{
		base.OnGotFocus(pArgs);
		ChangeVisualState(false);

		GetParent()?.GroupHeaderItemFocused(this);
	}

	protected override void OnLostFocus(RoutedEventArgs pArgs)
	{
		base.OnLostFocus(pArgs);
		ChangeVisualState(true);

		GetParent()?.GroupHeaderItemUnfocused(this);
	}

	protected override void OnKeyDown(KeyRoutedEventArgs pArgs)
	{
		base.OnKeyDown(pArgs);

		var spParent = GetParent();

		if (spParent is not null)
		{
			bool isHandled = false;

			VirtualKey originalKey = pArgs.OriginalKey;
			VirtualKey key = pArgs.Key;

			// for gamepad, this action happens on key up. Toggling sezo on KeyDown will cause a key up
			// in the other view and toggle it back.
			if (originalKey != VirtualKey.GamepadA)
			{
				isHandled = spParent.OnGroupHeaderKeyDown(this, originalKey, key);
			}

			// Inform ListView that the keydown came from an Item. If it came from a header, or blank area
			// instead of an item ListView will just forward the event back to the ScrollView.
			if (!isHandled)
			{
				spParent.SetHandleKeyDownArgsFromItem(true /* fFromItem */);
			}

			if (isHandled)
			{
				pArgs.Handled = true;
			}
		}
	}

	// Handle Key Up for gamepad and toggle
	protected override void OnKeyUp(KeyRoutedEventArgs pArgs)
	{
		base.OnKeyUp(pArgs);

		var spParent = GetParent();

		if (spParent is not null)
		{
			bool isHandled = false;

			VirtualKey originalKey = pArgs.OriginalKey;
			VirtualKey key = pArgs.Key;

			if (originalKey == VirtualKey.GamepadA)
			{
				isHandled = spParent.OnGroupHeaderKeyUp(this, originalKey, key);
			}

			if (isHandled)
			{
				pArgs.Handled = true;
			}
		}
	}

	// Called when the user presses a pointer down over the ListViewBaseHeaderItem.
	protected override void OnPointerPressed(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerPressed(pArgs);

		if (!m_IsPressed)
		{
			m_IsPressed = true;

			ChangeVisualState(true /* useTransitions */);
		}
	}

	// Called when the user releases a pointer over the ListViewBaseHeaderItem.
	protected override void OnPointerReleased(PointerRoutedEventArgs pArgs)
	{
		// WinUI quirk: the base OnPointerPressed is invoked here, not OnPointerReleased.
		base.OnPointerPressed(pArgs);

		m_IsPressed = false;

		ChangeVisualState(true /* useTransitions */);

		// if we are in a semantic zoom, handle the pointer released key
		// so that the ScrollViewer above does not handle it and move focus
		// to itself.
		var spParent = GetParent();
		if (spParent is not null)
		{
			var semanticZoomOwner = spParent.SemanticZoomOwner;
			if (semanticZoomOwner is not null)
			{
				pArgs.Handled = true;
			}
		}
	}

	// Called when the ListViewBaseHeaderItem or its children lose pointer capture.
	protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerCaptureLost(pArgs);

		m_IsPressed = false;
		m_IsPointerOver = false;

		ChangeVisualState(true /* useTransitions */);
	}

	// Called when a pointer enters a ListViewBaseHeaderItem.
	protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerEntered(pArgs);

		m_IsPointerOver = true;

		ChangeVisualState(true /* useTransitions */);
	}

	// Called when a pointer exits a ListViewBaseHeaderItem.
	protected override void OnPointerExited(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerExited(pArgs);

		m_IsPressed = false;
		m_IsPointerOver = false;

		ChangeVisualState(true /* useTransitions */);
	}

	private protected override void ChangeVisualState(bool useTransitions)
	{
		bool isEnabled = IsEnabled;
		FocusState focusState = FocusState;

		// Common state group
		if (!isEnabled)
		{
			GoToState(useTransitions, "Disabled");
		}
		else if (m_IsPressed)
		{
			GoToState(useTransitions, "Pressed");
		}
		else if (m_IsPointerOver)
		{
			GoToState(useTransitions, "PointerOver");
		}
		else
		{
			GoToState(useTransitions, "Normal");
		}

		// Focus state group
		if (focusState == FocusState.Keyboard ||
			focusState == FocusState.Programmatic)
		{
			GoToState(useTransitions, "Focused");
		}
		else if (focusState == FocusState.Pointer)
		{
			GoToState(useTransitions, "PointerFocused");
		}
		else
		{
			GoToState(useTransitions, "Unfocused");
		}
	}

	// determines if this element should be transitioned using the passed in transition
	internal ThemeTransitionContext GetCurrentTransitionContext(int layoutTickId)
	{
		var spParentAsDO = VisualTreeHelper.GetParent(this);
		if (spParentAsDO is IChildTransitionContextProvider spContextProviderPanel)
		{
			return spContextProviderPanel.GetChildTransitionContext(this, layoutTickId);
		}

		return ThemeTransitionContext.None;
	}

	// determines if mutations are going fast
	internal bool IsCollectionMutatingFast()
	{
		var spParentAsDO = VisualTreeHelper.GetParent(this);
		if (spParentAsDO is IChildTransitionContextProvider spContextProviderPanel)
		{
			return spContextProviderPanel.IsCollectionMutatingFast();
		}

		return false;
	}

	// TODO Uno: WinUI returns E_NOTIMPL after zeroing the point.
	internal Point GetDropOffsetToRoot() => default;

	protected override void OnTapped(TappedRoutedEventArgs pArgs)
	{
		base.OnTapped(pArgs);

		if (!pArgs.Handled)
		{
			var spParent = GetParent();
			if (spParent is not null)
			{
				bool isHandled = spParent.OnHeaderItemTap(this);
				pArgs.Handled = isHandled;
			}
		}
	}

	protected override Size ArrangeOverride(Size arrangeSize)
	{
		var returnValue = base.ArrangeOverride(arrangeSize);
		m_invalidateGroupBoundsCache = true;
		return returnValue;
	}

	internal Rect GetGroupBounds()
	{
		if (m_invalidateGroupBoundsCache)
		{
			int indexHeader = 0;
			int indexCacheStart = 0;
			int indexCacheEnd = 0;
			int indexStartItem = 0;
			int countItemInGroup = 0;

			m_rectGroupBoundsCache = GetGlobalBounds(false);

			var spParentListViewBase = GetParent();

			// If the header is recycled, the parent ListViewBase is set
			// to null. We could potentially end up in this method when
			// the header is recycled since focus events are async (bug 676039)
			if (spParentListViewBase is not null)
			{
				var spItemsHostPanelModernCollection = (ModernCollectionBasePanel)spParentListViewBase.ItemsPanelRoot;
				indexHeader = spItemsHostPanelModernCollection.IndexFromHeader(this, false /*excludeHiddenEmptyGroups*/);
				spItemsHostPanelModernCollection.GetGroupInformationFromGroupIndex(indexHeader, out indexStartItem, out countItemInGroup);
				indexCacheEnd = spItemsHostPanelModernCollection.LastCacheIndexBase;

				int indexRealizedStart = (indexCacheStart > indexStartItem) ? indexCacheStart : indexStartItem;
				int indexRealizedEnd = (indexCacheEnd < (indexStartItem + countItemInGroup - 1)) ? indexCacheEnd : (indexStartItem + countItemInGroup - 1);
				for (int indexContainer = indexRealizedStart; indexContainer <= indexRealizedEnd; ++indexContainer)
				{
					var spItemContainerAsDO = spItemsHostPanelModernCollection.ContainerFromIndex(indexContainer);
					if (spItemContainerAsDO is UIElement spItemContainer)
					{
						Rect elementBounds = spItemContainer.GetGlobalBounds(false);
						m_rectGroupBoundsCache.Union(elementBounds);
					}
				}
			}
		}

		return m_rectGroupBoundsCache;
	}

	internal override string GetPlainText() => base.GetPlainText();

	// TODO Uno: AutomationPeer.GetLogicalParentForAP does not consult this hook yet.
	internal DependencyObject? GetLogicalParentForAPProtected() => GetParent();

	// Sticky Headers
	// Computes the offsets resulting from Sticky Headers
	// We delegate the computations to the panel
	internal void CoerceStickyHeaderOffsets(int cOffsets, double[] pOffsets)
	{
		var pVirtualizationInformation = GetVirtualizationInformationOrNull();

		if (pVirtualizationInformation is not null && pVirtualizationInformation.IsSticky)
		{
			var spParentAsDO = VisualTreeHelper.GetParent(this);
			if (spParentAsDO is ModernCollectionBasePanel spParentPanel)
			{
				spParentPanel.CoerceStickyHeaderOffsets(this, cOffsets, pOffsets);
			}
		}
	}

	// Callback when a LayoutTransition storyboard is created for this element
	// Gives us a chance to call back into the panel and allow it to take action, e.g., temporarily hide Sticky Headers
	void ILayoutTransitionStoryboardNotification.NotifyLayoutTransitionStart()
	{
		if (GetVirtualizationInformationOrNull() is { IsSticky: true })
		{
			var spParentAsDO = VisualTreeHelper.GetParent(this);
			if (spParentAsDO is ModernCollectionBasePanel spParentPanel)
			{
				spParentPanel.NotifyLayoutTransitionStart(this);
			}
		}
	}

	void ILayoutTransitionStoryboardNotification.NotifyLayoutTransitionEnd()
	{
		if (GetVirtualizationInformationOrNull() is { IsSticky: true })
		{
			var spParentAsDO = VisualTreeHelper.GetParent(this);
			if (spParentAsDO is ModernCollectionBasePanel spParentPanel)
			{
				spParentPanel.NotifyLayoutTransitionEnd(this);
			}
		}
	}

	// Called when the element leaves the tree. Clears the sticky header wrapper.
	internal override void LeaveImpl(Uno.UI.Xaml.LeaveParams @params)
	{
		if (GetVirtualizationInformationOrNull() is { } virtualizationInformation)
		{
			virtualizationInformation.m_stickyHeaderWrapper = null;
		}

		base.LeaveImpl(@params);
	}
}
