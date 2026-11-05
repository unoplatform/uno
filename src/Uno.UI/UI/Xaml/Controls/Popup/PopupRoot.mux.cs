// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Popup.cpp (CPopupRoot) and components/elements/PopupRoot.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.System;
using static Microsoft.UI.Xaml.Controls._Tracing;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class PopupRoot
{
	internal enum PopupFilter
	{
		LightDismissOnly,
		LightDismissOrFlyout,
		All,
	}

	// A linked list of open popups. The most recently opened is at the head.
	private readonly LinkedList<Popup> m_pOpenPopups = new();
	private List<Popup>? m_pDeferredPopups;
	private readonly List<Popup> m_popupsToDeferClose = new();

	// Has theme ever changed from startup theme?
	private bool m_hasThemeChanged;
	private bool m_shouldDeferClosePopups;
	private bool m_isRootHitTestingSuppressed;
	private bool m_isOnMeasureStack;

	// TODO Uno: Stands in for the TransitionRoot, which hosts the LTEs below the popups. Layout transition elements aren't implemented.
	private readonly List<UIElement> m_transitionElements = new();

	internal bool IsOnMeasureStack => m_isOnMeasureStack;

	internal bool ShouldDeferClosePopups => m_shouldDeferClosePopups;

	internal bool GetIsRootHitTestingSuppressed() => m_isRootHitTestingSuppressed;

	internal void SetIsRootHitTestingSuppressed(bool isRootHitTestingSuppressed)
	{
		m_isRootHitTestingSuppressed = isRootHitTestingSuppressed;
		UpdateHitTest();
	}

	/// <summary>
	/// Disposes all of the open popups
	/// </summary>
	internal void CloseAllPopupsForTreeReset()
	{
		// DCompTreeHost has a noref list of elements that are being kept visible, and it will iterate over that
		// list of elements. Children of unloading popups can be kept in this list, and we're about to release
		// them all. Remove the unloading children from this list so that the DCompTreeHost doesn't try to iterate
		// over deleted elements on the next tick.
		// TODO Uno: RemoveAllUnloadingChildren: no unloading storage.

		foreach (var popup in GetPopupsInSafeClosingOrder())
		{
			popup.ForceCloseForTreeReset();
		}

		//assert that open popup list is now empty
		MUX_ASSERT(m_pOpenPopups.First is null);
		m_pOpenPopups.Clear();

		m_pDeferredPopups = null;
	}

	internal void OnIslandLostFocus()
	{
		foreach (var popup in GetPopupsInSafeClosingOrder())
		{
			popup.OnIslandLostFocusImpl();
		}
	}

	/// <summary>
	/// implementation for MeasureOverride virtual. PopupRoot only measures the open popups.
	/// </summary>
	protected override Size MeasureOverride(Size availableSize)
	{
		var childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);

		foreach (var transitionElement in m_transitionElements)
		{
			MeasureElement(transitionElement, childConstraint);
		}

		m_isOnMeasureStack = true;
		try
		{
			for (var pNode = m_pOpenPopups.First; pNode is not null; pNode = pNode.Next)
			{
				var pPopup = pNode.Value;
				if (!pPopup.IsUnloading)
				{
					if (pPopup.OverlayElement is { } overlayElement)
					{
						MeasureElement(overlayElement, childConstraint);
					}

					MeasureElement(pPopup.Child, childConstraint);
				}
			}
		}
		finally
		{
			m_isOnMeasureStack = false;
		}

		// Open and measure the popups in the deferred list. These popups were open during
		// template expansion while measuring popups that are already open.
		if (m_pDeferredPopups is { } deferredPopups)
		{
			m_pDeferredPopups = null;

			foreach (var pPopup in deferredPopups)
			{
				if (pPopup.Child is not null && pPopup.IsOpenInternal)
				{
					pPopup.Open();

					if (pPopup.OverlayElement is { } overlayElement)
					{
						MeasureElement(overlayElement, childConstraint);
					}

					MeasureElement(pPopup.Child, childConstraint);
				}
			}
		}

		return default;
	}

	/// <summary>
	/// implementation for ArrangeOverride virtual. PopupRoot only arranges the open popups.
	/// </summary>
	protected override Size ArrangeOverride(Size finalSize)
	{
		foreach (var transitionElement in m_transitionElements)
		{
			ArrangeElement(transitionElement, new Rect(0, 0, finalSize.Width, finalSize.Height));
		}

		for (var pNode = m_pOpenPopups.First; pNode is not null; pNode = pNode.Next)
		{
			var pPopup = pNode.Value;
			if (!pPopup.IsUnloading)
			{
				var pChild = pPopup.Child;

				var x = pPopup.HorizontalOffset + pPopup.m_hAdjustment + pPopup.m_calculatedHOffset;
				var y = pPopup.VerticalOffset + pPopup.m_vAdjustment + pPopup.m_calculatedVOffset;

				pChild.EnsureLayoutStorage();

				if (pPopup.OverlayElement is { } overlayElement)
				{
					// TODO Uno: GetContentBoundsForElement. The popup root covers the content of its XamlRoot.
					ArrangeElement(overlayElement, new Rect(0, 0, finalSize.Width, finalSize.Height));
				}

				//Arrange at the HorizontalOffset and VerticalOffset specified on the Popup.
				var childSize = pChild.DesiredSize;
				ArrangeElement(pChild, new Rect(x, y, childSize.Width, childSize.Height));

				// For windowed popups, the LTE for the Load transition caused by the Arrange() will be clipped to
				// the Jupiter window, although the popup's window's content is not clipped. This causes an ugly flicker
				// where the clipped LTE is drawn followed by the unclipped popup window's content. Fix this by canceling
				// the Unload transition.
				if (pPopup.IsWindowed)
				{
					// TODO Uno: no layout transitions to cancel, nor a system backdrop to update.

					// The render walk moves the window in WinUI, when the comp node's properties are pushed (HWCompNodeWinRT.cpp:706).
					// CompositionTarget.WindowedPopupSurface does that too, but the window's bounds are known from here.
					pPopup.Reposition();
				}
			}
		}

		return finalSize;
	}

	/// <summary>
	/// Initial phase: the provided popup simply gets added to the list of open popups.
	/// </summary>
	internal void StartAdditionToOpenPopupList(Popup pPopup)
	{
		MUX_ASSERT(pPopup.Child is not null && pPopup.IsOpenInternal);

		// The popup must not be unloading. If it is, the caller must finish unloading it first.
		MUX_ASSERT(!ContainsOpenOrUnloadingPopup(pPopup));

		m_pOpenPopups.AddFirst(pPopup);
	}

	/// <summary>
	/// Undo the addition done by StartAdditionToOpenPopupList above.
	/// </summary>
	internal void UndoAdditionToOpenPopupList(Popup pPopup)
	{
		if (ContainsOpenOrUnloadingPopup(pPopup))
		{
			m_pOpenPopups.Remove(pPopup);
		}
	}

	/// <summary>
	/// Final phase: peg managed peer, update themes and rendering trees.
	/// </summary>
	internal void CompleteAdditionToOpenPopupList(Popup pPopup)
	{
		MUX_ASSERT(pPopup.Child is not null && pPopup.IsOpenInternal);
		MUX_ASSERT(ContainsOpenOrUnloadingPopup(pPopup));

		// Open popup should not be GC'd. Hold a ref on the managed peer.
		// Uno: m_pOpenPopups holds a strong reference.

		// If app's theme has changed, notify popup
		// A popup root created after an app theme switch (secondary windows, islands) never saw that walk, so the
		// core-level flag, set by the same CCoreServices::NotifyThemeChange that notifies the main popup root, backs it up.
		if ((m_hasThemeChanged || WinUICoreServices.Instance.HasThemeEverChanged) && pPopup.ShouldPopupRootNotifyThemeChange())
		{
			pPopup.NotifyThemeChanged(WinUICoreServices.Instance.Theming.GetTheme());
		}

		//
		// Child Added - Dirties (Render | Bounds)
		//
		// TODO Uno: UpdateHas3DDepthInSubtree: no 3D hit testing.
	}

	internal bool ContainsOpenOrUnloadingPopup(Popup pPopup) => m_pOpenPopups.Contains(pPopup);

	internal void ClearLastPointerPointForReplay()
	{
		foreach (var openPopup in GetOpenPopups())
		{
			if (openPopup.IsWindowed)
			{
				openPopup.ClearLastPointerPointForReplay();
			}
		}
	}

	internal bool ReplayPointerUpdate()
	{
		var popupsThatReplayedPointerUpdate = 0;

		// Snapshot the open popups before iterating.  Inside CPopup::ReplayPointerUpdate
		// (-> WindowedPopupInputSiteAdapter::ReplayPointerUpdate) we synthesize a real
		// pointer event and run it through the input pipeline, which executes app-side
		// handlers that can synchronously close *other* popups (light-dismiss, cascading
		// menus, focus-loss handlers, etc.).
		foreach (var popup in GetOpenPopups())
		{
			if (popup.IsOpenInternal && !popup.IsUnloading)
			{
				if (popup.ReplayPointerUpdate())
				{
					popupsThatReplayedPointerUpdate++;
				}
			}
		}

		MUX_ASSERT(popupsThatReplayedPointerUpdate <= 1);

		return popupsThatReplayedPointerUpdate != 0;
	}

	/// <summary>
	/// Removes popup from the open popup list and releases the ref on it.
	/// </summary>
	internal void RemoveFromOpenPopupList(Popup pPopup)
	{
		// The releases that we do in this method could be the final release on the popup, which deletes it. CPopup's dtor
		// calls RemoveChild, which can call CPopup::Close, which calls back here, which can cause us to release the already
		// deleted popup again. Make sure that the popup is actually in the list before releasing it to prevent this reentrancy.
		if (ContainsOpenOrUnloadingPopup(pPopup))
		{
			m_pOpenPopups.Remove(pPopup);

			//
			// Child removed - Dirties (Render | Bounds)
			//
			// TODO Uno: UpdateHas3DDepthInSubtree: no 3D hit testing.
		}
	}

	/// <summary>
	/// Add popup to the deferred list. This is for popups opened during template expansion.
	/// These will be opened by popup root after it is done with layout for currently open popups.
	/// </summary>
	internal void AddToDeferredOpenPopupList(Popup pPopup)
	{
		m_pDeferredPopups ??= new();
		m_pDeferredPopups.Add(pPopup);
	}

	internal void AddTransitionElement(UIElement element)
	{
		m_transitionElements.Add(element);
		Children.Insert(0, element);
	}

	internal void RemoveTransitionElement(UIElement element)
	{
		m_transitionElements.Remove(element);
		Children.Remove(element);
	}

	/// <summary>
	/// Whether the hit test walk goes into this child of the popup root.
	/// </summary>
	/// <remarks>
	/// Maps CPopupRoot::BoundsTestChildrenImpl onto Uno's walk over visual children. The walk tests the children top to
	/// bottom, which is the order WinUI goes through the open popups (most recently opened first). The overlay isn't one
	/// of the popups: WinUI never tests it.
	/// </remarks>
	internal bool ShouldHitTestChild(UIElement child)
	{
		if (m_transitionElements.Contains(child))
		{
			return true;
		}

		var popup = GetOpenPopupWithChild(child, checkUnloadingChildToo: false);

		return popup is { IsUnloading: false }
			&& popup.IsVisible()
			&& popup.AreAllAncestorsVisible();
	}

	/// <summary>
	/// Whether the popup root takes the hit when the walk gets past this child: the light dismiss layer of its popup is
	/// under it, and over everything opened earlier.
	/// </summary>
	internal bool IsLightDismissLayerBelowChild(UIElement child)
	{
		// Check for light dismiss.
		// If a drag and drop operation is in progress, we allow it to hit test through the light dismiss layer.
		return GetOpenPopupWithChild(child, checkUnloadingChildToo: false) is { IsUnloading: false, IsLightDismiss: true }
			&& !DXamlCore.IsWinRTDndOperationInProgress();
	}

	/// <summary>
	/// Gets all open popups, rooted and unrooted.
	/// </summary>
	internal IReadOnlyList<Popup> GetOpenPopups() => GetOpenPopupList(includeUnloadingPopups: false);

	internal List<Popup> GetOpenPopupList(bool includeUnloadingPopups)
	{
		List<Popup> openPopups = new(m_pOpenPopups.Count);

		for (var pNode = m_pOpenPopups.First; pNode is not null; pNode = pNode.Next)
		{
			if (includeUnloadingPopups || !pNode.Value.IsUnloading)
			{
				openPopups.Add(pNode.Value);
			}
		}

		return openPopups;
	}

	// CPopupRoot::HitTestLocalInternal
	internal override bool IsViewHit()
	{
		if (!m_isRootHitTestingSuppressed)
		{
			// Hit testing doesn't run on unloading popups.
			for (var pNode = m_pOpenPopups.First; pNode is not null; pNode = pNode.Next)
			{
				var pPopup = pNode.Value;
				if (!pPopup.IsUnloading && pPopup.IsLightDismiss)
				{
					return true;
				}
			}
		}

		return false;
	}

	internal override bool HitTest(Point relativeLocation) => IsViewHit();

	/// <summary>
	/// Leaves the CPopupRoot and all its popup children.
	/// </summary>
	internal override void LeaveImpl(LeaveParams @params)
	{
		// scope
		{
			// Set the bool indicating that we in the base LeaveImpl, and restore the bool on exit.
			MUX_ASSERT(!m_shouldDeferClosePopups);
			var restoreShouldDeferClosePopups = m_shouldDeferClosePopups;
			m_shouldDeferClosePopups = true;
			try
			{
				base.LeaveImpl(@params);
			}
			finally
			{
				m_shouldDeferClosePopups = restoreShouldDeferClosePopups;
			}
		}

		// Close any popups queued to be defer closed.
		foreach (var popup in m_popupsToDeferClose)
		{
			if (popup.IsOpenInternal)
			{
				popup.IsOpen = false;
			}
		}
		m_popupsToDeferClose.Clear();

		// TODO Uno: LeavePCSceneSubgraph: Uno's render walk follows the visual tree.
	}

	internal void DeferClosePopup(Popup popup)
	{
		if (!m_shouldDeferClosePopups)
		{
			throw new InvalidOperationException("Popups can only be defer closed while the popup root is leaving the tree.");
		}

		m_popupsToDeferClose.Add(popup);
	}

	// TODO Uno: LeavePCSceneSubgraph, CleanupDeviceRelatedResourcesRecursive, CleanupAndLeaveSubgraphHelper,
	// PrintChildren, ClearPrintDirtyFlagOnOpenPopups, GetTransitionsForChildElementNoAddRef, ComputeDepthInOpenPopups: no equivalents.
	// GenerateChildOuterBounds: Uno computes hit-test bounds during the walk.
	// EffectiveViewportWalkToChild: FrameworkElement.GetEffectiveViewport, on Visual.HasOwnSurface.
	// HitTestPopups: VisualTreeHelper.FindElementsInHostCoordinates walks the root.

	internal bool CloseTopmostPopup(FocusState focusStateAfterClosing, PopupFilter filter)
	{
		var didCloseAPopup = false;

		var popupNoRef = GetTopmostPopup(filter);
		if (popupNoRef is not null)
		{
			// Give the popup an opportunity to cancel closing.
			var cancel = false;
			popupNoRef.OnClosing(ref cancel);
			if (cancel)
			{
				return false;
			}

			popupNoRef.m_focusStateAfterClosing = focusStateAfterClosing;
			popupNoRef.IsOpen = false;
			didCloseAPopup = true;
		}

		return didCloseAPopup;
	}

	// Handler for PointerPressed on the CPopupRoot, for light dismiss Popups.
	private void OnPointerPressed(object sender, PointerRoutedEventArgs pEventArgs)
	{
		// Make sure we are the original source.  We do not want to handle PointerPressed on the Popup itself.
		if (pEventArgs.OriginalSource == this)
		{
			var didCloseAPopup = CloseTopmostPopup(FocusState.Pointer, PopupFilter.LightDismissOnly);
			pEventArgs.Handled = didCloseAPopup;
		}
	}

	// The ESC key closes the topmost light-dismiss-enabled popup.
	// Handling must be done by CPopupRoot because the popups reparent their children to be under CPopupRoot,
	// so routed events from beneanth the popups route to CPopupRoot and skip the popups themselves.
	private void OnKeyDown(object sender, KeyRoutedEventArgs pEventArgs)
	{
		if (pEventArgs.Key == VirtualKey.Escape)
		{
			var didCloseAPopup = CloseTopmostPopup(FocusState.Keyboard, PopupFilter.LightDismissOrFlyout);
			pEventArgs.Handled = didCloseAPopup;
		}
	}

	internal Popup? GetTopmostPopup(PopupFilter filter)
	{
		// Find the most recently opened Popup that has light dismiss enabled.
		// Unloading popups don't count.
		for (var pNode = m_pOpenPopups.First; pNode is not null; pNode = pNode.Next)
		{
			var pPopup = pNode.Value;
			if (!pPopup.IsUnloading)
			{
				if (filter == PopupFilter.All || (filter == PopupFilter.LightDismissOrFlyout && pPopup.IsFlyout) || pPopup.IsLightDismiss)
				{
					return pPopup;
				}
			}
		}

		return null;
	}

	internal bool IsTopmostPopupInLightDismissChain()
	{
		// Find the most recently opened Popup and check that has light dismiss enabled,
		// or which has a parent that has light dismiss enabled.
		// Unloading popups don't count.
		for (var pNode = m_pOpenPopups.First; pNode is not null; pNode = pNode.Next)
		{
			var pPopup = pNode.Value;
			if (!pPopup.IsUnloading)
			{
				return pPopup.IsSelfOrAncestorLightDismiss;
			}
		}

		return false;
	}

	internal Popup? GetTopmostPopupInLightDismissChain()
	{
		// Return recently opened Popup if it has light dismiss enabled,
		// or if it has a parent that has light dismiss enabled.
		// Unloading popups don't count.
		for (var pNode = m_pOpenPopups.First; pNode is not null; pNode = pNode.Next)
		{
			var pPopup = pNode.Value;
			if (!pPopup.IsUnloading)
			{
				return pPopup.IsSelfOrAncestorLightDismiss ? pPopup : null;
			}
		}

		return null;
	}

	// If the element is an open popup, return that popup
	public static Popup? GetOpenPopupForElement(UIElement pElement)
	{
		// Early exit when there are no open popups
		var pPopupRootNoRef = VisualTree.GetPopupRootForElement(pElement);
		if (pPopupRootNoRef is null || !pPopupRootNoRef.HasOpenOrUnloadingPopups())
		{
			return null;
		}

		// If the element is in a popup, GetRootOfPopupSubTree returns the popup's subtree root.
		if (pElement.GetRootOfPopupSubTree() is FrameworkElement pPopupSubTreeNoRef)
		{
			// The logical parent of the subtree root is the popup
			if (pPopupSubTreeNoRef.LogicalParentOverride is Popup { IsOpenInternal: true, IsUnloading: false } pPopupNoRef)
			{
				return pPopupNoRef;
			}
		}

		return null;
	}

	/// <summary>
	/// Notify open popups that app's theme has changed
	/// </summary>
	internal override void NotifyThemeChangedCore(Theme theme, bool forceRefresh)
	{
		// An open popup's content is set as child of PopupRoot, and we
		// want to prevent that child from getting the PopupRoot's theme
		// in CDepedencyObject::EnterImpl. So the PopupRoot's theme is never
		// changed. Instead, when a popup is opened, it will propagate the
		// theme to its content.
		m_hasThemeChanged = true;

		// Notify open popups
		for (var pNode = m_pOpenPopups.First; pNode is not null;)
		{
			var pNext = pNode.Next;
			var pPopup = pNode.Value;

			// Notification propagates to unloading popups too
			if (pPopup.ShouldPopupRootNotifyThemeChange())
			{
				pPopup.NotifyThemeChanged(theme, forceRefresh);
			}

			pNode = pNext;
		}
	}

	internal void ClearWasOpenedDuringEngagementOnAllOpenPopups()
	{
		// Includes unloading popups too
		for (var pNode = m_pOpenPopups.First; pNode is not null; pNode = pNode.Next)
		{
			pNode.Value.SetOpenedDuringEngagement(false);
		}
	}

	internal static IList<DependencyObject> GetPopupChildrenOpenedDuringEngagement(DependencyObject element)
	{
		var popupRoot = VisualTree.GetPopupRootForElement(element);

		if (popupRoot is null)
		{
			return Array.Empty<DependencyObject>();
		}

		var openedPopups = popupRoot.GetOpenPopups();
		List<DependencyObject> popupChildrenDuringEngagement = new(openedPopups.Count);

		foreach (var popup in openedPopups)
		{
			if (popup.WasOpenedDuringEngagement && popup.Child is { } child)
			{
				popupChildrenDuringEngagement.Add(child);
			}
		}

		return popupChildrenDuringEngagement;
	}

	// components/elements/PopupRoot.cpp

	internal Popup? GetOpenPopupWithChild(UIElement child, bool checkUnloadingChildToo)
	{
		// Include unloading popups too.
		for (var pNode = m_pOpenPopups.First; pNode is not null; pNode = pNode.Next)
		{
			var popup = pNode.Value;

			// TODO Uno: checkUnloadingChildToo needs m_unloadingChild (implicit Hide animations).
			if (popup.Child == child)
			{
				return popup;
			}
		}

		return null;
	}

	internal bool HasOpenOrUnloadingPopups() => m_pOpenPopups.First is not null;

	// When closing all open popups, we can't just close them in the order that they were opened. With nested
	// popups, calling Close on the outer popup will also close the inner popup, which calls back to the popup
	// root and causes reentrancy since we're in the middle of closing the outer popup. So we need to close a
	// nested popups before closing its parent popup. This method returns open popups in an order that's safe
	// to iterate over and close.
	internal List<Popup> GetPopupsInSafeClosingOrder()
	{
		List<Popup> popupsInSafeClosingOrder = new(m_pOpenPopups.Count);

		for (var openPopupNode = m_pOpenPopups.First; openPopupNode is not null; openPopupNode = openPopupNode.Next)
		{
			var popup = openPopupNode.Value;

			// For each open popup, find a safe time to call Close on it. By default the popup is closed in natural
			// order (i.e. the order that it was opened in). But if the popup has popup ancestors that also need to
			// be closed, it must be closed before the ancestors get closed. So default to the index at end of the
			// list, then walk up the ancestor chain. For each popup ancestor, if that ancestor is already in the
			// close list, then we have to insert this popup before that ancestor.
			var insertPopupIterator = popupsInSafeClosingOrder.Count;

			for (var ancestor = popup.GetUIElementAdjustedParentInternal(false);
				// We can early exit if we determine that this popup must be closed before all other popups.
				ancestor is not null && insertPopupIterator != 0;
				ancestor = ancestor.GetUIElementAdjustedParentInternal(false))
			{
				if (ancestor is Popup { IsOpenInternal: true } ancestorPopup)
				{
					// We found an open ancestor popup. If it also needs to be closed, then make sure we're
					// closed before that ancestor.
					var ancestorIterator = popupsInSafeClosingOrder.IndexOf(ancestorPopup);
					if (ancestorIterator != -1 && ancestorIterator < insertPopupIterator)
					{
						insertPopupIterator = ancestorIterator;
					}
				}
			}

			popupsInSafeClosingOrder.Insert(insertPopupIterator, popup);
		}

		return popupsInSafeClosingOrder;
	}
}
