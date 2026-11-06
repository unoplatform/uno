// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference StackingLayoutStrategyImpl.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

// New stacking strategy to mimick stackpanel

#nullable enable

using System;
using DirectUI;
using Windows.Foundation;

#pragma warning disable CS0219 // Upstream assigns referenceIsHeader without reading it.

namespace Microsoft.UI.Xaml.Controls;

internal partial class StackingLayoutStrategyImpl
{
	public StackingLayoutStrategyImpl()
		: base(true /* useFullWidthHeaders */, false /* isWrapping */)
	{
		m_headerSize = default;
		m_headerSizeSet = false;
		m_containerSizesTotal = 0;
		m_headerSizesTotal = 0;
		m_containerSizesStoredTotal = 0;
		m_headerSizesStoredTotal = 0;
		m_extentInNonVirtualizingDirection = 0.0f;
	}

	public void Initialize()
	{
		m_containerSizes.Clear();
		m_containerSizes.AddRange(new float[c_totalSizesForAveraging]);
		m_headerSizes.Clear();
		m_headerSizes.AddRange(new float[c_totalSizesForAveraging]);
	}

	#region Layout related methods

	// returns the size we should use to measure a container or header with
	// itemIndex - indicates an index of valid item or -1 for general, non-special items
	public Size GetElementMeasureSize(
		ElementType elementType,
		int elementIndex,
		Rect windowConstraint)
	{
		Size result = new Size(float.PositiveInfinity, float.PositiveInfinity);

		// try to limit to the constraint
		SetSizeInNonVirtualizingDirection(ref result, SizeFromRectInNonVirtualizingDirection(windowConstraint));

		// account for group padding in measure size, reduce the size in non virtualizing direction by the amount of group padding
		float groupPaddingInNonVirtualizingDirection = SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()) + SizeInNonVirtualizingDirection(GetGroupPaddingAtEnd());
		float measureSizeInNonVirtualizingDirection = SizeInNonVirtualizingDirection(result) - groupPaddingInNonVirtualizingDirection;
		SetSizeInNonVirtualizingDirection(ref result, Math.Max(0.0f, measureSizeInNonVirtualizingDirection));

		// the special groupindex should get the available size
		// we only limit if we have a headersize set
		// we only need to limit in parallel layouts: inline scenarios need to have a different logic
		if (GroupHeaderStrategy == GroupHeaderStrategy.Parallel &&
			elementType == ElementType.GroupHeader &&
			elementIndex != c_specialGroupIndex &&
			m_headerSizeSet)
		{
			// in parallel groups, the header size influences the space that we have to measure with
			// this is a hint to the header it will only have this amount of space
			SetSizeInNonVirtualizingDirection(ref result, SizeInNonVirtualizingDirection(m_headerSize));
		}

