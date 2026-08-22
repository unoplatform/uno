// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBase_Partial_SemanticZoomInformation.cpp, commit dc46907e92

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	private object? m_deferredSemanticZoomScrollItem;
	private ScrollIntoViewAlignment m_deferredAlignment;

	partial void OnApplyTemplatePartial() => ExecuteDeferredSemanticZoomScroll();

	private protected override void OnItemsPanelRootPrepared()
	{
		base.OnItemsPanelRootPrepared();
		ExecuteDeferredSemanticZoomScroll();
	}

	// Prepare the view for a zoom transition.
	public void InitializeViewChange()
	{
		// block scrollbars from showing during sezo operation
		m_tpScrollViewer?.BlockIndicatorsFromShowing();

		// need to precalculate the focusstate our item is going to be in
		// since the sezo will lose the trigger state soon
		if (SemanticZoomOwner is { } semanticZoomOwner)
		{
			m_semanticZoomCompletedFocusState = FocusState.Programmatic;

			if (semanticZoomOwner.GetIsProcessingKeyboardInput() ||
				semanticZoomOwner.GetIsProcessingPointerInput())
			{
				// will set focus to the destination element using either keyboard or pointer focus depending on what
				// triggered the statechange.
				m_semanticZoomCompletedFocusState =
					semanticZoomOwner.GetIsProcessingKeyboardInput()
						? FocusState.Keyboard
						: FocusState.Pointer;
			}
		}
	}

	// Cleanup the view after a zoom transition.
	public void CompleteViewChange()
	{
		// unblock scrollbars from showing during sezo operation
		m_tpScrollViewer?.ResetBlockIndicatorsFromShowing();
	}

	// Forces content to scroll until the coordinate space of the SemanticZoomLocation is
	// visible.
	public void MakeVisible(SemanticZoomLocation item)
	{
		var isJumpListEnabled = SemanticZoomOwner is not null;

		// For threshold, the jumplist behavior of aligning the items in zoomed out view and zoomed in view
		// is the converged behavior
		var isZoomedInView = isJumpListEnabled && IsZoomedInView;
		if (item.Item is not { } semanticItem)
		{
			return;
		}

		if (m_tpScrollViewer is not { } scrollViewer)
		{
			m_deferredAlignment = item.IsBottomAlignment
				? ScrollIntoViewAlignment.Default
				: ScrollIntoViewAlignment.Leading;
			m_deferredSemanticZoomScrollItem = semanticItem;
			return;
		}

		if (!CanScrollIntoView())
		{
			return;
		}

		// Get the index of the corresponding item
		// this could be a group, let's try that first
		var foundGroup = TryGetGroupItemIndex(semanticItem, out var groupItemIndex);
		var targetItem = ResolveSemanticZoomItem(semanticItem);
		if (targetItem is null)
		{
			return;
		}

		// On the phone when exiting the JumpList, we want to force top alignment for the selected group
		var alignment = isJumpListEnabled && isZoomedInView && foundGroup
			? ScrollIntoViewAlignment.Leading
			: ScrollIntoViewAlignment.Default;

		// whether spItem is a group or an actual item, this should work
		ScrollIntoView(targetItem, alignment);
		UpdateLayout(); // need to flush layout to get the container into view

		// let's get the position relative to the root
		// this is how we communicated the location of the source itemcontainer
		FrameworkElement? container = foundGroup
			? GetGroupHeaderContainer(groupItemIndex)
			// fallback to expecting just a regular item
			: GetSemanticZoomContainerAtIndex(GetSemanticZoomIndexFromItem(targetItem)) as FrameworkElement;
		if (container is null)
		{
			return;
		}

		// current destination location
		var destinationContainerLocation =
			container.TransformToVisual(this).TransformPoint(default);
		var destinationContainerNewLocation = default(Rect);
		var readLocationFromISZL = true;

		// If we are following jumplist behavior, we may want the item to be bottom aligned:
		//   (a) if we are in a JumpList scenario
		//   (b) and we are the ZoomedOut view
		if (isJumpListEnabled && !isZoomedInView)
		{
			destinationContainerNewLocation.X = destinationContainerLocation.X;
			destinationContainerNewLocation.Y = item.IsBottomAlignment
				? ActualHeight - container.ActualHeight
				: 0;
			readLocationFromISZL = false;
		}

		if (readLocationFromISZL)
		{
			// the location where we really want this container to live
			destinationContainerNewLocation = item.Bounds;
		}

		// need to take care of both horizontal and vertical based on the modes of our ScrollViewer
		var shouldScrollVertically = scrollViewer.ScrollableHeight > 0;
		var shouldScrollHorizontally = scrollViewer.ScrollableWidth > 0;
		scrollViewer.ComputePixelViewportWidth(null, false, out var viewportWidth);
		scrollViewer.ComputePixelViewportHeight(null, false, out var viewportHeight);

		while (shouldScrollHorizontally || shouldScrollVertically)
		{
			// offset we wish to be at:
			var amountToScroll = shouldScrollHorizontally
				? destinationContainerNewLocation.X - destinationContainerLocation.X
				: destinationContainerNewLocation.Y - destinationContainerLocation.Y;

			// we should not scroll further then the edge of our viewport
			// the remainer will be done using a rendertransform on the view by the sezo

			// limit to the left: a container can be placed flush against the left of the viewport
			amountToScroll = Math.Max(
				amountToScroll,
				shouldScrollHorizontally ? -destinationContainerLocation.X : -destinationContainerLocation.Y);

			// limit to the right: a container can be placed against the right of the viewport,taking into account its width or height so it is fully visible.
			amountToScroll = Math.Min(
				amountToScroll,
				shouldScrollHorizontally
					? viewportWidth - destinationContainerLocation.X - container.ActualWidth
					: viewportHeight - destinationContainerLocation.Y - container.ActualHeight);

			var originalOffset = shouldScrollHorizontally
				? scrollViewer.GetPixelHorizontalOffset()
				: scrollViewer.GetPixelVerticalOffset();

			// Note that ScrollByPixelDelta uses the 2nd argument for non-logical scrolling scenarios (say, with StackPanels), while
			// it uses the 3d argument for logical scrolling scenarios.
			// Both the 2nd and 3rd arguments need to be expressed in pixels.
			scrollViewer.ScrollByPixelDelta(
				shouldScrollHorizontally,
				originalOffset - amountToScroll,
				-amountToScroll,
				false /*isDManipInput*/);

			// setup for next iteration
			if (shouldScrollHorizontally)
			{
				shouldScrollHorizontally = false;
			}
			else
			{
				shouldScrollVertically = false;
			}
		}

		// communicate where the destination container actually ended up
		// this allows the SeZo to apply extra transforms
		UpdateLayout(); // need to flush layout to get the container in the right position
		destinationContainerLocation =
			container.TransformToVisual(this).TransformPoint(default);
		// point of the container will be relative to this listview
		destinationContainerNewLocation.X -= destinationContainerLocation.X;
		destinationContainerNewLocation.Y -= destinationContainerLocation.Y;
		destinationContainerNewLocation.Width = container.ActualWidth;
		destinationContainerNewLocation.Height = container.ActualHeight;
		// put the delta in the remainder field - this is the remainder that we were unable to scroll
		// we are currently in lockdown phase, so no new api is allowed.
		// ofcourse, we would like to make this public and put the remainder field in the IDL or
		// have some better mechanism to pass this value.
		item.Remainder = destinationContainerNewLocation;
	}

	private void ExecuteDeferredSemanticZoomScroll()
	{
		if (m_deferredSemanticZoomScrollItem is not { } semanticItem ||
			m_tpScrollViewer is null ||
			ItemsPanelRoot is null)
		{
			return;
		}

		m_deferredSemanticZoomScrollItem = null;
		if (ResolveSemanticZoomItem(semanticItem) is { } targetItem)
		{
			ScrollIntoView(targetItem, m_deferredAlignment);
		}
	}

	// When this ListViewBase is the active view and we're changing to the other
	// view, optionally provide the source and destination items.
	//
	// Two modes:
	//  1. user has tapped an item and this listview has decided to toggle a semantic zoom based on that
	//  2. user has used a DM gesture such as pinch or zoomout and SemanticZoom has decided to perform a semantic zoom
	//
	//  in the first case we can use the focused element
	//  in the second case we need to find out what the closest element is to the center point
	public void StartViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)
	{
		object? sourceItem = source.Item;
		var sourceIndex = -1;
		// the container that represents our source.
		FrameworkElement? container = null;
		var jumpListAlignmentBehavior = false;
		var zoomPoint = source.ZoomPoint;

		try
		{
			if (sourceItem is null)
			{
				var validZoomPoint = zoomPoint.X != 0 || zoomPoint.Y != 0;

				// only use zoompoint if there was a gesture that got us to a zoompoint
				if (validZoomPoint)
				{
					// scenario 2: the user has zoomed with DM or used ctrl-mousewheel and that is why we are doing this viewchange
					(sourceItem, sourceIndex, container) = FindClosestSemanticZoomItem(zoomPoint);
				}
				else
				{
					// Let's check if we are in a jump list situation
					if (SemanticZoomOwner is { } semanticZoomOwner)
					{
						jumpListAlignmentBehavior = true;
						// SemanticZoom checks other conditions than the apiset such as type of ZI/ZO views,
						// popup in template, etc
						if (!IsZoomedInView &&
							semanticZoomOwner.GetIsCancellingJumpList())
						{
							// We are the Zoomed Out View
							// Cancel: don't set a destination item
							return;
						}
					}

					if (jumpListAlignmentBehavior && IsZoomedInView)
					{
						// We are the ZoomedInView
						var startIndex = 0;
						var bottomAlignment = false;
						if (m_tpSZRequestingItem is not null)
						{
							// If a group has requested the change, let's take it as reference
							if (TryGetGroupIndexFromHeader(m_tpSZRequestingItem, out var groupIndex))
							{
								startIndex = groupIndex;
							}
							bottomAlignment = true;
						}

						(sourceIndex, sourceItem) = FindFirstItem(startIndex);
						destination.IsBottomAlignment = bottomAlignment;
					}
					else
					{
						// scenario 1: the user has selected an item and that is why we are doing this viewchange

						// this is only valid if we are in the zoomedout view (which will never have grouping)
						sourceIndex = GetFocusedIndex() >= 0
							? GetFocusedIndex()
							: GetLastFocusedIndex();
					}
				}

				// some of the specialized codepaths have advised on an spitem already.
				if (sourceItem is null &&
					sourceIndex >= 0 &&
					sourceIndex < Items.Count)
				{
					// the lastFocusedIndex is an INT, not UINT, so -1 does not come into play
					// which is why we make absolutely sure this is a valid index we are going to.
					sourceItem = GetSemanticZoomItemAtIndex(sourceIndex);
				}

				// hopefully we found an item somehow. Fill the szl with it.
				if (sourceItem is not null)
				{
					// Set the focused item as the SourceItem
					source.Item = sourceItem;
				}

				// set the destination bounding box
				// if we had an item, this is the items container. if we are a group, this
				// is the groups container
				// some of the specialized codepaths(grouping) have advised on a container already
				container ??= GetSemanticZoomContainerAtIndex(sourceIndex) as FrameworkElement;

				// container surely can be null, because we're not guaranteed to get back an item
				// that was in view.
				// if the container is null, there is not much we can do.
				// If we are doing jumpList behavior then dont adjust co-ordinate systems.
				if (container is not null && !jumpListAlignmentBehavior)
				{
					var relativeToThis =
						container.TransformToVisual(this).TransformPoint(default);
					source.Bounds = new Rect(
						relativeToThis.X,
						relativeToThis.Y,
						container.ActualWidth,
						container.ActualHeight);
				}
				else
				{
					source.Bounds = new Rect(zoomPoint.X, zoomPoint.Y, 0, 0);
				}
			}

			// If our ItemsSource is grouped, automatically provide a destination of the
			// group's name (so that if the next level's items are just the names, then
			// we won't need to write any code to handle the mapping)
			if (CanSemanticZoomGroup() && destination.Item is null && sourceItem is not null)
			{
				object? destinationItem = null;

				// Create a SemanticZoomLocation if we don't have one yet
				// If there's no DestinationItem currently set, then will use its group
				// name (or the first item in the group if heading the other direction)
				// If we're changing from the ZoomedInView to the ZoomedOutView then we
				// need to look up a group from an item, but otherwise we'll just
				// get the first item in the group
				if (IsZoomedInView)
				{
					if (sourceIndex >= 0)
					{
						// in the normal case, spitem is set to an item, and we are going to find a group out of that now
						// however, in the case of an empty group, that is not true.
						// That codepath has already adviced on a group
						var destinationGroup = sourceItem as ICollectionViewGroup;
						if (destinationGroup is null)
						{
							// have to dig a little bit deeper
							destinationGroup = GetGroupFromItemIndex(sourceIndex);
						}

						// use the group to set the destination item
						destinationItem = destinationGroup;
					}
				}
				else
				{
					// currently in zoomedoutview, going to zoomedinview

					// Get the first item in the group as the destination
					var destinationGroup = sourceItem as ICollectionViewGroup;

					// if that did not succeed (empty group) or if we are using sticky headers, fallback to the group
					if (destinationGroup is not null)
					{
						destinationItem = destinationGroup.Group;
					}
				}

				// If we've found the corresponding destination item
				if (destinationItem is not null)
				{
					// Set the group key as the DestinationItem
					destination.Item = destinationItem;
				}
			}
		}
		finally
		{
			m_tpSZRequestingItem = null;
		}
	}

	// Determines whether or not a SemanticZoom associated with this ListViewBase has
	// grouped data as its ZoomedInView.
	private bool CanSemanticZoomGroup()
	{
		if (IsZoomedInView)
		{
			// If we are the ZoomedInView, then we just need to check if we support
			// grouping
			return IsGrouping;
		}

		// Otherwise, get the ZoomedInView and see if it supports grouping
		return SemanticZoomOwner?.ZoomedInView is ItemsControl { IsGrouping: true };
	}

	// When this ListViewBase is the inactive view and we're changing to it,
	// optionally provide the source and destination items.
	public void StartViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)
	{
	}

	// Complete the change to the other view when this ListViewBase was
	// the active view.
	public void CompleteViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)
	{
	}

	// Complete the change to make this ListViewBase the active view.
	public void CompleteViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)
	{
		var shouldTakeFocus =
			SemanticZoomOwner is not { } semanticZoomOwner ||
			semanticZoomOwner.TryGetFocusState(out _);

		// We only want to focus the destination item (or any item in the destination view) if and
		// only if the semantic zoom has focus at this time.
		if (!shouldTakeFocus)
		{
			return;
		}

		if (destination.Item is { } item)
		{
			if (IsGrouping &&
				TryGetGroupItemIndex(item, out var groupIndex) &&
				TryFindSelectableItemNearGroup(groupIndex, out var firstSelectableElementIndex))
			{
				SetSemanticZoomFocusedItem(firstSelectableElementIndex, false);
			}
			else
			{
				var index = Items.IndexOf(item);
				if (index >= 0)
				{
					SetSemanticZoomFocusedItem(index, false);
				}
			}
		}

		if (!HasSemanticZoomFocus())
		{
			SetSemanticZoomFocusedItem(GetLastFocusedIndex(), true);

			if (!HasSemanticZoomFocus())
			{
				// If still we could not focus something that makes sense, focus the  destination view itself
				// for the sake of not leaving the focus on the source view.
				Focus(m_semanticZoomCompletedFocusState);
			}
		}
	}

	// Find the first non emptyGoup of the underlying collection
	private (int index, object? item) FindFirstItem(int targetGroupIndex)
	{
		var groups = GetSemanticZoomGroups();
		if (groups is null)
		{
			return (-1, null);
		}

		var itemIndex = 0;
		for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
		{
			if (groups[groupIndex] is not ICollectionViewGroup group ||
				group.GroupItems.Count == 0)
			{
				continue;
			}

			if (groupIndex >= targetGroupIndex)
			{
				// We stop at the first non empty group after (including) targetGroupIndex
				return (itemIndex, group.GroupItems[0]);
			}

			itemIndex += group.GroupItems.Count;
		}

		return (-1, null);
	}

	private (object? item, int index, FrameworkElement? container) FindClosestSemanticZoomItem(Point point)
	{
		var itemsHost = ItemsPanelRoot;
		if (itemsHost is null)
		{
			return (null, -1, null);
		}

		var zoomPointToFirstPanel = TransformToVisual(itemsHost).TransformPoint(point);
		if (TryGetClosestElementInfo(itemsHost, zoomPointToFirstPanel, out var elementInfo))
		{
			object? sourceItem = null;
			var sourceIndex = elementInfo.childIndex;
			FrameworkElement? container = null;

			if (elementInfo.childIsHeader)
			{
				if (sourceIndex >= 0 && sourceIndex < NumberOfGroups)
				{
					sourceItem = GetGroupAt(sourceIndex);
				}
			}

			if (IsGrouping)
			{
				if (!IsModernCollectionPanel(itemsHost))
				{
					// so we now have an index that points to a group. That is not what we need, we want
					// to figure out which item was zoomed at.
					// we will have to look at this group, find the itemscontrol over there, find the panel
					// and then get a better index.
					// NOTE: this will change as we get a different grouping panel
					container = GetGroupHeaderContainer(sourceIndex);
					if (container is GroupItem groupItem &&
						groupItem.GetTemplatedItemsControl() is { } itemsControl &&
						itemsControl.ItemsPanelRoot is { } nestedItemsHost)
					{
						// we found a panel. This panel might be arbitrarily positioned inside of our moco.
						// ZoomPoint is relative to the moco, it would be good to transform that point to
						// the panels coordinate space.
						var zoomPointToNestedPanel =
							TransformToVisual(nestedItemsHost).TransformPoint(point);

						int innerSourceIndex;
						if (TryGetClosestElementInfo(nestedItemsHost, zoomPointToNestedPanel, out var innerElementInfo))
						{
							// try to use the optimized IITemLookup version
							innerSourceIndex = innerElementInfo.childIndex;
						}
						else
						{
							// fallback to a naive and slow (full iteration) implementation of all the
							// arranged children
							innerSourceIndex = GetClosestPanelChildIndexSlow(nestedItemsHost, zoomPointToNestedPanel);
						}

						// nice, now we have a sourceindex that belongs to another itemscontrol
						// what we need is an index into our original itemscollection.
						// Grouping does not support virtualization.
						if (innerSourceIndex >= 0 && innerSourceIndex < itemsControl.Items.Count)
						{
							// correct the sourceindex to point to the view
							// we are mixing up our UINT and INT's unfortunately. The IItemLookupPanel uses INT's
							// but collections use UINT. To be honest, we cannot support an INT amount of children
							// so i'm not afraid here.

							// use inner information
							sourceItem = itemsControl.Items[innerSourceIndex];

							// assign the actual sourceIndex as it is within the outer collection
							sourceIndex = GetSemanticZoomIndexFromItem(sourceItem);

							// clear out the container since we should try and find a new container
							container = null;
						}
						else if (sourceIndex >= 0 && sourceIndex < NumberOfGroups)
						{
							// fallback to the group. The sourceindex still points to the view property
							// instead of an actual item.
							sourceItem = GetGroupAt(sourceIndex);
						}
					}
				}
				else
				{
					container = elementInfo.childIsHeader
						? GetGroupHeaderContainer(sourceIndex)
						: ContainerFromIndex(sourceIndex) as FrameworkElement;
				}
			}

			if (sourceItem is null)
			{
				sourceItem = GetSemanticZoomItemAtIndex(sourceIndex);
			}

			return (sourceItem, sourceIndex, container);
		}

		// fallback to a naieve and slow (full iteration) implementation of all the
		// arranged children
		var closestIndex = GetClosestPanelChildIndexSlow(itemsHost, point);
		return GetSemanticZoomItemAtIndex(closestIndex) is { } closestItem
			? (closestItem, closestIndex, null)
			: (null, closestIndex, null);
	}

	private bool TryGetClosestElementInfo(
		Panel panel,
		Point position,
		out (int childIndex, bool childIsHeader) elementInfo)
	{
		if (panel is StackPanel stackPanel)
		{
			stackPanel.GetClosestElementInfo(position, out var stackPanelElementInfo);
			elementInfo = (stackPanelElementInfo.Item1, stackPanelElementInfo.Item2);
			return true;
		}

		if (IsModernCollectionPanel(panel))
		{
			var childIndex = GetClosestPanelChildIndexSlow(panel, position);
			if (childIndex < 0 || childIndex >= panel.Children.Count)
			{
				elementInfo = (-1, false);
				return true;
			}

			var child = panel.Children[childIndex];
			var itemsOwner = ItemsControl.GetItemsOwner(panel);
			if (itemsOwner is ListViewBase listViewBase &&
				listViewBase.TryGetGroupIndexFromHeader(child, out var groupIndex))
			{
				elementInfo = (groupIndex, true);
				return true;
			}

			elementInfo = (itemsOwner?.IndexFromContainer(child) ?? -1, false);
			return true;
		}

		elementInfo = default;
		return false;
	}

	private static bool IsModernCollectionPanel(Panel panel) =>
		panel is ItemsStackPanel or ItemsWrapGrid;

	private static int GetClosestPanelChildIndexSlow(Panel panel, Point location)
	{
		var shortestDistance = double.MaxValue;
		var closestIndex = -1;

		// naive implementation that knows nothing about internals, but just looks at where a
		// child has been arranged. It will cost N.
		// it does not involve transforms.

		// the algorithm is simply to find the closest point on the bounding rect of the item
		// to the passed in point.
		// 1. see if there is a straight line on x-axis
		// 2. see if there is a straight line on y-axis
		// both: we can short-circuit the algorithm since we are within the item.
		//       (currently I do not care about multiple items taking up the same space).
		// single: that is the shortest distance
		// none  : find the closest edge and calculate the distance
		for (var index = 0; index < panel.Children.Count; index++)
		{
			var child = panel.Children[index];
			var x1 = child.ActualOffset.X;
			var y1 = child.ActualOffset.Y;
			var x2 = x1 + child.ActualSize.X;
			var y2 = y1 + child.ActualSize.Y;
			var distance = double.MaxValue;

			// short circuit - point is located within boundaries
			if (x1 <= location.X && location.X <= x2 &&
				y1 <= location.Y && location.Y <= y2)
			{
				// we have found a great point
				distance = 0;
			}
			else
			{
				// straight line up/down from point would intersect the x-axis of the bounds
				if (x1 <= location.X && location.X <= x2)
				{
					// so the shortest distance would be the distance in the y direction
					distance = Math.Min(Math.Abs(location.Y - y1), Math.Abs(location.Y - y2));
				}
				else if (y1 <= location.Y && location.Y <= y2)
				{
					// so the shortest distance would be the distance in the x direction
					distance = Math.Min(Math.Abs(location.X - x1), Math.Abs(location.X - x2));
				}
				else if ((Math.Abs(location.X - x1) < shortestDistance ||
					Math.Abs(location.X - x2) < shortestDistance) &&
					(Math.Abs(location.Y - y1) < shortestDistance ||
					Math.Abs(location.Y - y2) < shortestDistance))
				{
					// need to find the point on the bounds that is closest and then
					// calculate the distance.

					// the if-condition above makes sure we only undertake this calculation if we have a chance of being smaller
					var a = Math.Min(Math.Abs(location.X - x1), Math.Abs(location.X - x2));
					var b = Math.Min(Math.Abs(location.Y - y1), Math.Abs(location.Y - y2));
					distance = Math.Sqrt(a * a + b * b);
				}
			}

			// update
			if (distance < shortestDistance)
			{
				shortestDistance = distance;
				closestIndex = index;
				if (distance == 0)
				{
					break;
				}
			}
		}

		return closestIndex;
	}

	private object? ResolveSemanticZoomItem(object? item)
	{
		if (item is null)
		{
			return null;
		}

		if (IsZoomedInView &&
			TryGetGroupItemIndex(item, out var groupIndex) &&
			GetGroupAt(groupIndex) is { GroupItems.Count: > 0 } group)
		{
			return group.GroupItems[0];
		}

		return GetSemanticZoomIndexFromItem(item) >= 0 ? item : null;
	}

	private ICollectionViewGroup? GetGroupFromItemIndex(int itemIndex)
	{
		var indexPath = GetIndexPathFromIndex(itemIndex);
		return indexPath is { } path &&
			path.Section >= 0 &&
			path.Section < NumberOfDisplayGroups
				? GetGroupAtDisplaySection(path.Section)
				: null;
	}

	private bool TryGetGroupItemIndex(object item, out int groupIndex)
	{
		groupIndex = -1;
		var groups = GetSemanticZoomGroups();
		if (groups is null)
		{
			return false;
		}

		for (var index = 0; index < groups.Count; index++)
		{
			if (groups[index] is ICollectionViewGroup candidate &&
				(ReferenceEquals(candidate, item) || Equals(candidate.Group, item)))
			{
				groupIndex = index;
				return true;
			}
		}

		return false;
	}

	private global::Windows.Foundation.Collections.IObservableVector<object>? GetSemanticZoomGroups() =>
		(UnwrapItemsSource() as ICollectionView)?.CollectionGroups;

	internal FrameworkElement? GetGroupHeaderContainer(int groupIndex)
	{
		var group = groupIndex >= 0 && groupIndex < NumberOfGroups
			? GetGroupAt(groupIndex)
			: null;
		return GetItemsPanelChildren()
			.OfType<FrameworkElement>()
			.FirstOrDefault(element =>
				ReferenceEquals(element.DataContext, group) ||
				Equals(element.DataContext, group?.Group));
	}

	private bool TryGetGroupIndexFromHeader(object header, out int groupIndex)
	{
		groupIndex = -1;
		if (header is not DependencyObject dependencyObject)
		{
			return false;
		}

		if (header is FrameworkElement headerElement &&
			GetSemanticZoomGroups() is { } groups)
		{
			for (var index = 0; index < groups.Count; index++)
			{
				if (groups[index] is ICollectionViewGroup group &&
					(ReferenceEquals(headerElement.DataContext, group) ||
					 ReferenceEquals(headerElement.DataContext, group.Group) ||
					 Equals(headerElement.DataContext, group.Group)))
				{
					groupIndex = index;
					return true;
				}
			}
		}

		for (var index = 0; index < NumberOfGroups; index++)
		{
			var groupHeaderContainer = GetGroupHeaderContainer(index);
			for (var current = dependencyObject;
				current is not null && !ReferenceEquals(current, this);
				current = VisualTreeHelper.GetParent(current))
			{
				if (ReferenceEquals(groupHeaderContainer, current))
				{
					groupIndex = index;
					return true;
				}
			}
		}

		return false;
	}

	private bool TryFindSelectableItemNearGroup(int groupIndex, out int itemIndex)
	{
		// Our destination is a group.
		itemIndex = -1;

		for (var index = groupIndex; index < NumberOfGroups; index++)
		{
			if (TryFindFirstSelectableItemInGroup(index, out itemIndex))
			{
				return true;
			}
		}

		// Search the previous groups
		for (var index = groupIndex - 1; index >= 0; index--)
		{
			if (TryFindFirstSelectableItemInGroup(index, out itemIndex))
			{
				return true;
			}
		}

		return false;
	}

	// Gets the first selectable item within the given group.
	private bool TryFindFirstSelectableItemInGroup(int groupIndex, out int itemIndex)
	{
		itemIndex = -1;
		if (groupIndex < 0 || groupIndex >= NumberOfGroups)
		{
			return false;
		}

		var groupStartIndex = 0;
		for (var index = 0; index < groupIndex; index++)
		{
			groupStartIndex += GetGroupCount(index);
		}

		var groupEndIndex = groupStartIndex + GetGroupCount(groupIndex);
		for (var currentItemIndex = groupStartIndex;
			currentItemIndex < Items.Count && currentItemIndex < groupEndIndex;
			currentItemIndex++)
		{
			var item = GetSemanticZoomItemAtIndex(currentItemIndex);
			if (!IsSelectableHelper(item))
			{
				continue;
			}

			var container = GetSemanticZoomContainerAtIndex(currentItemIndex);
			if (container is null || IsSelectableHelper(container))
			{
				itemIndex = currentItemIndex;
				return true;
			}
		}

		return false;
	}

	private void SetSemanticZoomFocusedItem(int index, bool shouldScrollIntoView)
	{
		if (index < 0 || index >= Items.Count)
		{
			return;
		}

		var item = GetSemanticZoomItemAtIndex(index);
		if (item is null)
		{
			return;
		}

		if (shouldScrollIntoView)
		{
			ScrollIntoView(item);
			UpdateLayout();
		}

		if (IsGrouping &&
			GetSemanticZoomContainerAtIndex(index) is SelectorItem container)
		{
			container.FocusSelfOrChild(
				m_semanticZoomCompletedFocusState,
				animateIfBringIntoView: false,
				out _,
				FocusNavigationDirection.None,
				Uno.UI.Xaml.Input.InputActivationBehavior.RequestActivation);
			FocusedIndexContainerItem = (index, container, item);
			SetLastFocusedIndex(index);
			SetFocusedIndex(index);
			return;
		}

		SetFocusedItem(
			index,
			shouldScrollIntoView: false,
			forceFocus: true,
			focusState: m_semanticZoomCompletedFocusState,
			animateIfBringIntoView: false);
	}

	private object? GetSemanticZoomItemAtIndex(int index)
	{
		if (index < 0 || index >= Items.Count)
		{
			return null;
		}

		if (IsGrouping &&
			GetIndexPathFromIndex(index) is { } indexPath)
		{
			return GetDisplayItemFromIndexPath(indexPath);
		}

		return Items[index];
	}

	private int GetSemanticZoomIndexFromItem(object item)
	{
		if (IsGrouping)
		{
			var indexPath = GetIndexPathFromItem(item);
			if (indexPath.Section >= 0 && indexPath.Row >= 0)
			{
				return GetIndexFromIndexPath(indexPath);
			}
		}

		return Items.IndexOf(item);
	}

	private DependencyObject? GetSemanticZoomContainerAtIndex(int index)
	{
		if (IsGrouping &&
			!IsModernCollectionPanel(ItemsPanelRoot) &&
			GetIndexPathFromIndex(index) is { } indexPath &&
			GetGroupHeaderContainer(indexPath.Section) is GroupItem groupItem &&
			groupItem.GetTemplatedItemsControl() is { } itemsControl)
		{
			return itemsControl.ContainerFromIndex(indexPath.Row);
		}

		return ContainerFromIndex(index);
	}

	private bool HasSemanticZoomFocus()
	{
		if (XamlRoot is null ||
			FocusManager.GetFocusedElement(XamlRoot) is not DependencyObject focusedElement)
		{
			return false;
		}

		for (var current = focusedElement; current is not null; current = VisualTreeHelper.GetParent(current))
		{
			if (ReferenceEquals(current, this))
			{
				return true;
			}
		}

		return false;
	}
}
