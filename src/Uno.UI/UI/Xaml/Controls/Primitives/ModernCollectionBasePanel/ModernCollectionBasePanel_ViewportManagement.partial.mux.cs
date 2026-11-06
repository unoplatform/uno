// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_ViewportManagement_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	// When called the first time, this method starts tracking the first
	// visible element. Otherwise, it does nothing.
	private void BeginTrackingFirstVisibleElement(bool ignoreIsTrackingExtentEnd = false)
	{
		int index = -1;
		ElementType type = ElementType.ItemContainer;

		UIElement? spFirstVisibleElement = null;
		Rect window = m_windowState.GetVisibleWindow();
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		ItemsUpdatingScrollMode itemsUpdatingScrollMode = GetItemsUpdatingScrollMode();

		// We only support ItemsStackPanel for this viewport behavior.
		// Also, if we are already tracking something, we can leave.
		if (!IsMaintainViewportSupportedAndEnabled() ||
			m_viewportBehavior.isTracking ||
			(!ignoreIsTrackingExtentEnd && m_viewportBehavior.isTrackingExtentEnd) ||
			spScrollViewer is null)
		{
#if IUSM_DEBUG
			global::System.Diagnostics.Debug.WriteLine(
				$"IUSM_DEBUG[{GetHashCode()}]: ModernCollectionBasePanel::BeginTrackingFirstVisibleElement. Early exit.");
#endif
			return;
		}

#if IUSM_DEBUG
		if (ignoreIsTrackingExtentEnd && m_viewportBehavior.isTrackingExtentEnd)
		{
			global::System.Diagnostics.Debug.WriteLine(
				$"IUSM_DEBUG[{GetHashCode()}]: ModernCollectionBasePanel::BeginTrackingFirstVisibleElement. Ignores isTrackingExtentEnd & skips early exit.");
		}
#endif

		float windowFirstEdge = PointFromRectInVirtualizingDirection(window);
		float windowSecondEdge = windowFirstEdge + SizeFromRectInVirtualizingDirection(window);

		// If we are at the very beginning and we are tracking the first visible element,
		// we don't have to do anything. KeepItemsInView actually means Keep*First*ItemInView
		// because we added KeepLastItemInView in RS1.
		if (itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepItemsInView &&
		   (windowFirstEdge <= 0.0f || DoubleUtil.IsZero((double)windowFirstEdge)))
		{
			return;
		}

		// On the other hand, if we are at the very end and the tracking mode is KeepLastItemInView,
		// we need to take note and scroll at the "new" end (if it's different from the current) so
		// that we have a behavior that's symmetric to KeepItemsInView.
		if (itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView)
		{
			// We use m_estimatedSize instead of get_DesiredSize because the latter accounts for layout rounding.
			Size extent = m_estimatedSize;
			float extentInVirtualizingDirection = PointFromSizeInVirtualizingDirection(extent);

			if ((double)extentInVirtualizingDirection <= (double)windowSecondEdge + 0.5)
			{
				m_viewportBehavior.isTrackingExtentEnd = true;
				m_viewportBehavior.originalExtent = m_viewportBehavior.currentExtent = extentInVirtualizingDirection;

#if IUSM_DEBUG
				m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::BeginTrackingFirstVisibleElement for KeepLastItemInView.");
#endif
			}
		}

		// Finds the first visible element, if any.
		GetFirstVisibleElement(out type, out index, out spFirstVisibleElement);

		if (spFirstVisibleElement is not null)
		{
			m_viewportBehavior.isTracking = true;
			m_viewportBehavior.index = index;
			m_viewportBehavior.type = type;
			m_viewportBehavior.initialViewportEdge =
				itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepItemsInView ? windowFirstEdge : windowSecondEdge;
			m_viewportBehavior.elementBounds = GetBoundsFromElement(spFirstVisibleElement);

#if IUSM_DEBUG
			m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::BeginTrackingFirstVisibleElement for FirstVisibleElement.");
#endif

			SetTrackedElementOffsetRelativeToViewport();
		}
	}

	// Stores the first visible element.
	// We need this method on orientation change to remember the first visible element in the visible window.
	// Then, when Generate() next runs (asynchronously) with the new orientation set and new realization window,
	// we can center the visible window around this element that we cached.
	// When switching orientations, we thus preserve the first element in the visible window.
	// Note that we do not preserve offset into the first visible element.
	private protected void CacheFirstVisibleElementBeforeOrientationChange()
	{
		int index = -1;
		ElementType type = ElementType.ItemContainer;

		UIElement? spFirstVisibleElement = null;

		m_tpFirstVisibleElementBeforeOrientationChange = null;
		m_typeOfFirstVisibleElementBeforeOrientationChange = ElementType.ItemContainer;
		m_indexOfFirstVisibleElementBeforeOrientationChange = -1;

		// Finds the first visible element, if any.
		GetFirstVisibleElement(out type, out index, out spFirstVisibleElement);
		if (spFirstVisibleElement is not null)
		{
			global::System.Diagnostics.Debug.Assert(index >= 0);
			m_tpFirstVisibleElementBeforeOrientationChange = spFirstVisibleElement;
			m_typeOfFirstVisibleElementBeforeOrientationChange = type;
			m_indexOfFirstVisibleElementBeforeOrientationChange = index;
		}
	}

	private Rect BeginTrackingOnOrientationChange()
	{
		UIElement? spFirstVisibleElement = m_tpFirstVisibleElementBeforeOrientationChange;
		Rect window = m_windowState.GetVisibleWindow();

		try
		{
			// if we had mutations, we kind of give up on tracking (given milestone that we are in).
			UIElement? spCurrentElementAtCachedIndex = m_containerManager.GetElementAtDataIndex(
				m_typeOfFirstVisibleElementBeforeOrientationChange,
				m_indexOfFirstVisibleElementBeforeOrientationChange);

			if (spFirstVisibleElement is not null && ReferenceEquals(spFirstVisibleElement, spCurrentElementAtCachedIndex))
			{
				Rect elementRect;
				EstimationReference resolvedHeaderReference = CreateDefaultEstimationReference(ElementType.GroupHeader);
				EstimationReference resolvedContainerReference = CreateDefaultEstimationReference(ElementType.ItemContainer);
				Orientation orientation;
				if (m_typeOfFirstVisibleElementBeforeOrientationChange == ElementType.ItemContainer)
				{
					elementRect = m_spLayoutStrategy.EstimateElementBounds(
						ElementType.ItemContainer,
						m_indexOfFirstVisibleElementBeforeOrientationChange,
						resolvedHeaderReference,
						resolvedContainerReference,
						m_windowState.GetRealizationWindow());

				}
				else
				{
					global::System.Diagnostics.Debug.Assert(m_typeOfFirstVisibleElementBeforeOrientationChange == ElementType.GroupHeader);

					elementRect = m_spLayoutStrategy.EstimateElementBounds(
						ElementType.GroupHeader,
						m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, m_indexOfFirstVisibleElementBeforeOrientationChange),
						resolvedHeaderReference,
						resolvedContainerReference,
						m_windowState.GetRealizationWindow());
				}
				SetBoundsForElement(spFirstVisibleElement, elementRect);

				window = PlaceWindowAroundElementRect(m_windowState.GetVisibleWindow(), elementRect, ScrollIntoViewAlignment.Leading, 0.0 /* offset */);

				orientation = m_spLayoutStrategy.VirtualizationDirection;
				if (orientation == Orientation.Horizontal)
				{
					// we are scrolling horizontally
					window.Y = 0;
				}
				else
				{
					window.X = 0;
				}

				m_viewportBehavior.isTracking = true;
				m_viewportBehavior.index = m_indexOfFirstVisibleElementBeforeOrientationChange;
				m_viewportBehavior.type = m_typeOfFirstVisibleElementBeforeOrientationChange;
				m_viewportBehavior.initialViewportEdge =
					GetItemsUpdatingScrollMode() == ItemsUpdatingScrollMode.KeepLastItemInView ?
					PointFromRectInVirtualizingDirection(window) + SizeFromRectInVirtualizingDirection(window) :
					PointFromRectInVirtualizingDirection(window);
				m_viewportBehavior.elementBounds = GetBoundsFromElement(spFirstVisibleElement);

#if IUSM_DEBUG
				m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::BeginTrackingOnOrientationChange.");
#endif

				SetTrackedElementOffsetRelativeToViewport();
			}

			return window;
		}
		finally
		{
			m_tpFirstVisibleElementBeforeOrientationChange = null;
			m_typeOfFirstVisibleElementBeforeOrientationChange = ElementType.ItemContainer;
			m_indexOfFirstVisibleElementBeforeOrientationChange = -1;
		}
	}

	private void BeginTrackingOnRefresh(ref Rect newVisibleWindow)
	{
		if (m_viewportBehavior.isTracking)
		{
			// Early out if we are already tracking.
			return;
		}

		if ((m_cacheManager.IsGrouping() && m_cacheManager.GetTotalLayoutGroupCount() == 0)
			|| (!m_cacheManager.IsGrouping() && m_cacheManager.GetTotalItemCount() == 0))
		{
			// If the data is empty, we get out as well.
			return;
		}

		// Begin tracking when KeepLastItemInView is set and
		// either m_refreshPendingLayout is true or we are in a
		// reset like scenario where the data isn't empty but there
		// is no container realized yet.
		if (GetItemsUpdatingScrollMode() == ItemsUpdatingScrollMode.KeepLastItemInView &&
			(m_refreshPendingLayout ||
			 (m_containerManager.GetValidContainerCount() == 0 &&
			  m_containerManager.GetValidHeaderCount() == 0)))
		{
			int scrollToTargetIndex;
			ElementType scrollToTargetType;

			// Find the index and type of the last element.
			m_cacheManager.GetLastElementInLayout(out scrollToTargetIndex, out scrollToTargetType);
			global::System.Diagnostics.Debug.Assert(scrollToTargetIndex != -1);

			// Scroll to the last element.
			if (scrollToTargetType == ElementType.GroupHeader)
			{
				DoScrollGroupHeaderIntoView(
					scrollToTargetIndex,
					ScrollIntoViewAlignment.Default,
					0.0 /* offset */,
					false /* animate */,
					out newVisibleWindow);
			}
			else
			{
				DoScrollItemIntoView(
					scrollToTargetIndex,
					ScrollIntoViewAlignment.Default,
					0.0 /* offset */,
					false /* animate */,
					out newVisibleWindow);
			}

#if IUSM_DEBUG
			global::System.Diagnostics.Debug.WriteLine(
				$"IUSM_DEBUG[{GetHashCode()}]: ModernCollectionBasePanel::BeginTrackingOnRefresh. Initiated scroll to index {scrollToTargetIndex}.");
#endif

			m_viewportBehavior.isTrackingExtentEnd = true;
			m_viewportBehavior.originalExtent = m_viewportBehavior.currentExtent = PointFromSizeInVirtualizingDirection(m_estimatedSize);

#if IUSM_DEBUG
			m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::BeginTrackingOnRefresh.");
#endif

			ItemsPresenter? itemsPresenter = null;
			m_wrItemsPresenter?.TryGetTarget(out itemsPresenter);
			if (itemsPresenter is not null)
			{
				itemsPresenter.LoadFooter(false /* updateLayout */);
			}
		}
	}

	// Starts tracking the first visible element if it's not already the case.
	// If we already tracking something, it updates the tracked element so
	// we can deal with its removal or replacement.
	private void BeginTrackingFirstVisibleElement(
		bool isCollectionChange,
		bool isGroupChange,
		int index,
		CollectionChange action = CollectionChange.Reset)
	{
		bool wasTrackingBeforeThisCall = m_viewportBehavior.isTracking;

		if (isCollectionChange && action == CollectionChange.Reset)
		{
			return;
		}

		BeginTrackingFirstVisibleElement();

		// We should keep the tracked element's index up to date.
		if (m_viewportBehavior.isTracking &&
			isCollectionChange &&
			((isGroupChange && m_viewportBehavior.type == ElementType.GroupHeader) ||
			(!isGroupChange && m_viewportBehavior.type == ElementType.ItemContainer)) &&
			index <= m_viewportBehavior.index)
		{
			switch (action)
			{
				// an element (group or item) was inserted before the tracked element.
				// we only need to update our index in this case.
				case CollectionChange.ItemInserted:
				{
					++m_viewportBehavior.index;
					break;
				}
				// an element (group or item) was removed.
				// if it was removed before the tracked element, we only need to update the index.
				// if the tracked element was removed, we need to pick a new one.
				// by convention, it's the realized element after.
				case CollectionChange.ItemRemoved:
				{
					if (index == m_viewportBehavior.index)
					{
						ElementType oldType = m_viewportBehavior.type;
						ElectNewTrackedElement();

						// -1 to account for the old tracked element that hasn't been removed yet.
						if (m_viewportBehavior.isTracking &&
							m_viewportBehavior.type == oldType &&
							GetItemsUpdatingScrollMode() == ItemsUpdatingScrollMode.KeepItemsInView)
						{
							--m_viewportBehavior.index;
						}
					}
					else
					{
						--m_viewportBehavior.index;
					}
					break;
				}
				// if the first group in the valid range is replaced by a non-empty group and we are tracking
				// the group header of the old group, we should not be tracking the group header of the new group
				// because the latter is going to be a floating header and we can't generate forward from a floating header.
				case CollectionChange.ItemChanged:
				{
					if (isGroupChange && index == m_viewportBehavior.index && index == m_containerManager.GetGroupIndexFromValidIndex(0))
					{
						int itemCountInGroup = 0;
						GetGroupInformationFromGroupIndex(index, out _ /* pStartItemIndex */, out itemCountInGroup);
						if (itemCountInGroup > 0)
						{
							ElectNewTrackedElement();
						}
					}
					break;
				}
			}
		}

		// If an element (container or header) was inserted or removed or changed size before the tracked element.
		// We need to reevaluate the layout of the tracked element, using estimations.
		if (m_viewportBehavior.isTracking &&
			(!isCollectionChange ||
			action == CollectionChange.ItemInserted ||
			action == CollectionChange.ItemRemoved))
		{
			bool isChangeBeforeTrackedElement = false;
			bool isChangeGoingToHideTrackedGroupHeader = false;

			// Is the change before the tracked element?
			IsChangeBeforeTrackedElement(isCollectionChange, isGroupChange, index, action, out isChangeBeforeTrackedElement, out isChangeGoingToHideTrackedGroupHeader);
			if (isChangeBeforeTrackedElement)
			{
				if (isCollectionChange)
				{
					float estimatedElementBoundsShift = 0.0f;

					switch (action)
					{
						case CollectionChange.ItemInserted:
						case CollectionChange.ItemRemoved:
						{
							if (isGroupChange)
							{
								// First, only account for the header itself when it's inline.
								GroupHeaderStrategy headerStrategy = GroupHeaderStrategy.None;

								headerStrategy = GetGroupHeaderStrategy();

								if (headerStrategy == GroupHeaderStrategy.Inline)
								{
									// A group header was inserted before the tracked element.
									// Attempt to access an average header size in the virtualized direction.
									estimatedElementBoundsShift = GetAverageHeaderSize();
								}
								// Else headers that are placed in Parallel and have no effect on the tracked element's position.
							}
							else
							{
								// A container was inserted before the tracked element.
								// Attempt to access an average container size in the virtualized direction.
								estimatedElementBoundsShift = GetAverageContainerSize();
							}

							// When estimatedElementBoundsShift >= 0.0f, use that successfully accessed average size as the estimated shift...
							if (estimatedElementBoundsShift < 0.0f)
							{
								// ...else fall back to using the tracked element's size.
								estimatedElementBoundsShift = SizeFromRectInVirtualizingDirection(m_viewportBehavior.elementBounds);
							}

							if (isGroupChange)
							{
								// Second, take the GroupPadding into account irrespective of the GroupHeaderStrategy value.
								double groupPadding = 0.0;

								groupPadding = GroupPaddingSizeInVirtualizingDirection();

								estimatedElementBoundsShift += (float)groupPadding;
							}
							break;
						}
					}

					switch (action)
					{
						case CollectionChange.ItemInserted:
						{
							// When an item was inserted before the tracked element, its estimated bounds are increased.
							SetPointFromRectInVirtualizingDirection(ref m_viewportBehavior.elementBounds, PointFromRectInVirtualizingDirection(m_viewportBehavior.elementBounds) + estimatedElementBoundsShift);
							break;
						}

						case CollectionChange.ItemRemoved:
						{
							// When an item was inserted before the tracked element, its estimated bounds are decreased.
							SetPointFromRectInVirtualizingDirection(ref m_viewportBehavior.elementBounds, PointFromRectInVirtualizingDirection(m_viewportBehavior.elementBounds) - estimatedElementBoundsShift);
							SetPointFromRectInVirtualizingDirection(ref m_viewportBehavior.elementBounds, Math.Max(PointFromRectInVirtualizingDirection(m_viewportBehavior.elementBounds), 0.0f));
							break;
						}
					}
				}
			}
			// Before this call, we were not tracking and it turns out the modification (collection change or size change)
			// happened *after* the first visible element. For these cases, we don't have to do anything...
			// ...except in one specific situation: HidesIfEmpty is true and we are tracking a group header whose group is now empty.
			// In this case, keep tracking the header and we will elect a new one during the Generate pass when it gets destroyed.
			// We won't do it here because HidesIfEmpty's value might change by then.
			else if (!(wasTrackingBeforeThisCall || isChangeGoingToHideTrackedGroupHeader || m_viewportBehavior.isTrackingExtentEnd))
			{
				m_viewportBehavior.Reset();
			}
		}

#if IUSM_DEBUG
		m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::BeginTrackingFirstVisibleElement.");
#endif
	}

	// Wrapper that calls BeginTrackingFirstVisibleElement when a collection change happens.
	private void BeginTrackingOnCollectionChange(
		bool isGroupChange,
		int index,
		CollectionChange action)
		=> BeginTrackingFirstVisibleElement(true /* isCollectionChange */, isGroupChange, index, action);

	// Wrapper that calls BeginTrackingFirstVisibleElement when an element changes size.
	private void BeginTrackingOnMeasureChange(bool isGroupChange, int index)
		=> BeginTrackingFirstVisibleElement(false /* isCollectionChange */, isGroupChange, index);

	// Wrapper that calls BeginTrackingFirstVisibleElement when we detect a viewport size change.
	private void BeginTrackingOnViewportSizeChange(
		Rect previousWindow,
		Rect currentWindow)
	{
		// We only need to track the first visible element (from the bottom/right) if
		// we are not already tracking, KeepLastItemInView is set and the viewport size has
		// changed. The goal is to keep the first visible element in the same position relative
		// to the viewport edge (bottom/right end of the viewport).
		if (!m_viewportBehavior.isTracking &&
			GetItemsUpdatingScrollMode() == ItemsUpdatingScrollMode.KeepLastItemInView &&
			!DoubleUtil.AreClose(
				(double)SizeFromRectInVirtualizingDirection(previousWindow),
				(double)SizeFromRectInVirtualizingDirection(currentWindow)))
		{
			// Since m_viewportBehavior.isTrackingExtentEnd is unconditionally reset below, BeginTrackingFirstVisibleElement
			// must not exit early when m_viewportBehavior.isTrackingExtentEnd is True. Instead, it must carry on and update
			// the m_viewportBehavior fields originalExtent, isTracking, index, type, initialViewportEdge and elementBounds.
			// This is particularly critical when the viewport shrinks in both dimensions.
			BeginTrackingFirstVisibleElement(true /*ignoreIsTrackingExtentEnd*/);
			// Don't allow tracking the extent end because it's not going to change.
			// We need to track a specific element and its offset to the bottom of the viewport
			// that just changed size.
			m_viewportBehavior.isTrackingExtentEnd = false;

#if IUSM_DEBUG
			m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::BeginTrackingOnViewportSizeChange.");
#endif
		}
	}

	// Called during the Generate pass to shift all the elements, including the
	// tracked element, to the new realization window.
	private void ApplyTrackedElementShift()
	{
		global::System.Diagnostics.Debug.Assert(m_viewportBehavior.isTracking);

		UIElement? spTrackedElement = GetTrackedElement();

		// Because of bug 14508725, GetTrackedElement may return null here even when m_viewportBehavior.isTracking is True.
		if (spTrackedElement is not null)
		{
			m_viewportBehavior.trackedElementShift =
				PointFromRectInVirtualizingDirection(m_viewportBehavior.elementBounds) -
				PointFromRectInVirtualizingDirection(GetBoundsFromElement(spTrackedElement));

#if IUSM_DEBUG
			m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::ApplyTrackedElementShift.");
#endif

			for (int typeIndex = 0; typeIndex < ElementType_Count; ++typeIndex)
			{
				ElementType type = (ElementType)typeIndex;
				for (int validIndex = 0; validIndex < m_containerManager.GetValidElementCount(type); ++validIndex)
				{
					UIElement? spElement = m_containerManager.GetElementAtValidIndex(type, validIndex);
					if (spElement is not null)
					{
						VirtualizationInformation pVirtualizationInfo = GetVirtualizationInformationFromElement(spElement)!;
						Rect bounds = pVirtualizationInfo.Bounds;
						SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(bounds) + m_viewportBehavior.trackedElementShift);
						pVirtualizationInfo.Bounds = bounds;
					}
				}
			}
		}
	}

	// Returns whether a change happened before the tracked element.
	private void IsChangeBeforeTrackedElement(
		bool isCollectionChange,
		bool isGroupChange,
		int index,
		CollectionChange action,
		out bool isChangeBeforeTrackedElement,
		out bool isChangeGoingToHideTrackedGroupHeader)
	{
		isChangeBeforeTrackedElement = false;
		isChangeGoingToHideTrackedGroupHeader = false;

		global::System.Diagnostics.Debug.Assert(!isCollectionChange ||
			action == CollectionChange.ItemInserted ||
			action == CollectionChange.ItemRemoved);

		if (isCollectionChange)
		{
			if (m_viewportBehavior.type == ElementType.ItemContainer)
			{
				if (isGroupChange)
				{
					// We are tracking an item and a group was added.
					if (action == CollectionChange.ItemInserted)
					{
						int startItemIndexOfNewGroup = 0;

						// At this point, the cache manager know about the new group. But we haven't
						// got the item added notification yet (we won't get any for empty groups).

						m_cacheManager.GetGroupInformationFromGroupIndex(index, out startItemIndexOfNewGroup, out _);
						isChangeBeforeTrackedElement = (startItemIndexOfNewGroup <= m_viewportBehavior.index);
					}
					// We are tracking an item and a group was removed.
					else if (action == CollectionChange.ItemRemoved)
					{
						int groupIndexOfTrackedItem = 0;

						// Information on the removed group is not in the cache manager.
						// All we know is its index, so let's compare between group indices.

						m_cacheManager.GetGroupInformationFromItemIndex(m_viewportBehavior.index, out groupIndexOfTrackedItem, out _, out _);
						isChangeBeforeTrackedElement = (index <= groupIndexOfTrackedItem);
					}
				}
				// isGroupChange == false
				else
				{
					// We are tracking an item and an item was added
					if (action == CollectionChange.ItemInserted)
					{
						isChangeBeforeTrackedElement = (index < m_viewportBehavior.index);
					}
					// we are tracking an item and an item was removed.
					else if (action == CollectionChange.ItemRemoved)
					{
						// Note: we also check for equality on remove because the tracked element index was
						// updated at this point.
						isChangeBeforeTrackedElement = (index <= m_viewportBehavior.index);
					}
				}
			}
			else
			{
				if (isGroupChange)
				{
					// We are tracking a header and a group was added.
					if (action == CollectionChange.ItemInserted)
					{
						// At this point, the cache manager knows about the new group.
						isChangeBeforeTrackedElement = (index < m_viewportBehavior.index);
					}
					// We are tracking a header and a group was removed.
					else if (action == CollectionChange.ItemRemoved)
					{
						// At this point, information on the removed group is not in the cache manager.
						// All we know is its index, so let's compare between group indices.
						isChangeBeforeTrackedElement = (index <= m_viewportBehavior.index);
					}
				}
				// isGroupChange == false
				else
				{
					// We are tracking a header and an item was added.
					if (action == CollectionChange.ItemInserted)
					{
						int groupIndexOfNewItem = 0;
						m_cacheManager.GetGroupInformationFromItemIndex(index, out groupIndexOfNewItem, out _, out _);

						isChangeBeforeTrackedElement = (groupIndexOfNewItem < m_viewportBehavior.index);
					}
					// We are tracking a header and an item was removed.
					else if (action == CollectionChange.ItemRemoved)
					{
						// If the group is being reset, the cache has no information about it at this point.
						// Fortunately, we stored the index.
						if (m_containerManager.IsGroupBeingReset())
						{
							isChangeBeforeTrackedElement = (m_containerManager.GetIndexOfGroupBeingReset() < m_viewportBehavior.index);
						}
						else
						{
							// We use the cache history here because after a group item is removed and the cache is reset,
							// there is no way to tell precisely what group it used to be in.
							// In other words, we run into this ambiguity: did we delete the first item of the tracked group
							// or the last item of the previous group?
							int groupIndexOfRemovedItem = 0;
							m_cacheManager.GetGroupHistoryInformationFromItemIndex(index, out groupIndexOfRemovedItem, out _, out _);

							isChangeBeforeTrackedElement = (groupIndexOfRemovedItem < m_viewportBehavior.index);
						}

						isChangeGoingToHideTrackedGroupHeader = !ShouldElementBeVisible(ElementType.GroupHeader, m_viewportBehavior.index);
					}
				}
			}
		}
		// isCollectionChange == false
		else
		{
			CollectionIterator iterator = new(m_cacheManager, m_viewportBehavior.index, m_viewportBehavior.type);
			var trackedElement = iterator.GetCurrent();

			// Check if the tracked element or something before it changed size.
			// The 'before tracked element' idea for measure changes is relative to the far edge.

			// We are tracking an item...
			if (m_viewportBehavior.type == ElementType.ItemContainer &&
				((isGroupChange && index <= trackedElement.groupIndex) ||
				(!isGroupChange && index <= trackedElement.itemIndex)))
			{
				isChangeBeforeTrackedElement = true;
			}
			// We are tracking a group header...
			else if (m_viewportBehavior.type == ElementType.GroupHeader &&
				((isGroupChange && index <= trackedElement.groupIndex) ||
				(!isGroupChange && index < trackedElement.itemIndex)))
			{
				isChangeBeforeTrackedElement = true;
			}
		}
	}

	private void TrackLastElement()
	{
		if (m_viewportBehavior.isTrackingExtentEnd)
		{
			// We only need to follow the extent's end from now on.
			m_viewportBehavior.isTracking = false;

			Size panelExtent = EstimatePanelExtent();

			float requiredViewportShift =
				PointFromSizeInVirtualizingDirection(panelExtent) - m_viewportBehavior.currentExtent;

			m_viewportBehavior.currentExtent = PointFromSizeInVirtualizingDirection(panelExtent);

			if (requiredViewportShift > 0.0)
			{
				WindowState.WindowCommand func = (ref Rect pNewVisibleWindow) =>
				{
					Orientation orientation;
					Rect newWindow = m_windowState.GetVisibleWindow();
					orientation = m_spLayoutStrategy.VirtualizationDirection;
					if (orientation == Orientation.Horizontal)
					{
						newWindow.X += requiredViewportShift;
					}
					else
					{
						newWindow.Y += requiredViewportShift;
					}

					pNewVisibleWindow = newWindow;
				};
				m_windowState.m_command = func;
			}

#if IUSM_DEBUG
			m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::TrackLastElement.");
#endif
		}
	}

	// Called during arrange.
	// Moves the viewport to the new location of the first visible element.
	private void EndTrackingFirstVisibleElement()
	{
		float viewportShift = 0.0f;

		if (m_viewportBehavior.isTrackingExtentEnd)
		{
			Size extent = DesiredSize;
			Point extentPoint = new Point(extent.Width, extent.Height);
			viewportShift = PointFromPointInVirtualizingDirection(extentPoint) - m_viewportBehavior.originalExtent;
			m_viewportBehavior.Reset();
		}
		else if (m_viewportBehavior.isTracking)
		{
			UIElement? spTrackedElement = GetTrackedElement();

			// Because of bug 14508453, GetTrackedElement may return null here even when m_viewportBehavior.isTracking is True.
			if (spTrackedElement is not null)
			{
				m_viewportBehavior.elementBounds = GetBoundsFromElement(spTrackedElement);

				float newViewportEdge = GetNewViewportEdge();

				viewportShift = newViewportEdge - m_viewportBehavior.initialViewportEdge;

				// Shifts the viewport.
				Rect visibleWindow = m_windowState.GetVisibleWindow();
				float windowShift =
					newViewportEdge -
					(GetItemsUpdatingScrollMode() == ItemsUpdatingScrollMode.KeepItemsInView ?
						PointFromRectInVirtualizingDirection(visibleWindow) :
						PointFromRectInVirtualizingDirection(visibleWindow) + SizeFromRectInVirtualizingDirection(visibleWindow)) +
					m_viewportBehavior.viewportOffsetDelta;

				m_viewportBehavior.Reset();
				if (!DoubleUtil.IsZero(windowShift))
				{
					Point correction = default;
					SetPointFromPointInVirtualizingDirection(ref correction, windowShift);

					// Scroll to the new window by coercing the visible window.
					m_windowState.ApplyAdjustment(correction);

#if IUSM_DEBUG
					global::System.Diagnostics.Debug.WriteLine(
						$"IUSM_DEBUG[{GetHashCode()}]: ModernCollectionBasePanel::EndTrackingFirstVisibleElement. Applied windowShift: {windowShift}.");
#endif
				}
			}
		}

#if IUSM_DEBUG
		m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::EndTrackingFirstVisibleElement.");
#endif

		AdjustLayoutTransitions(viewportShift);
	}

	// Called during MeasureExistingElements to get the realized window accounting for the viewport shift.
	// This make sure ShouldContinueFillingUpSpace and recycling logic behaves correctly.
	private Rect GetRealizationWindowAfterViewportShift()
	{
		global::System.Diagnostics.Debug.Assert(m_viewportBehavior.isTracking);

		Rect realizedWindow = m_windowState.GetVisibleWindow();
		float newViewportOffset =
			GetNewViewportEdge() -
			(GetItemsUpdatingScrollMode() == ItemsUpdatingScrollMode.KeepItemsInView ? 0 : SizeFromRectInVirtualizingDirection(realizedWindow));

		// Adjust the visible and realized window without scrolling.
		SetPointFromRectInVirtualizingDirection(
			ref realizedWindow,
			Math.Max(newViewportOffset, 0.0f));

		return realizedWindow;
	}

	// Called before arrange.
	// Adjust layout transitions to account for viewport shift.
	private void AdjustLayoutTransitions(float viewportShift)
	{
		if (!DoubleUtil.IsZero(viewportShift))
		{
			Orientation layoutOrientation = m_spLayoutStrategy.VirtualizationDirection;

			// If we are tracking the last element and the list is not scrollable,
			// there is not going to be any "viewport shift" from the user's perspective
			// unless we are bottom/right aligned. If we are not, we should bail out.
			if (GetItemsUpdatingScrollMode() == ItemsUpdatingScrollMode.KeepLastItemInView)
			{
				bool isListScrollable = false;
				ScrollViewer? scrollViewer = null;
				m_wrScrollViewer?.TryGetTarget(out scrollViewer);
				if (scrollViewer is not null)
				{
					double scrollableSize = layoutOrientation == Orientation.Horizontal ?
						scrollViewer.ScrollableWidth :
						scrollViewer.ScrollableHeight;
					isListScrollable = !DoubleUtil.IsZero(scrollableSize);
				}

				if (!isListScrollable)
				{
					bool isBottomOrRightAligned = false;
					ItemsPresenter? itemsPresenter = null;
					m_wrItemsPresenter?.TryGetTarget(out itemsPresenter);
					if (itemsPresenter is not null)
					{
						isBottomOrRightAligned = IsBottomOrRightAligned(itemsPresenter, layoutOrientation);
					}
					if (!isBottomOrRightAligned)
					{
						isBottomOrRightAligned = IsBottomOrRightAligned(this, layoutOrientation);
					}
					if (!isBottomOrRightAligned) { return; }
				}
			}

			IList<UIElement> spChildren = m_cacheManager.GetChildren();

			// Containers and headers usually transition from their old location to their new location location.
			// To account for the viewport shift, they should transition from their (old location + viewport shift)
			// to their new location.
			for (int i = 0; i < m_containerManager.StartOfGarbageSection(); ++i)
			{
				UIElement spElement = spChildren[i];
				AdjustLayoutTransitionsForElement(spElement, viewportShift, layoutOrientation);
			}

			uint unloadingElementsCount = (uint)m_unloadingElements.Count;
			for (uint i = 0; i < unloadingElementsCount; ++i)
			{
				UIElement spElement = m_unloadingElements[(int)i];
				AdjustLayoutTransitionsForElement(spElement, viewportShift, layoutOrientation);
			}
		}
	}

	private void AdjustLayoutTransitionsForElement(
		UIElement element,
		float viewportShift,
		Orientation layoutOrientation)
	{
		VirtualizationInformation? virtualizationInformation;
		virtualizationInformation = GetVirtualizationInformationFromElement(element);

		var pTarget = element;

		// Ignore newly realized elements.
		if (pTarget.m_pLayoutTransitionStorage is { } transitionStorage &&
			transitionStorage.m_enteredTreeCounter != 0 &&
			pTarget.IsFirstArrangeDone)
		{
			// finalRect includes Margin of the element.
			// We sould use m_offset and m_size to provide right element size to transition location context.
			var slot = pTarget.LayoutSlotWithMarginsAndAlignments;
			var currentOffset = new Point(slot.X, slot.Y);

			if (layoutOrientation == Orientation.Horizontal)
			{
				currentOffset.X += viewportShift;
			}
			else
			{
				currentOffset.Y += viewportShift;
			}

			bool hasTransitions = false;
			CoreImports.UIElement_GetHasTransition(pTarget, out hasTransitions, out _);
			if (hasTransitions)
			{
				// pTarget is fading out (being unloaded). It's too late to set its current
				// transition location with CoreImports::UIElement_SetCurrentTransitionLocation. We need
				// to update m_transformStart so that it gets picked up by the transition system.
				transitionStorage.m_transformStart = currentOffset;
			}
			else
			{
				element.InvalidateArrange();
				CoreImports.UIElement_SetCurrentTransitionLocation(
					pTarget,
					(float)currentOffset.X,
					(float)currentOffset.Y,
					(float)slot.Width,
					(float)slot.Height);
			}
		}
	}

	// Returns the first visible element's type, index and offset relative to the top.
	internal void GetFirstVisibleElementForSerialization(
		out ElementType type,
		out double offset,
		out UIElement? uiElement)
	{
		int elementIndex = 0;
		Rect window = m_windowState.GetVisibleWindow();
		Rect actualWindow = default; // adjusted for sticky headers, if any.

		offset = 0.0;

		// Finds the first visible element, if any.
		GetFirstElementInWindow(window, out type, out elementIndex, out uiElement, out actualWindow);

		if (elementIndex != -1)
		{
			global::System.Diagnostics.Debug.Assert(uiElement is not null);
			Rect elementBounds = GetBoundsFromElement(uiElement!);
			offset =
				GetItemsUpdatingScrollMode() == ItemsUpdatingScrollMode.KeepLastItemInView ?
				PointFromRectInVirtualizingDirection(actualWindow) + SizeFromRectInVirtualizingDirection(actualWindow) - PointFromRectInVirtualizingDirection(elementBounds) :
				PointFromRectInVirtualizingDirection(actualWindow) - PointFromRectInVirtualizingDirection(elementBounds);
		}
	}

	internal double AdjustViewportOffsetForDeserialization(double offset)
	{
		if (GetItemsUpdatingScrollMode() == ItemsUpdatingScrollMode.KeepLastItemInView)
		{
			offset = offset - SizeFromRectInVirtualizingDirection(m_windowState.GetVisibleWindow());
		}
		return offset;
	}

	// Returns the first visible element's type, index and UIElement.
	private void GetFirstVisibleElement(
		out ElementType type,
		out int index,
		out UIElement? uiElement)
		=> GetFirstElementInWindow(m_windowState.GetVisibleWindow(), out type, out index, out uiElement, out _ /* pAdjustedWindowForStickyHeaders */);

	// Returns the first visible element's type, index and UIElement
	// in a given window.
	// The adjustedWindowForStickyHeaders parameter is the given window adjusted for sticky headers if they
	// are active. Otherwise, it's the same as window.
	private void GetFirstElementInWindow(
		Rect window,
		out ElementType elementType,
		out int elementIndex,
		out UIElement? uiElement,
		out Rect adjustedWindowForStickyHeaders)
	{
		bool found = false;

		int[] currentValidIndices = new int[ElementType_Count];
		double[] elementsOffsets = new double[ElementType_Count];
		UIElement?[] currentUIElements = new UIElement?[ElementType_Count];
		Rect windowAdjustedForStickyHeaders = window;
		windowAdjustedForStickyHeaders.Y += m_lastVisibleWindowClippingHeight;
		windowAdjustedForStickyHeaders.Height -= m_lastVisibleWindowClippingHeight;

		Array.Fill(currentValidIndices, 0);
		Array.Fill(elementsOffsets, DoubleUtil.MaxValue);

		uiElement = null;
		elementType = ElementType.ItemContainer;
		elementIndex = -1;
		adjustedWindowForStickyHeaders = window;

		ItemsUpdatingScrollMode itemsUpdatingScrollMode = GetItemsUpdatingScrollMode();

		// GetFirstElementInWindow is used when elected a new tracked element.
		// In this case, we would like to ignore the currently tracked element.
		int trackedElementValidIndex = m_viewportBehavior.isTracking
			? m_containerManager.GetValidElementIndexFromDataIndex(m_viewportBehavior.type, m_viewportBehavior.index)
			: -1;

		// We will be going through the valid range.
		// For each type, retrieve the offset relative to the viewport and store it.
		for (int typeIndex = 0; typeIndex < ElementType_Count; ++typeIndex)
		{
			ElementType type = (ElementType)typeIndex;
			if (m_containerManager.GetValidElementCount(type) == 0) { continue; }

			Rect intersectionWindow = (typeIndex == (int)ElementType.ItemContainer) ? windowAdjustedForStickyHeaders : window;

			int startIndex =
				itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView ?
				m_containerManager.GetValidElementCount(type) - 1 :
				currentValidIndices[typeIndex];

			int endIndex =
				itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView ?
				currentValidIndices[typeIndex] - 1 :
				m_containerManager.GetValidElementCount(type);

			int increment = (itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView) ? -1 : 1;

			for (int validIndex = startIndex; validIndex != endIndex; validIndex += increment)
			{
				Rect bounds = default;
				UIElement? spElement;

				if (validIndex == trackedElementValidIndex && m_viewportBehavior.type == type)
				{
					continue;
				}

				spElement = m_containerManager.GetElementAtValidIndex(type, validIndex);

				// Ignores any sentinel.
				if (GetElementIsSentinel(spElement))
				{
					continue;
				}

				bounds = GetBoundsFromElement(spElement!);

				// We don't want elements to overlap over one line.
				SetSizeFromRectInVirtualizingDirection(ref bounds, SizeFromRectInVirtualizingDirection(bounds) - EdgeOverlayDisambiguationDelta);

				if (!RectUtil.AreDisjoint(bounds, intersectionWindow))
				{
					elementsOffsets[typeIndex] =
						itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView ?
						(PointFromRectInVirtualizingDirection(window) + SizeFromRectInVirtualizingDirection(window)) -
							(PointFromRectInVirtualizingDirection(bounds) + SizeFromRectInVirtualizingDirection(bounds)) :
						PointFromRectInVirtualizingDirection(bounds) - PointFromRectInVirtualizingDirection(window);
					currentUIElements[typeIndex] = spElement;
					currentValidIndices[typeIndex] = m_containerManager.GetDataIndexFromValidIndex(type, validIndex);
					found = true;
					break;
				}
			}
		}

		// If we found something, return it.
		if (found)
		{
			// Preference goes to header.
			var type = (elementsOffsets[(int)ElementType.GroupHeader] <= elementsOffsets[(int)ElementType.ItemContainer])
				? ElementType.GroupHeader
				: ElementType.ItemContainer;

			elementType = type;
			elementIndex = currentValidIndices[(int)type];
			uiElement = currentUIElements[(int)type];

			if (m_bUseStickyHeaders && type == ElementType.ItemContainer)
			{
				adjustedWindowForStickyHeaders = windowAdjustedForStickyHeaders;
			}
		}
	}

	// Sets the offset between the tracked element and the visible window.
	private void SetTrackedElementOffsetRelativeToViewport()
	{
		global::System.Diagnostics.Debug.Assert(m_viewportBehavior.isTracking);

		// Note: trackedElementShift's value is always zero outside of Measure/Arrange.
		float effectiveViewport = m_viewportBehavior.initialViewportEdge + m_viewportBehavior.trackedElementShift;
		Rect bounds = m_viewportBehavior.elementBounds;
		ItemsUpdatingScrollMode itemsUpdatingScrollMode = GetItemsUpdatingScrollMode();
		float boundsFirstEdge =
			itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView ?
			PointFromRectInVirtualizingDirection(bounds) + SizeFromRectInVirtualizingDirection(bounds) :
			PointFromRectInVirtualizingDirection(bounds);

		// The calculated offset between the element and the viewport is relative to an edge (first or second from the top\left).
		if (itemsUpdatingScrollMode != ItemsUpdatingScrollMode.KeepLastItemInView ||
			(itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView && boundsFirstEdge <= effectiveViewport))
		{
			m_viewportBehavior.isOffsetRelativeToSecondEdge = false;
			m_viewportBehavior.elementOffset = effectiveViewport - boundsFirstEdge;
		}
		else
		{
			float boundsSecondEdge =
				itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView ?
				PointFromRectInVirtualizingDirection(bounds) :
				PointFromRectInVirtualizingDirection(bounds) + SizeFromRectInVirtualizingDirection(bounds);

			m_viewportBehavior.isOffsetRelativeToSecondEdge = true;
			m_viewportBehavior.elementOffset = effectiveViewport - boundsSecondEdge;
		}

#if IUSM_DEBUG
		m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::SetTrackedElementOffsetRelativeToViewport.");
#endif
	}

	// Returns the tracked UIElement or null.
	// Because of bugs 14508453 & 14508725, this method may return null even when m_viewportBehavior.isTracking is True.
	private UIElement? GetTrackedElement()
	{
		int validIndex = m_containerManager.GetValidElementIndexFromDataIndex(m_viewportBehavior.type, m_viewportBehavior.index);

		global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidElementCount(m_viewportBehavior.type) > 0);
		global::System.Diagnostics.Debug.Assert(m_containerManager.IsValidElementIndexWithinBounds(m_viewportBehavior.type, validIndex));

		UIElement? spElement = m_containerManager.GetElementAtValidIndex(
			m_viewportBehavior.type,
			validIndex);

		return spElement;
	}

	// When the tracked element gets removed, we need to elect a new one.
	private void ElectNewTrackedElement()
	{
		UIElement? spOldTrackedElement;
		UIElement? spNewTrackedElement;
		Rect elementBounds;
		ElementType elementType = ElementType.ItemContainer;
		int elementIndex = 0;
		Rect windowStartingAtOldTrackedElement = m_windowState.GetVisibleWindow();

		global::System.Diagnostics.Debug.Assert(m_viewportBehavior.isTracking);

		spOldTrackedElement = GetTrackedElement();

		// The new tracked element will be the first one that visually come after/before the currently
		// tracked element .
		SetPointFromRectInVirtualizingDirection(
			ref windowStartingAtOldTrackedElement,
			GetItemsUpdatingScrollMode() == ItemsUpdatingScrollMode.KeepItemsInView ?
			PointFromRectInVirtualizingDirection(GetBoundsFromElement(spOldTrackedElement!)) :
			PointFromRectInVirtualizingDirection(GetBoundsFromElement(spOldTrackedElement!)) - SizeFromRectInVirtualizingDirection(windowStartingAtOldTrackedElement));

		GetFirstElementInWindow(windowStartingAtOldTrackedElement, out elementType, out elementIndex, out spNewTrackedElement, out _ /* pAdjustedWindowForStickyHeaders */);

		elementBounds = spNewTrackedElement is not null ? GetBoundsFromElement(spNewTrackedElement) : default;

		if (spNewTrackedElement is not null &&
		   !RectUtil.AreDisjoint(elementBounds, m_windowState.GetVisibleWindow()))
		{
			m_viewportBehavior.index = elementIndex;
			m_viewportBehavior.type = elementType;
			m_viewportBehavior.elementBounds = elementBounds;

			SetTrackedElementOffsetRelativeToViewport();
		}
		else
		{
			// If we can't elect a new tracked element from the realized range, cancel the tracking.
			// The reason we don't allow tracked elements outside the visible window is that we can't
			// guarantee that they will be protected from recycling during the next layout.
			// In other words, they might be outside the window returned by GetRealizationWindowAfterViewportShift.
			// The latter is based off the size of the visible window.
			m_viewportBehavior.Reset();
		}

#if IUSM_DEBUG
		m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::ElectNewTrackedElement.");
#endif
	}

	// Returns where the viewport should move to 'follow' the tracked element.
	private float GetNewViewportEdge()
	{
		float newViewportOffset = 0;

		global::System.Diagnostics.Debug.Assert(m_viewportBehavior.isTracking);

		Rect bounds = m_viewportBehavior.elementBounds;
		ItemsUpdatingScrollMode itemsUpdatingScrollMode = GetItemsUpdatingScrollMode();

		if (m_viewportBehavior.isOffsetRelativeToSecondEdge)
		{
			float boundsSecondEdge =
				itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView ?
				PointFromRectInVirtualizingDirection(bounds) :
				PointFromRectInVirtualizingDirection(bounds) + SizeFromRectInVirtualizingDirection(bounds);

			newViewportOffset = boundsSecondEdge + m_viewportBehavior.elementOffset;
		}
		else
		{
			float boundsFirstEdge =
				itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView ?
				PointFromRectInVirtualizingDirection(bounds) + SizeFromRectInVirtualizingDirection(bounds) :
				PointFromRectInVirtualizingDirection(bounds);

			newViewportOffset = boundsFirstEdge + m_viewportBehavior.elementOffset;
		}

		return newViewportOffset;
	}

	private bool IsBottomOrRightAligned(
		FrameworkElement element,
		Orientation orientation)
	{
		bool result = false;
		if (orientation == Orientation.Horizontal)
		{
			HorizontalAlignment alignment = element.HorizontalAlignment;
			result = (alignment == HorizontalAlignment.Right);
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(orientation == Orientation.Vertical);
			VerticalAlignment alignment = element.VerticalAlignment;
			result = (alignment == VerticalAlignment.Bottom);
		}
		return result;
	}

	private void UpdateViewportOffsetDelta(
		Rect oldVisibleWindow,
		Rect newVisibleWindow)
	{
		// This condition is true when we get a ViewChanging notification
		// between the time we started tracking (before layout, ie during a collection change)
		// and the time we run layout (IsMeasureDirty returns false).
		// We use the viewport delta to adjust, later during arrange, the window shift.
		if (m_viewportBehavior.isTracking &&
			m_viewportBehavior.viewportOffsetDelta == 0.0f &&
			IsMeasureDirty)
		{
			m_viewportBehavior.viewportOffsetDelta =
				PointFromRectInVirtualizingDirection(newVisibleWindow) -
				PointFromRectInVirtualizingDirection(oldVisibleWindow);

#if IUSM_DEBUG
			m_viewportBehavior.TraceChangesDbg("ModernCollectionBasePanel::UpdateViewportOffsetDelta.");
#endif
		}
	}
}