		return result;
	}

	public Rect GetElementBounds(
		ElementType elementType,
		int elementIndex,
		Size containerDesiredSize,
		LayoutReference referenceInformation,
		Rect windowConstraint)
	{
		Rect pReturnValue;

		if (elementType == ElementType.ItemContainer)
		{
			int indexInGroup = elementIndex;
			if (IsGrouping)
			{
				GetLayoutDataInfoProviderNoRef.GetGroupInformationFromItemIndex(
					elementIndex,
					out _ /* pIndexOfGroup */,
					out indexInGroup,
					out _ /* pItemCountInGroup */);
			}
			pReturnValue = GetContainerBounds(elementIndex, indexInGroup, containerDesiredSize, referenceInformation, windowConstraint);
		}
		else
		{
			pReturnValue = GetHeaderBounds(elementIndex, containerDesiredSize, referenceInformation, windowConstraint);
		}

		m_extentInNonVirtualizingDirection = Math.Max(
			m_extentInNonVirtualizingDirection,
			PointFromRectInNonVirtualizingDirection(pReturnValue) + SizeFromRectInNonVirtualizingDirection(pReturnValue));

		return pReturnValue;
	}

	public Rect GetElementArrangeBounds(
		ElementType elementType,
		int elementIndex,
		Rect containerBounds,
		Rect windowConstraint,
		Size finalSize)
	{
		Rect result = containerBounds;
		if (elementType == ElementType.ItemContainer)
		{
			// we will give the container what it requested if bigger than the constraint, and let the clipping occur by the ScrollViewer
			// we will give the container the constraint of the window (the viewport really) so that it can be laid out inside of the
			// viewport. Basically this means that a bigger container does not influence the alignment of smaller elements.
			// we do not use the finalsize, because that will represent the largest element in the viewport.
			SetSizeFromRectInNonVirtualizingDirection(ref result, Math.Max(SizeFromRectInNonVirtualizingDirection(containerBounds), SizeFromRectInNonVirtualizingDirection(windowConstraint)));

			// unfortunate, but incorrectly configured panels (for instance, panel is set to orient horizontally, where ScrollViewer is set to enable scrolling vertically
			// will potentially have infinity here. Also, the listview itself might have been inside of ScrollViewer that allowed infinite in this direction
			SetSizeFromRectInNonVirtualizingDirection(ref result, Math.Min(SizeFromRectInNonVirtualizingDirection(result), SizeInNonVirtualizingDirection(finalSize)));

			if (m_headerSizeSet && GroupHeaderStrategy == GroupHeaderStrategy.Parallel)
			{
				SetSizeFromRectInNonVirtualizingDirection(ref result, SizeFromRectInNonVirtualizingDirection(result) - SizeInNonVirtualizingDirection(m_headerSize));
			}

			SetSizeFromRectInNonVirtualizingDirection(ref result, SizeFromRectInNonVirtualizingDirection(result) - (SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()) + SizeInNonVirtualizingDirection(GetGroupPaddingAtEnd())));
		}
		else
		{
			if (GroupHeaderStrategy == GroupHeaderStrategy.Inline)
			{
				SetSizeFromRectInNonVirtualizingDirection(ref result, SizeFromRectInNonVirtualizingDirection(windowConstraint));

				// unfortunate, but incorrectly configured panels (for instance, panel is set to orient horizontally, where ScrollViewer is set to enable scrolling vertically
				// will potentially have infinity here. Also, the listview itself might have been inside of ScrollViewer that allowed infinite in this direction
				SetSizeFromRectInNonVirtualizingDirection(ref result, Math.Min(SizeFromRectInNonVirtualizingDirection(result), SizeInNonVirtualizingDirection(finalSize)));

				// account for group padding, reduce the size in non virtualizing direction by the amount of group padding
				float groupPaddingInNonVirtualizingDirection = SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()) + SizeInNonVirtualizingDirection(GetGroupPaddingAtEnd());
				float arrangeSizeInNonVirtualizingDirection = SizeFromRectInNonVirtualizingDirection(result) - groupPaddingInNonVirtualizingDirection;
				SetSizeFromRectInNonVirtualizingDirection(ref result, Math.Max(0.0f, arrangeSizeInNonVirtualizingDirection));
			}
			else
			{
				if (m_headerSizeSet)
				{
					SetSizeFromRectInNonVirtualizingDirection(ref result, Math.Max(SizeFromRectInNonVirtualizingDirection(result), SizeInNonVirtualizingDirection(m_headerSize)));
				}
			}
		}
		return result;
	}

	public bool ShouldContinueFillingUpSpace(
		ElementType elementType,
		int elementIndex,
		LayoutReference referenceInformation,
		Rect windowToFill)
	{
		bool shouldContinue = false;
		bool requestingHeader = (elementType == ElementType.GroupHeader);

		if (referenceInformation.RelativeLocation == ReferenceIdentity.Myself)
		{
			// always do yourself
			shouldContinue = true;
		}
		else if (referenceInformation.RelativeLocation == ReferenceIdentity.BeforeMe)
		{
			// is there room on the right?
			if (referenceInformation.ReferenceIsHeader &&
				GroupHeaderStrategy.Parallel == GroupHeaderStrategy &&
				!requestingHeader)
			{
				// If we are going from a header to a container, and the header is parallel, then we can generate a container
				// if only the first edge of the header is in view
				shouldContinue =
					PointFromRectInVirtualizingDirection(windowToFill) + SizeFromRectInVirtualizingDirection(windowToFill)
					>= PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);

			}
			else
			{
				// with an inline header, or following an item, we shouldn't continue until
				// the entire reference is in the window
				shouldContinue =
					PointFromRectInVirtualizingDirection(windowToFill) + SizeFromRectInVirtualizingDirection(windowToFill)
					>= PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
			}
		}
		else
		{
			// is there room to the left?
			shouldContinue = PointFromRectInVirtualizingDirection(windowToFill) <= PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
		}

		return shouldContinue;
	}

	public Point GetPositionOfFirstElement()
	{
		Size initialPadding = GetGroupPaddingAtStart();
		return new Point(initialPadding.Width, initialPadding.Height);
	}

	#endregion

	#region Estimation and virtualization related methods.

	public void EstimateElementIndex(
		ElementType elementType,
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window,
		out Rect pTargetRect,
		out int pReturnValue)
	{
		int result = -1;
		if (elementType == ElementType.ItemContainer)
		{
			EstimateItemIndexFromWindow(
				headerReference,
				containerReference,
				window,
				out pTargetRect,
				out result);
		}
		else
		{
			EstimateGroupIndexFromWindow(
				headerReference,
				containerReference,
				window,
				out pTargetRect,
				out result);
		}

		pReturnValue = result;
	}

	public Rect EstimateElementBounds(
		ElementType elementType,
		int elementIndex,
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window)
	{
		Rect result = default;

		if (elementType == ElementType.ItemContainer)
		{
			EstimateContainerLocation(
				elementIndex,
				headerReference,
				containerReference,
				window,
				out result);
		}
		else
		{
			EstimateHeaderLocation(
				elementIndex,
				headerReference,
				containerReference,
				window,
				out result);
		}

		return result;
	}

	public Size EstimatePanelExtent(
		EstimationReference lastHeaderReference,
		EstimationReference lastContainerReference,
		Rect windowConstraint)
	{
		Size result = default;

		if (IsGrouping)
		{
			EstimateGroupedExtent(
				lastHeaderReference,
				lastContainerReference,
				windowConstraint,
				out result);
		}
		else
		{
			EstimateNonGroupedExtent(
				lastContainerReference,
				windowConstraint,
				out result);
		}

		return result;
	}

	#endregion

	#region IItemLookupPanel related

	// Estimates the index or the insertion index closest to the given point.
	public void EstimateIndexFromPoint(
		bool requestingInsertionIndex,
		Point point,
		EstimationReference reference,
		Rect windowConstraint,
		out IndexSearchHint pSearchHint,
		out ElementType pElementType,
		out int pElementIndex)
	{
		pElementIndex = reference.ElementIndex;
		Rect referenceItemRect = reference.ElementBounds;
		pElementType = ElementType.ItemContainer;

		int totalItems;
		totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

		// compare the point to the rect and either return exact, above or below
		// notice how I only care about the virtualizing direction.
		if (PointInVirtualizingDirection(point) < PointFromRectInVirtualizingDirection(referenceItemRect) &&
			pElementIndex > 0)
		{
			pSearchHint = IndexSearchHint.SearchBackwards;
		}
		else if
			((PointInVirtualizingDirection(point) > PointFromRectInVirtualizingDirection(referenceItemRect) + SizeFromRectInVirtualizingDirection(referenceItemRect)) &&
			(totalItems - 1 > pElementIndex))
		{
			// the point is further along than the rect, so indicate that to find it, you should search backwards
			pSearchHint = IndexSearchHint.SearchForwards;
		}
		else
		{
			pSearchHint = IndexSearchHint.Exact;
		}

		// if exact, just follow below calculation if we are returning the insertion index.
		if (requestingInsertionIndex &&
			pSearchHint == IndexSearchHint.Exact)
		{
			// If we're below the midpoint of the container under the point, we actually want to insert at the next index.
			if (PointInVirtualizingDirection(point) - PointFromRectInVirtualizingDirection(referenceItemRect)
				>= (SizeFromRectInVirtualizingDirection(referenceItemRect) / 2.0f))
			{
				++pElementIndex;
			}
		}
	}

	// Based on current element's index/type and action, return the next element index/type.
	public void GetTargetIndexFromNavigationAction(
		ElementType elementType,
		int elementIndex,
		KeyNavigationAction action,
		Rect windowConstraint,
		out ElementType pTargetElementType,
		out int pTargetElementIndex)
	{
		int targetIndex = elementIndex;
		var targetType = elementType;

		if (action != KeyNavigationAction.Left &&
			action != KeyNavigationAction.Right &&
			action != KeyNavigationAction.Up &&
			action != KeyNavigationAction.Down)
		{
			throw new ArgumentException("E_INVALIDARG", nameof(action));
		}

		int step = 0;
		int targetIndexMax = 0;

		if (elementType == ElementType.ItemContainer)
		{
			targetIndexMax = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(IsGrouping);

			targetIndexMax = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();
		}

		global::System.Diagnostics.Debug.Assert(0 <= elementIndex && elementIndex < targetIndexMax);

		// The action is along layout orientation, therefore handle it.
		if ((VirtualizationDirection == Orientation.Horizontal && (action == KeyNavigationAction.Left || action == KeyNavigationAction.Right)) ||
			(VirtualizationDirection == Orientation.Vertical && (action == KeyNavigationAction.Up || action == KeyNavigationAction.Down)))
		{
			step = (action == KeyNavigationAction.Left || action == KeyNavigationAction.Up) ? -1 : 1;
		}

		targetIndex = Math.Min(Math.Max(elementIndex + step, 0), targetIndexMax - 1);

		// Evaluate whether we need to change the element type & index.
		if (IsGrouping)
		{
			if (elementType == ElementType.ItemContainer)
			{
				int targetGroupHeaderIndex;
				TryGetTargetHeaderIndexWithItemNavigation(elementIndex, targetIndex, step, out targetGroupHeaderIndex);
				if (targetGroupHeaderIndex != -1)
				{
					targetIndex = targetGroupHeaderIndex;
					targetType = ElementType.GroupHeader;
				}
			}
			else
			{
				int targetItemIndex = -1;
				TryGetTargetItemIndexWithHeaderNavigation(elementIndex, targetIndex, step, out targetItemIndex);
				if (targetItemIndex != -1)
				{
					targetIndex = targetItemIndex;
					targetType = ElementType.ItemContainer;
				}
			}
		}

		pTargetElementType = targetType;
		pTargetElementIndex = targetIndex;
	}

	private void TryGetTargetHeaderIndexWithItemNavigation(
		int currentItemIndex,
		int targetItemIndex,
		int step,
		out int targetGroupHeaderIndex)
	{
		global::System.Diagnostics.Debug.Assert(IsGrouping);

		targetGroupHeaderIndex = -1;

		if (step != 0)
		{
			int currIndexOfGroup = 0;
			int currIndexInsideOfGroup = 0;
			int currItemCountInGroup = 0;
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromItemIndex(currentItemIndex, out currIndexOfGroup, out currIndexInsideOfGroup, out currItemCountInGroup);

			if (currentItemIndex == targetItemIndex)
			{
				// Handle the special-case of going up/back from the first line in the first group
				// with items.
				if (currentItemIndex == 0 && step < 0)
				{
					targetGroupHeaderIndex = currIndexOfGroup;
				}
				else
				{
					int totalGroups = 0;
					totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

					int totalItems = 0;
					totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

					// Handle the special-case of going down from the last line in the last group with items.
					bool isInLastGroupWithItems = (currentItemIndex == (totalItems - currItemCountInGroup + currIndexInsideOfGroup));
					if (isInLastGroupWithItems && step > 0 && currIndexOfGroup < (totalGroups - 1))
					{
						targetGroupHeaderIndex = currIndexOfGroup + 1;
					}
				}
			}
			else
			{
				// Evaluate whether the new target item index would cross a group boundary
				// when compared to the previous item index.
				int targetIndexOfGroup = 0;
				int targetIndexInsideOfGroup = 0;
				int targetItemCountInGroup = 0;
				GetLayoutDataInfoProviderNoRef.GetGroupInformationFromItemIndex(targetItemIndex, out targetIndexOfGroup, out targetIndexInsideOfGroup, out targetItemCountInGroup);

				if (currIndexOfGroup != targetIndexOfGroup)
				{
					// If advancing forward, don't just set the new target group index to targetIndexOfGroup,
					// instead just increment the value of currIndexOfGroup.  This is so we don't skip
					// group headers that have no items.
					targetGroupHeaderIndex = (step < 0 ? currIndexOfGroup : currIndexOfGroup + 1);
				}
			}
		}
	}

	private void TryGetTargetItemIndexWithHeaderNavigation(
		int currentGroupIndex,
		int targetGroupIndex,
		int step,
		out int targetItemIndex)
	{
		global::System.Diagnostics.Debug.Assert(IsGrouping);

		targetItemIndex = -1;

		if (step != 0)
		{
			int totalItems = 0;
			totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

			int currGroupStartItemIndex = 0;
			int currGroupItemCountInGroup = 0;
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(currentGroupIndex, out currGroupStartItemIndex, out currGroupItemCountInGroup);

			bool isInLastGroupWithItems = currGroupItemCountInGroup > 0 && (totalItems == (currGroupStartItemIndex + currGroupItemCountInGroup));

			if (currentGroupIndex == targetGroupIndex && isInLastGroupWithItems && step > 0)
			{
				// Handle the special-case of going down/forward from the group header for the last group
				// with items.
				targetItemIndex = currGroupStartItemIndex;
			}
			else if (currentGroupIndex != targetGroupIndex)
			{
				int targetGroupStartItemIndex = 0;
				int targetGroupItemCountInGroup = 0;
				GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(targetGroupIndex, out targetGroupStartItemIndex, out targetGroupItemCountInGroup);

				// Change the targetIndex to point to an item index if jumping between groups would
				// jump us over some items.
				if (targetGroupIndex < currentGroupIndex && targetGroupItemCountInGroup > 0)
				{
					targetItemIndex = targetGroupStartItemIndex + targetGroupItemCountInGroup - 1;
				}
				else if (targetGroupIndex > currentGroupIndex && currGroupItemCountInGroup > 0)
				{
					targetItemIndex = currGroupStartItemIndex;
				}
			}
		}
	}

	// Determines whether or not the given item index
	// is a layout boundary.
	public void IsIndexLayoutBoundary(
		ElementType elementType,
		int elementIndex,
		Rect windowConstraint,
		out bool pIsLeftBoundary,
		out bool pIsTopBoundary,
		out bool pIsRightBoundary,
		out bool pIsBottomBoundary)
	{
		// We only support non-grouped scenarios here, since we never use this method in a grouped scenario.
		global::System.Diagnostics.Debug.Assert(elementType == ElementType.ItemContainer);

		int totalItems;
		totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

		bool isHorizontal = (VirtualizationDirection == Orientation.Horizontal);
		if (isHorizontal)
		{
			pIsLeftBoundary = (elementIndex == 0);
			pIsBottomBoundary = true;
			pIsTopBoundary = true;
			pIsRightBoundary = (elementIndex == totalItems - 1);
		}
		else
		{
			pIsLeftBoundary = true;
			pIsBottomBoundary = (elementIndex == totalItems - 1);
			pIsTopBoundary = (elementIndex == 0);
			pIsRightBoundary = true;
		}
	}

	#endregion

	#region Snap points related

	// Stacking layout doesn't have regular snap points
	public bool GetRegularSnapPoints(
		out float pNearOffset,
		out float pFarOffset,
		out float pSpacing)
	{
		pNearOffset = 0.0f;
		pFarOffset = 0.0f;
		pSpacing = 0.0f;
		return false;
	}

	public bool HasIrregularSnapPoints(
		ElementType elementType)
	{
		bool result;
		if (elementType == ElementType.ItemContainer)
		{
			// Always give snap points for items
			result = true;
		}
		else
		{
			// ISP returns header snap points if we are grouping and we are drawing inline headers
			result = (GroupHeaderStrategy == GroupHeaderStrategy.Inline);
		}

		return result;
	}

	#endregion

	public Rect GetElementTransitionsBounds(
		ElementType elementType,
		int elementIndex,
		Rect windowConstraint)
	{
		return default;
	}


	private float GetDistanceBetweenGroups()
	{
		Thickness groupPadding = GroupPadding;
		return VirtualizationDirection == Orientation.Vertical ?
			(float)(groupPadding.Bottom + groupPadding.Top)
			: (float)(groupPadding.Left + groupPadding.Right);

	}

	private float GetVirtualizedGroupExtent(float itemsInGroup, float headerExtent)
	{
		float result = GetAverageVirtualizedExtentOfItems(itemsInGroup);

		switch (GroupHeaderStrategy)
		{
			case GroupHeaderStrategy.Parallel:
				// Constrain it by the average header size
				result = Math.Max(result, headerExtent);
				break;

			case GroupHeaderStrategy.Inline:
				// Add the average header size
				result += headerExtent;
				break;

			default:
				global::System.Diagnostics.Debug.Assert(false);
				break;
		}

		return result + GetDistanceBetweenGroups();
	}

	// todo: validate that this needs the header size as well, seems like it from usage
	private float GetAverageVirtualizedGroupExtent(float averageItemsPerGroup)
	{
		float averageHeaderSize = GetAverageHeaderSize();

		return GetVirtualizedGroupExtent(averageItemsPerGroup, averageHeaderSize);
	}

	private float GetAverageVirtualizedExtentOfItems(float itemCount)
	{
		if (itemCount > 0)
		{
			float averageItemSize = GetAverageContainerSize();
			return averageItemSize * itemCount;
		}

		return 0.0f;
	}

	public void RegisterSpecialContainerSize(int itemIndex, Size containerDesiredSize)
	{
		global::System.Diagnostics.Debug.Assert(itemIndex == c_specialItemIndex);
		RegisterSize(itemIndex, false /* isHeader */, SizeInVirtualizingDirection(containerDesiredSize));
	}
	public void RegisterSpecialHeaderSize(int groupIndex, Size headerDesiredSize)
	{
		global::System.Diagnostics.Debug.Assert(groupIndex == c_specialGroupIndex);
		m_headerSize = headerDesiredSize;
		m_headerSizeSet = true;
		RegisterSize(groupIndex, true /* isHeader */, SizeInVirtualizingDirection(headerDesiredSize));
	}

	// registers a size so that we can be better at averaging
	private void RegisterSize(int index, bool isHeader, float newSize)
	{
		// if we scrolled past 10.000, let's start looking at these new ones.
		// in general we do this mostly so that we can deal with a panel that starts of scrolled to the right
		// and would not be registering any sizes at all if it were not for
		int moddedIndex = index % c_totalSizesForAveraging;

		if (isHeader)
		{
			float oldSize = m_headerSizes[moddedIndex];
			m_headerSizes[moddedIndex] = newSize;
			m_headerSizesTotal += newSize - oldSize;

			if (DoubleUtil.AreClose(0, oldSize) && !DoubleUtil.AreClose(0, newSize))
			{
				++m_headerSizesStoredTotal;
			}
			if (!DoubleUtil.AreClose(0, oldSize) && DoubleUtil.AreClose(0, newSize))
			{
				--m_headerSizesStoredTotal;
			}
		}
		else
		{
			float oldSize = m_containerSizes[moddedIndex];
			m_containerSizes[moddedIndex] = newSize;
			m_containerSizesTotal += newSize - oldSize;

			if (DoubleUtil.AreClose(0, oldSize) && !DoubleUtil.AreClose(0, newSize))
			{
				++m_containerSizesStoredTotal;
			}
			if (!DoubleUtil.AreClose(0, oldSize) && DoubleUtil.AreClose(0, newSize))
			{
				--m_containerSizesStoredTotal;
			}
		}
	}

	public float GetAverageHeaderSize()
	{
		// if we have just 1 group header and it is collapsed there will be no registered size for it.
		return (float)(m_headerSizesStoredTotal > 0 ? m_headerSizesTotal / m_headerSizesStoredTotal : 10.0);
	}

	public float GetAverageContainerSize()
	{
		// if we have just 1 container and it is collapsed there will be no registered size for it.
		return (float)(m_containerSizesStoredTotal > 0 ? m_containerSizesTotal / m_containerSizesStoredTotal : 10.0);
	}

	//
	// Private methods
	//

	private Rect GetContainerBounds(
		int indexInItems,
		int indexInGroup,
		Size containerDesiredSize,
		LayoutReference referenceInformation,
		Rect windowConstraint)
	{
		Rect bounds = default;

		// Required sizes set
		global::System.Diagnostics.Debug.Assert(!IsGrouping || (IsGrouping && m_headerSizeSet));

		// register the size of this item for better averaging
		RegisterSize(indexInItems, false /* isHeader */, SizeInVirtualizingDirection(containerDesiredSize));

		if (referenceInformation.ReferenceIsHeader)
		{
			// input
			float distanceBetweenGroups = GetDistanceBetweenGroups();

			// mismatch
			global::System.Diagnostics.Debug.Assert(referenceInformation.RelativeLocation != ReferenceIdentity.Myself);

			if (referenceInformation.RelativeLocation == ReferenceIdentity.BeforeMe)
			{
				global::System.Diagnostics.Debug.Assert(indexInGroup == 0); // if not, this is an invalid calculation

				SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
				if (GroupHeaderStrategy.Inline == GroupHeaderStrategy)
				{
					// Add the size of the inline header
					SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(bounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
				}
			}
			else
			{
				// I'm going to be the last container in some new header that we will also be creating
				SetPointFromRectInVirtualizingDirection(ref bounds,
					PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) - SizeInVirtualizingDirection(containerDesiredSize) - distanceBetweenGroups);
			}
		}
		else
		{
			// we got a sibling container
			if (referenceInformation.RelativeLocation == ReferenceIdentity.BeforeMe)
			{
				SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
			}
			else if (referenceInformation.RelativeLocation == ReferenceIdentity.AfterMe)
			{
				SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) - SizeInVirtualizingDirection(containerDesiredSize));
			}
			else
			{
				SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
			}
		}

		SetPointFromRectInNonVirtualizingDirection(ref bounds, SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()));
		if (GroupHeaderStrategy.Parallel == GroupHeaderStrategy)
		{
			// If we have a parallel header, move out of its way
			SetPointFromRectInNonVirtualizingDirection(ref bounds, PointFromRectInNonVirtualizingDirection(bounds) + SizeInNonVirtualizingDirection(m_headerSize));
		}

		SetSizeFromRectInVirtualizingDirection(ref bounds, SizeInVirtualizingDirection(containerDesiredSize));
		SetSizeFromRectInNonVirtualizingDirection(ref bounds, SizeInNonVirtualizingDirection(containerDesiredSize));

		return bounds;
	}

	private Rect GetHeaderBounds(
		int groupIndex,
		Size headerDesiredSize,
		LayoutReference referenceInformation,
		Rect windowConstraint)
	{
		Rect bounds = default;

		float distanceBetweenGroups = GetDistanceBetweenGroups();

		// Required sizes set
		global::System.Diagnostics.Debug.Assert(IsGrouping && m_headerSizeSet);

		// register the size of this item for better averaging
		RegisterSize(groupIndex, true /* isHeader */, SizeInVirtualizingDirection(headerDesiredSize));

		if (referenceInformation.ReferenceIsHeader)
		{
			if (referenceInformation.RelativeLocation == ReferenceIdentity.Myself)
			{
				// we did not have a correct location for this header. We will probably have estimated something
				SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
			}
			else
			{
				SetPointFromRectInVirtualizingDirection(ref bounds, referenceInformation.RelativeLocation == ReferenceIdentity.BeforeMe ?
					PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + distanceBetweenGroups
					: PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) - SizeInVirtualizingDirection(headerDesiredSize) - distanceBetweenGroups);
			}
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(referenceInformation.RelativeLocation != ReferenceIdentity.Myself); // mismatch

			if (referenceInformation.RelativeLocation == ReferenceIdentity.BeforeMe)
			{
				switch (GroupHeaderStrategy)
				{
					case GroupHeaderStrategy.Parallel:
						if (RectUtil.GetIsEmpty(referenceInformation.HeaderBounds))
						{
							// If we are forward generating, and the reference rect belongs to a container, that container should always have an associated header
							// with which the header bounds are populated
							global::System.Diagnostics.Debug.Assert(false);
							SetPointFromRectInVirtualizingDirection(ref bounds,
								PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
						}
						else
						{
							// looking for rightmost/bottommost location, based on either the last container or (if that's bigger) the header of that group
							SetPointFromRectInVirtualizingDirection(ref bounds, Math.Max(
								PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds), // based on container location
								PointFromRectInVirtualizingDirection(referenceInformation.HeaderBounds) + SizeFromRectInVirtualizingDirection(referenceInformation.HeaderBounds))); // based on header location
						}
						break;

					case GroupHeaderStrategy.Inline:
						// Easy, just go off the reference. No comparison of header and reference bounds needed.
						SetPointFromRectInVirtualizingDirection(ref bounds,
							PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
						break;

					default:
						global::System.Diagnostics.Debug.Assert(false);
						break;
				}

				SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(bounds) + distanceBetweenGroups);
			}
			else
			{
				switch (GroupHeaderStrategy)
				{
					case GroupHeaderStrategy.Parallel:
						// Snapping a header to the first item in its group. For parallel layout, it's left-aligned with the item
						if (RectUtil.GetIsEmpty(referenceInformation.HeaderBounds))
						{
							SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
						}
						else
						{
							// Make sure we leave enough room for the header of the next group
							SetPointFromRectInVirtualizingDirection(ref bounds, Math.Min(
								PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds),
								PointFromRectInVirtualizingDirection(referenceInformation.HeaderBounds) - SizeInVirtualizingDirection(headerDesiredSize)));
						}
						break;

					case GroupHeaderStrategy.Inline:
						// For an inline header, we need to move it over so that it comes before the container
						SetPointFromRectInVirtualizingDirection(ref bounds,
							PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) - SizeInVirtualizingDirection(headerDesiredSize));
						break;

					default:
						global::System.Diagnostics.Debug.Assert(false);
						break;
				}
			}
		}

		SetPointFromRectInNonVirtualizingDirection(ref bounds, SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()));

		SetSizeFromRectInVirtualizingDirection(ref bounds, SizeInVirtualizingDirection(headerDesiredSize));
		SetSizeFromRectInNonVirtualizingDirection(ref bounds, SizeInNonVirtualizingDirection(headerDesiredSize));  // not constrained

		return bounds;
	}

	// Estimate how many items we need to traverse to get from our reference point to a suitable anchor item for the window.
	private void EstimateItemIndexFromWindow(
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window,
		out Rect pTargetRect,
		out int targetItemIndex)
	{
		int totalItems;
		totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

		global::System.Diagnostics.Debug.Assert(0 < totalItems);

		// todo: it is known that this estimation is not perfect
		// we will improve this as we log bugs for non-accuracy

		// Determine which reference we will use
		// For now, if both are provided, we only use the header
		Rect referenceRect;
		int referenceIndex;
		int referenceIndexInGroup; // Used to determine virtualizing and stacking line locations
		float headerAdjustment;
		bool referenceIsHeader;
		if (headerReference.ElementIndex == -1)
		{
			referenceIndex = containerReference.ElementIndex;
			referenceIndexInGroup = containerReference.ElementIndex;
			referenceRect = containerReference.ElementBounds;
			headerAdjustment = 0;
			referenceIsHeader = false;
		}
		else
		{
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
				headerReference.ElementIndex,
				out referenceIndex,
				out _ /* pItemCountInGroup */);
			referenceIndexInGroup = 0;
			referenceRect = headerReference.ElementBounds;
			float headerExtent = SizeFromRectInVirtualizingDirection(headerReference.ElementBounds);
			if (headerExtent <= 0)
			{
				headerExtent = GetAverageHeaderSize();
			}
			// If we have inline headers, take the size into account before we start counting items
			if (GroupHeaderStrategy == GroupHeaderStrategy.Inline)
			{
				headerAdjustment = headerExtent;
			}
			else
			{
				headerAdjustment = 0;
			}
			referenceIsHeader = true;
		}
		float virtualizedReferencePoint = PointFromRectInVirtualizingDirection(referenceRect);
		float calculatedPosition = virtualizedReferencePoint;

		// Figure out which direction we're going
		RelativePosition relativeReferencePosition = GetReferenceDirectionFromWindow(referenceRect, window);

		float averageContainerSize = GetAverageContainerSize();

		int itemDelta = 0;
		switch (relativeReferencePosition)
		{
			case RelativePosition.Before:
			{
				// Take into account a potential inline header
				virtualizedReferencePoint += headerAdjustment;

				// Gather information about the distance to traverse
				float nearWindowEdge = PointFromRectInVirtualizingDirection(window);
				float distance = nearWindowEdge - virtualizedReferencePoint;
				if (distance > 0)
				{
					itemDelta = (int)Math.Floor(distance / averageContainerSize);
				}
				else
				{
					itemDelta = 0;
				}
				break;
			}

			case RelativePosition.After:
			{
				// Gather information about the distance to traverse
				float nearWindowEdge = PointFromRectInVirtualizingDirection(window) + SizeFromRectInVirtualizingDirection(window);
				float distance = nearWindowEdge - virtualizedReferencePoint;
				if (distance < 0 && !referenceIsHeader)
				{
					itemDelta = (int)Math.Floor(distance / averageContainerSize);
				}
				else
				{
					// In case we're going backwards from a header, just pick the first item in the group.
					itemDelta = 0;
				}
				break;
			}

			default:
			{
				itemDelta = 0;
				break;
			}
		}

		// Get the target index, bounds-check it, and calculate the distance moved
		targetItemIndex = referenceIndex + itemDelta;
		targetItemIndex = Math.Max(0, targetItemIndex);
		targetItemIndex = Math.Min(targetItemIndex, totalItems - 1);
		itemDelta = targetItemIndex - referenceIndex;
		calculatedPosition = virtualizedReferencePoint + (itemDelta * averageContainerSize);

		pTargetRect = default;
		SetPointFromRectInVirtualizingDirection(ref pTargetRect, calculatedPosition);
		switch (GroupHeaderStrategy)
		{
			case GroupHeaderStrategy.Parallel:
				SetPointFromRectInNonVirtualizingDirection(ref pTargetRect, SizeInNonVirtualizingDirection(m_headerSize));
				break;

			case GroupHeaderStrategy.Inline:
			case GroupHeaderStrategy.None:
				SetPointFromRectInNonVirtualizingDirection(ref pTargetRect, 0);
				break;
		}
		SetPointFromRectInNonVirtualizingDirection(ref pTargetRect, PointFromRectInNonVirtualizingDirection(pTargetRect) + SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()));
		SetSizeFromRectInVirtualizingDirection(ref pTargetRect, averageContainerSize);
		SetSizeFromRectInNonVirtualizingDirection(ref pTargetRect, 0);
	}

	// Estimate how many groups we need to traverse to get from our reference point to a suitable
	// anchor group for the window, by walking through the groups.
	private void EstimateGroupIndexFromWindow(
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window,
		out Rect pTargetRect,
		out int targetGroupIndex)
	{
		int totalItems;
		int totalGroups;
		totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
		totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

		float averageHeaderSize = GetAverageHeaderSize();
		float referenceHeaderSize = (SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) > 0) ?
			SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) : averageHeaderSize;
		float nearWindowEdge = PointFromRectInVirtualizingDirection(window);
		float farWindowEdge = nearWindowEdge + SizeFromRectInVirtualizingDirection(window);

		global::System.Diagnostics.Debug.Assert(nearWindowEdge <= farWindowEdge);

		// CalculatedPosition is the current position we are looking at. candidatePosition is the next one
		// to be looked at.
		float calculatedPosition = PointFromRectInVirtualizingDirection(headerReference.ElementBounds);
		targetGroupIndex = headerReference.ElementIndex;

		if (calculatedPosition + referenceHeaderSize < nearWindowEdge)
		{
			// Our header is before of the window
			// Need to walk forward to reach it, making sure we step back if a large group overshoots the window entirely
			// To perform the backtrack in this case, we first calculate a "candidate position", accepting it only if we didn't overshoot

			bool found = false;
			int itemsInGroup;
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
				targetGroupIndex,
				out _ /* pStartItemIndex */,
				out itemsInGroup);

			// Since we have this header's size and group count, let's just use it first before jumping off into averages
			float candidatePosition = calculatedPosition + GetVirtualizedGroupExtent((float)itemsInGroup, referenceHeaderSize);

			// Bail out if we encounter the last group, as we can't go farther than that.
			while (!found && targetGroupIndex + 1 < totalGroups)
			{
				if (farWindowEdge < candidatePosition)
				{
					// That group was big! We overshot the window, so let's not move to the next group and use the current one
					found = true;
				}
				else if (candidatePosition + averageHeaderSize < nearWindowEdge)
				{
					// We're going to move to the next group and continue
					++targetGroupIndex;
					calculatedPosition = candidatePosition;
					GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
						targetGroupIndex,
						out _ /* pStartItemIndex */,
						out itemsInGroup);
					candidatePosition = calculatedPosition + GetVirtualizedGroupExtent((float)itemsInGroup, averageHeaderSize);
				}
				else
				{
					// Nailed it! This header looks like it will actually be in the window. Let's take it.
					++targetGroupIndex;
					calculatedPosition = candidatePosition;
					found = true;
				}
			}
		}
		else if (farWindowEdge < calculatedPosition)
		{
			// Our header is currently after the window
			// Need to walk backward to reach it

			bool found = false;
			// If we hit group 0, we can't go any farther, so we might as well stop there
			while (!found && targetGroupIndex > 0)
			{
				--targetGroupIndex;
				int itemsInGroup;
				GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
					targetGroupIndex,
					out _ /* pStartItemIndex */,
					out itemsInGroup);

				calculatedPosition -= GetVirtualizedGroupExtent((float)itemsInGroup, averageHeaderSize);

				if (calculatedPosition <= farWindowEdge)
				{
					// Hooray! We've encountered the window. Let's stop here, now that we've traveled far enough
					found = true;
				}
			}
		}
		else
		{
			// This header is currently hitting the window. Nothing to do here!
		}

		Rect targetRect = default;

		SetPointFromRectInVirtualizingDirection(ref targetRect, calculatedPosition);
		SetPointFromRectInNonVirtualizingDirection(ref targetRect, SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()));
		if (targetGroupIndex == headerReference.ElementIndex)
		{
			SetSizeFromRectInVirtualizingDirection(ref targetRect, referenceHeaderSize);
		}
		else
		{
			SetSizeFromRectInVirtualizingDirection(ref targetRect, GetAverageHeaderSize());
		}
		SetSizeFromRectInNonVirtualizingDirection(ref targetRect, SizeInNonVirtualizingDirection(m_headerSize));
		pTargetRect = targetRect;
	}

	// Estimate the location of an anchor item, given an item index delta.
	private void EstimateContainerLocation(
		int targetItemIndex,
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window,
		out Rect targetRect)
	{
		int totalItems;
		totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

		global::System.Diagnostics.Debug.Assert(0 < totalItems);
		global::System.Diagnostics.Debug.Assert(0 <= targetItemIndex && targetItemIndex < totalItems);

		targetRect = default;

		// Determine which reference we will use
		// For now, if both are provided, we only use the header
		Rect referenceRect;
		int referenceIndex;
		int referenceIndexInGroup; // Used to determine virtualizing and stacking line locations
		float headerAdjustment;
		bool referenceIsHeader;
		if (headerReference.ElementIndex == -1)
		{
			referenceIndex = containerReference.ElementIndex;
			referenceIndexInGroup = containerReference.ElementIndex;
			referenceRect = containerReference.ElementBounds;
			headerAdjustment = 0;
			referenceIsHeader = false;
		}
		else
		{
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
				headerReference.ElementIndex,
				out referenceIndex,
				out _ /* pItemCountInGroup */);

			referenceIndexInGroup = 0;
			referenceRect = headerReference.ElementBounds;
			float headerExtent = SizeFromRectInVirtualizingDirection(headerReference.ElementBounds);
			if (headerExtent <= 0)
			{
				headerExtent = GetAverageHeaderSize();
			}
			// If we have inline headers, take the size into account before we start counting items
			if (GroupHeaderStrategy == GroupHeaderStrategy.Inline)
			{
				headerAdjustment = headerExtent;
			}
			else
			{
				headerAdjustment = 0;
			}
			referenceIsHeader = true;

			// If we attempt to estimate backwards from a header, just use the first item in the group
			targetItemIndex = Math.Max(targetItemIndex, referenceIndex);
		}
		float virtualizedReferencePoint = PointFromRectInVirtualizingDirection(referenceRect) + headerAdjustment;
		float averageContainerSize = GetAverageContainerSize();

		int itemDelta = targetItemIndex - referenceIndex;
		SetPointFromRectInVirtualizingDirection(ref targetRect, virtualizedReferencePoint + (itemDelta * averageContainerSize));

		switch (GroupHeaderStrategy)
		{
			case GroupHeaderStrategy.Parallel:
				SetPointFromRectInNonVirtualizingDirection(ref targetRect, SizeInNonVirtualizingDirection(m_headerSize));
				break;

			case GroupHeaderStrategy.Inline:
			case GroupHeaderStrategy.None:
				SetPointFromRectInNonVirtualizingDirection(ref targetRect, 0);
				break;
		}
		SetPointFromRectInNonVirtualizingDirection(ref targetRect, PointFromRectInNonVirtualizingDirection(targetRect) + SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()));
		SetSizeFromRectInVirtualizingDirection(ref targetRect, averageContainerSize);
		SetSizeFromRectInNonVirtualizingDirection(ref targetRect, 0);
	}

	// Estimate the location of an anchor group, using items-per-group to estimate an average group extent.
	private void EstimateHeaderLocation(
		int targetGroupIndex,
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window,
		out Rect targetRect)
	{
		int totalItems;
		int totalGroups;
		totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
		totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

		global::System.Diagnostics.Debug.Assert(0 < totalGroups);
		global::System.Diagnostics.Debug.Assert(0 <= targetGroupIndex && targetGroupIndex < (int)totalGroups);

		targetRect = default;

		float averageHeaderSize = GetAverageHeaderSize();
		float referenceHeaderSize = (SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) > 0) ?
			SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) : averageHeaderSize;

		float calculatedPosition;

		// Let's handle the special case where we are estimating an adjacent header
		if (targetGroupIndex == headerReference.ElementIndex || targetGroupIndex == headerReference.ElementIndex + 1)
		{
			int firstItemInGroup;
			int itemCountInGroup;

			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
				headerReference.ElementIndex,
				out firstItemInGroup,
				out itemCountInGroup);

			if (containerReference.ElementIndex != -1 && firstItemInGroup <= containerReference.ElementIndex && containerReference.ElementIndex < firstItemInGroup + itemCountInGroup)
			{
				float averageContainerSize = GetAverageContainerSize();

				float referenceItemSize = (SizeFromRectInVirtualizingDirection(containerReference.ElementBounds) > 0) ?
					SizeFromRectInVirtualizingDirection(containerReference.ElementBounds) : averageContainerSize;

				// We're using this item to estimate the placement of an adjacent header
				int itemIndexInGroup = containerReference.ElementIndex - firstItemInGroup;
				float itemReference = PointFromRectInVirtualizingDirection(containerReference.ElementBounds);

				if (targetGroupIndex == headerReference.ElementIndex)
				{
					// We're looking for the header immediately before our item
					calculatedPosition = itemReference - itemIndexInGroup * averageContainerSize;

					if (GroupHeaderStrategy == GroupHeaderStrategy.Inline)
					{
						// Make room for an inline header
						calculatedPosition -= referenceHeaderSize;
					}
				}
				else
				{
					// We're looking for the header immediately after our item
					// Add the item we know about
					calculatedPosition = itemReference + referenceItemSize;
					// And now, add the rest
					int remainingItems = itemCountInGroup - itemIndexInGroup - 1;
					if (remainingItems > 0)
					{
						calculatedPosition += remainingItems * averageContainerSize;
					}
				}
			}
			else
			{
				// We're using this header to estimate the placement of an adjacent header
				calculatedPosition = PointFromRectInVirtualizingDirection(headerReference.ElementBounds);
				if (targetGroupIndex == headerReference.ElementIndex + 1)
				{
					calculatedPosition += GetVirtualizedGroupExtent((float)itemCountInGroup, referenceHeaderSize);
				}
			}
		}
		else
		{
			float virtualizedReferencePoint = PointFromRectInVirtualizingDirection(headerReference.ElementBounds);
			calculatedPosition = virtualizedReferencePoint;

			// Put a sanity bound on the group index, for FRE builds
			targetGroupIndex = Math.Max(0, targetGroupIndex);
			targetGroupIndex = Math.Min(targetGroupIndex, (int)(totalGroups - 1));

			// Determine which direction we're going, and calculate average group sizes accordingly
			RelativePosition relativePos;
			if (headerReference.ElementIndex < targetGroupIndex)
			{
				// Going forward, i.e., our reference is before the estimation region
				relativePos = RelativePosition.Before;
			}
			else if (targetGroupIndex < headerReference.ElementIndex)
			{
				// Going backward, i.e., our reference is after the estimation region
				relativePos = RelativePosition.After;
			}
			else
			{
				relativePos = RelativePosition.Inside;
			}

			int firstItemIndexInGroup;
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
				headerReference.ElementIndex,
				out firstItemIndexInGroup,
				out _ /* pItemCountInGroup */);

			int remainingGroups = GetRemainingGroups(headerReference.ElementIndex, totalGroups, relativePos);
			int remainingItems = GetRemainingItems(firstItemIndexInGroup, totalItems, relativePos);

			if (remainingGroups > 0)
			{
				float averageItemsPerRemainingGroup = (remainingItems) / (float)remainingGroups;
				float averageExtentOfRemainingGroups = GetAverageVirtualizedGroupExtent(averageItemsPerRemainingGroup);
				calculatedPosition = virtualizedReferencePoint + averageExtentOfRemainingGroups * (targetGroupIndex - headerReference.ElementIndex);
			}
			else
			{
				calculatedPosition = virtualizedReferencePoint;
			}
		}

		SetPointFromRectInVirtualizingDirection(ref targetRect, calculatedPosition);
		SetPointFromRectInNonVirtualizingDirection(ref targetRect, SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()));
		if (targetGroupIndex == headerReference.ElementIndex)
		{
			// If the target group is the same as the reference group, just reuse its size
			SetSizeFromRectInVirtualizingDirection(ref targetRect, referenceHeaderSize);
		}
		else
		{
			SetSizeFromRectInVirtualizingDirection(ref targetRect, averageHeaderSize);
		}
		SetSizeFromRectInNonVirtualizingDirection(ref targetRect, SizeInNonVirtualizingDirection(m_headerSize));
	}

	private void EstimateNonGroupedExtent(
		EstimationReference containerReference,
		Rect windowConstraint,
		out Size estimate)
	{
		estimate = default;
		int totalItems;
		totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

		// containers are variable size, so going off just the average is wrong
		float distance = 0;
		if (totalItems > containerReference.ElementIndex + 1)
		{
			distance = GetAverageVirtualizedExtentOfItems((float)(totalItems - (containerReference.ElementIndex + 1))); // don't include lastitem itself
		}

		SetSizeInVirtualizingDirection(ref estimate, distance +
			PointFromRectInVirtualizingDirection(containerReference.ElementBounds) + SizeFromRectInVirtualizingDirection(containerReference.ElementBounds));

		// Account for the padding at the end
		SetSizeInVirtualizingDirection(ref estimate, SizeInVirtualizingDirection(estimate) + SizeInVirtualizingDirection(GetGroupPaddingAtEnd()));
		SetSizeInNonVirtualizingDirection(ref estimate, m_extentInNonVirtualizingDirection);
	}

	private void EstimateGroupedExtent(
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect windowConstraint,
		out Size extent)

	{
		global::System.Diagnostics.Debug.Assert(IsGrouping && m_headerSizeSet);

		int totalItems;
		int totalGroups;
		totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
		totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

		extent = default;

		float averageHeaderSize = GetAverageHeaderSize();
		float referenceHeaderExtent = (SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) > 0) ?
			SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) : averageHeaderSize;

		float referencePoint;
		int firstItemIndexInGroup;
		int itemCountInGroup;
		GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
			headerReference.ElementIndex,
			out firstItemIndexInGroup,
			out itemCountInGroup);

		if (containerReference.ElementIndex != -1 && containerReference.ElementIndex >= firstItemIndexInGroup)
		{
			// This item will gives us an idea of the group extent that's better than going from the header
			// So, we'll estimate from here to the end of the group, then pick back up with average group sizes
			referencePoint = PointFromRectInVirtualizingDirection(containerReference.ElementBounds) + SizeFromRectInVirtualizingDirection(containerReference.ElementBounds);

			global::System.Diagnostics.Debug.Assert(containerReference.ElementIndex < firstItemIndexInGroup + itemCountInGroup);
			int itemInGroup = containerReference.ElementIndex - firstItemIndexInGroup;
			int itemsLeftInThisGroup = itemCountInGroup - itemInGroup - 1;
			global::System.Diagnostics.Debug.Assert(itemsLeftInThisGroup >= 0);

			float averageContainerSize = GetAverageContainerSize();
			referencePoint += averageContainerSize * itemsLeftInThisGroup;

			// Make sure we don't have a header that's bigger than this
			float headerEnd = PointFromRectInVirtualizingDirection(headerReference.ElementBounds) + referenceHeaderExtent;
			if (headerEnd > referencePoint)
			{
				referencePoint = headerEnd;
			}

			// Take the group padding at the end of this group into account
			referencePoint += SizeInVirtualizingDirection(GetGroupPaddingAtEnd());
		}
		else
		{
			// Don't have an item in this group. Just estimate it based on item count and average item sizes
			referencePoint = PointFromRectInVirtualizingDirection(headerReference.ElementBounds);
			referencePoint += GetVirtualizedGroupExtent((float)itemCountInGroup, referenceHeaderExtent);

			// GetVirtualizedGroupExtent adds in the group padding on both ends, but our header has already had the left/top padding applied
			// Subtract it out so it's not counted twice
			referencePoint -= SizeInVirtualizingDirection(GetGroupPaddingAtStart());
		}

		// Now we have a point at the end of the reference group
		// Let's estimate any remaining groups based on an average group size
		int remainingGroups = GetRemainingGroups(headerReference.ElementIndex + 1, totalGroups, RelativePosition.Before);
		int remainingItems = GetRemainingItems(firstItemIndexInGroup + itemCountInGroup, totalItems, RelativePosition.Before);

		if (remainingGroups > 0)
		{
			float averageItemsPerRemainingGroup = remainingItems / (float)remainingGroups;
			float averageExtentOfRemainingGroups = GetAverageVirtualizedGroupExtent(averageItemsPerRemainingGroup);
			referencePoint += remainingGroups * averageExtentOfRemainingGroups;
		}

		SetSizeInVirtualizingDirection(ref extent, referencePoint);
		SetSizeInNonVirtualizingDirection(ref extent, m_extentInNonVirtualizingDirection);
	}
}
