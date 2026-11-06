// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference WrappingLayoutStrategyImpl.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

// Represents a wrapping strategy for new style virtualizing panels.

#nullable enable

using System;
using DirectUI;
using Windows.Foundation;

#pragma warning disable CS0219 // Upstream assigns referenceIsHeader without reading it.

namespace Microsoft.UI.Xaml.Controls;

internal partial class WrappingLayoutStrategyImpl
{
	public WrappingLayoutStrategyImpl()
		: base(true /* useFullWidthHeaders */, true /* isWrapping */)
	{
		m_cellSizeSet = false;
		m_headerSizeSet = false;
		m_maxRowsOrColumns = -1;
		m_itemWidthFromPanel = double.NaN;
		m_itemHeightFromPanel = double.NaN;
		m_cachedStackingLines = 0;
		m_cachedCellSize = default;
		m_cellSize = new Size(1, 0);
		m_headerSize = new Size(1, 0);
	}

	#region Layout related methods

	// returns the size we should use to measure a container or header with
	// itemIndex - indicates an index of valid item or -1 for general, non-special items
	public Size GetElementMeasureSize(
		ElementType elementType,
		int elementIndex,
		Rect windowConstraint)
	{
		Size result = new(float.PositiveInfinity, float.PositiveInfinity);
		SetSizeInNonVirtualizingDirection(ref result, SizeFromRectInNonVirtualizingDirection(windowConstraint));

		// account for group padding in measure size, reduce the size in non virtualizing direction by the amount of group padding
		float groupPaddingInNonVirtualizingDirection = SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()) + SizeInNonVirtualizingDirection(GetGroupPaddingAtEnd());
		float measureSizeInNonVirtualizingDirection = SizeInNonVirtualizingDirection(result) - groupPaddingInNonVirtualizingDirection;
		SetSizeInNonVirtualizingDirection(ref result, Math.Max(0.0f, measureSizeInNonVirtualizingDirection));

		if (elementType == ElementType.ItemContainer)
		{
			if (IsItemWidthPropertySet())
			{
				result.Width = (float)m_itemWidthFromPanel;
			}
			if (IsItemHeightPropertySet())
			{
				result.Height = (float)m_itemHeightFromPanel;
			}

			if (m_cellSizeSet && elementIndex != c_specialItemIndex)
			{
				result = m_cellSize;
			}
		}
		// the special groupindex should get the available size
		// we only limit if we have a headersize set
		// we only need to limit in parallel layouts: inline scenarios need to have a different logic
		else if (
			GroupHeaderStrategy == GroupHeaderStrategy.Parallel &&
			elementIndex != c_specialGroupIndex &&
			m_headerSizeSet)
		{
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
		Rect returnValue;

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
			returnValue = GetContainerBounds(elementIndex, indexInGroup, containerDesiredSize, referenceInformation, windowConstraint);
		}
		else
		{
			returnValue = GetHeaderBounds(elementIndex, containerDesiredSize, referenceInformation, windowConstraint);
		}
		return returnValue;
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
			// By the time we arrange, we will have a cell size.
			global::System.Diagnostics.Debug.Assert(m_cellSizeSet);

			result.Width = m_cellSize.Width;
			result.Height = m_cellSize.Height;
		}
		else
		{
			if (GroupHeaderStrategy == GroupHeaderStrategy.Inline)
			{
				SetSizeFromRectInNonVirtualizingDirection(ref result, SizeInNonVirtualizingDirection(finalSize));

				// account for group padding, reduce the size in non virtualizing direction by the amount of group padding
				float groupPaddingInNonVirtualizingDirection = SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()) + SizeInNonVirtualizingDirection(GetGroupPaddingAtEnd());
				float arrangeSizeInNonVirtualizingDirection = SizeFromRectInNonVirtualizingDirection(result) - groupPaddingInNonVirtualizingDirection;
				SetSizeFromRectInNonVirtualizingDirection(ref result, Math.Max(0.0f, arrangeSizeInNonVirtualizingDirection));
			}
			else
			{
				// By the time we arrange, we will have a cell size.
				global::System.Diagnostics.Debug.Assert(m_headerSizeSet);

				SetSizeFromRectInNonVirtualizingDirection(ref result, Math.Max(SizeFromRectInNonVirtualizingDirection(result), SizeInNonVirtualizingDirection(m_headerSize)));
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
		else
		{
			if (requestingHeader)
			{
				// this holds true for either a header or container reference
				if (referenceInformation.RelativeLocation == ReferenceIdentity.BeforeMe)
				{
					// we're a new header that is on the right of the reference. is there room  to the right?
					shouldContinue =
						PointFromRectInVirtualizingDirection(windowToFill) + SizeFromRectInVirtualizingDirection(windowToFill)
						> PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
				}
				else
				{
					// Due to the possibility of inline headers, the decision to start a new backwards group should be based on the header of
					// the current group, not some container inside it.
					global::System.Diagnostics.Debug.Assert(referenceInformation.ReferenceIsHeader);

					// is there room to the left?
					shouldContinue = PointFromRectInVirtualizingDirection(windowToFill) < PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
				}
			}
			else
			{
				int stackingLines = 0;
				int virtualizingLine = 0;
				int stackingLine = 0;

				DetermineLineInformation(windowToFill, elementIndex, out stackingLines, out virtualizingLine, out stackingLine);

				if (referenceInformation.RelativeLocation == ReferenceIdentity.BeforeMe)
				{
					if (stackingLine == 0)
					{
						if (referenceInformation.ReferenceIsHeader &&
							GroupHeaderStrategy.Parallel == GroupHeaderStrategy)
						{
							// we're under a header, it makes sense that if that header was created, we should also be created
							// since we are laid out under the header (todo: currently only supporting parallel)
							shouldContinue =
								PointFromRectInVirtualizingDirection(windowToFill) + SizeFromRectInVirtualizingDirection(windowToFill)
								> PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
						}
						else
						{
							// we're in a new column, so is there room to the right?
							shouldContinue =
								PointFromRectInVirtualizingDirection(windowToFill) + SizeFromRectInVirtualizingDirection(windowToFill)
								> PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
						}
					}
					else
					{
						shouldContinue = PointFromRectInVirtualizingDirection(windowToFill) + SizeFromRectInVirtualizingDirection(windowToFill) > PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
					}
				}
				else
				{
					if (referenceInformation.ReferenceIsHeader)
					{
						// we're a container in the last row of the group that is on the left of the referenced header
						shouldContinue = PointFromRectInVirtualizingDirection(windowToFill) <= PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
					}
					else if (stackingLine == stackingLines - 1)
					{
						// we're a container that is at the end of a column
						shouldContinue = PointFromRectInVirtualizingDirection(windowToFill) < PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
					}
					else
					{
						shouldContinue = PointFromRectInVirtualizingDirection(windowToFill) < PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeInVirtualizingDirection(m_cellSize);
					}
				}
			}
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
		out int result)
	{
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
	}

	public Rect EstimateElementBounds(
		ElementType elementType,
		int elementIndex,
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window)
	{
		Rect result;

		if (elementType == ElementType.ItemContainer)
		{
			result = EstimateContainerLocation(
				elementIndex,
				headerReference,
				containerReference,
				window);
		}
		else
		{
			result = EstimateHeaderLocation(
				elementIndex,
				headerReference,
				containerReference,
				window);
		}

		return result;
	}

	public Size EstimatePanelExtent(
		EstimationReference lastHeaderReference,
		EstimationReference lastContainerReference,
		Rect windowConstraint)
	{
		Size result;

		if (IsGrouping)
		{
			result = EstimateGroupedExtent(
				lastHeaderReference,
				lastContainerReference,
				windowConstraint);
		}
		else
		{
			result = EstimateNonGroupedExtent(
				lastContainerReference,
				windowConstraint);
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
		global::System.Diagnostics.Debug.Assert(m_cellSizeSet);

		pSearchHint = IndexSearchHint.NoHint;
		pElementType = ElementType.ItemContainer;

		int stackingLines = DetermineMaxStackingLine(windowConstraint);
		global::System.Diagnostics.Debug.Assert(stackingLines > 0);

		int totalItems;
		int referenceItemIndex;
		Rect referenceItemRect = reference.ElementBounds;

		// When grouping, we convert the indices to be relative to the group.
		if (IsGrouping)
		{
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromItemIndex(
				reference.ElementIndex,
				out _ /* pIndexOfGroup */,
				out referenceItemIndex,
				out totalItems);
		}
		else
		{
			referenceItemIndex = reference.ElementIndex;
			totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
		}

		// Calculate the row and column of the given point using the reference container as a starting point.
		int virtualizingDirectionDiff = (int)((PointInVirtualizingDirection(point) - PointFromRectInVirtualizingDirection(referenceItemRect)) / SizeInVirtualizingDirection(m_cellSize));
		int stackingDirectionDiff = (int)((PointInNonVirtualizingDirection(point) - PointFromRectInNonVirtualizingDirection(referenceItemRect)) / SizeInNonVirtualizingDirection(m_cellSize));

		// lock stacking direction to staking line size.
		stackingDirectionDiff = Math.Min(Math.Max(stackingDirectionDiff, -(referenceItemIndex % stackingLines)), (stackingLines - 1) - (referenceItemIndex % stackingLines));

		// if we drag off the panel boundary we should correct virtualizingDirectionDiff to be the first or last line.
		if (referenceItemIndex + virtualizingDirectionDiff * stackingLines < 0)
		{
			virtualizingDirectionDiff = -1 * referenceItemIndex / stackingLines;
		}
		else if (referenceItemIndex - (referenceItemIndex % stackingLines) + virtualizingDirectionDiff * stackingLines > totalItems - 1)
		{
			virtualizingDirectionDiff = (totalItems - 1 - (referenceItemIndex - (referenceItemIndex % stackingLines))) / stackingLines;
		}

		int pointIndex = referenceItemIndex + (virtualizingDirectionDiff * stackingLines + stackingDirectionDiff);
		if (pointIndex > totalItems)
		{
			// try previous line.
			int upperLinePointIndex = referenceItemIndex + ((virtualizingDirectionDiff - 1) * stackingLines + stackingDirectionDiff);

			// if we have upper line, then use it
			if (upperLinePointIndex > 0)
			{
				pointIndex = upperLinePointIndex;
			}
		}

		pElementIndex = Math.Min(totalItems, pointIndex);
		pSearchHint = IndexSearchHint.Exact;

		if (requestingInsertionIndex)
		{
			int unusedStackingLines = 0;
			int targetStackingLine = 0;
			int targetVirtualizingLine = 0;

			DetermineLineInformation(windowConstraint, pElementIndex, out unusedStackingLines, out targetVirtualizingLine, out targetStackingLine);
			float calculatedPosition = GetItemStackingPosition(targetStackingLine);

			// If we're below the midpoint of the container under the point, we actually want to insert at the next index.
			if ((PointInNonVirtualizingDirection(point) - calculatedPosition) >= SizeInNonVirtualizingDirection(m_cellSize) / 2.0f)
			{
				++pElementIndex;
			}
		}

		// When grouping, we convert to convert back indices to be relative to the source.
		if (IsGrouping)
		{
			int startItemIndex = reference.ElementIndex - referenceItemIndex;
			pElementIndex += startItemIndex;
		}
	}

	// Based on current element's index/type and action, return the next element index/type.
	public void GetTargetIndexFromNavigationAction(
		ElementType elementType,
		int elementIndex,
		KeyNavigationAction action,
		Rect windowConstraint,
		int itemIndexHintForHeaderNavigation,
		out ElementType pTargetElementType,
		out int pTargetElementIndex)
	{
		// If the element type is GroupHeader, then make sure that we're actually
		// grouping.
		global::System.Diagnostics.Debug.Assert(elementType == ElementType.ItemContainer || IsGrouping);

		int totalItems = 0;
		int totalGroups = 0;

		int targetIndex = elementIndex;
		var targetType = elementType;

		int maxLineLength = DetermineMaxStackingLine(windowConstraint);

		totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
		totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

		global::System.Diagnostics.Debug.Assert(0 <= elementIndex && elementIndex < (elementType == ElementType.ItemContainer ? totalItems : totalGroups));

		if (action != KeyNavigationAction.Left &&
			action != KeyNavigationAction.Right &&
			action != KeyNavigationAction.Up &&
			action != KeyNavigationAction.Down)
		{
			throw new ArgumentException("E_INVALIDARG", nameof(action));
		}

		int step = (action == KeyNavigationAction.Left || action == KeyNavigationAction.Up) ? -1 : 1;

		if (elementType == ElementType.ItemContainer)
		{
			// Easy case: the action is along layout orientation, therefore handle it.
			if ((VirtualizationDirection == Orientation.Vertical && (action == KeyNavigationAction.Left || action == KeyNavigationAction.Right)) ||
				(VirtualizationDirection == Orientation.Horizontal && (action == KeyNavigationAction.Up || action == KeyNavigationAction.Down)))
			{
				targetIndex = Math.Min(Math.Max(elementIndex + step, 0), totalItems - 1);
			}
			// The action is not in layout direction.  We must jump to the next item in the same row/column.
			// Situation 1 : we are not grouping, easy.
			else if (!IsGrouping)
			{
				int nextIndex = elementIndex + step * maxLineLength;
				if (0 <= nextIndex && nextIndex < totalItems)
				{
					targetIndex = nextIndex;
				}
			}
			// Situation 2 : we are grouping.
			else
			{
				targetIndex = GetTargetItemIndexInNextLineWithGrouping(elementIndex, step, maxLineLength);
			}
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(IsGrouping);
			targetIndex = Math.Min(Math.Max(elementIndex + step, 0), totalGroups - 1);

			// In-order navigating out of a group header should not use the item index hint since
			// we're not trying to maintain navigation across a row or down a column.
			if ((VirtualizationDirection == Orientation.Vertical && (action == KeyNavigationAction.Left || action == KeyNavigationAction.Right)) ||
				(VirtualizationDirection == Orientation.Horizontal && (action == KeyNavigationAction.Up || action == KeyNavigationAction.Down)))
			{
				itemIndexHintForHeaderNavigation = -1;
			}
		}

		// Evaluate whether we need to change the element type & index.
		if (IsGrouping)
		{
			if (elementType == ElementType.ItemContainer)
			{
				int targetGroupHeaderIndex = TryGetTargetHeaderIndexWithItemNavigation(elementIndex, targetIndex, step, maxLineLength);
				if (targetGroupHeaderIndex != -1)
				{
					targetIndex = targetGroupHeaderIndex;
					targetType = ElementType.GroupHeader;
				}
			}
			else
			{
				int targetItemIndex = TryGetTargetItemIndexWithHeaderNavigation(elementIndex, targetIndex, step, maxLineLength, itemIndexHintForHeaderNavigation);
				if (targetItemIndex != -1)
				{
					targetIndex = targetItemIndex;
					targetType = ElementType.ItemContainer;
				}
			}
		}

		global::System.Diagnostics.Debug.Assert(0 <= targetIndex && targetIndex < (targetType == ElementType.ItemContainer ? totalItems : totalGroups));
		pTargetElementIndex = targetIndex;
		pTargetElementType = targetType;
	}

	private int GetTargetItemIndexInNextLineWithGrouping(
		int currItemIndex,
		int step,
		int maxLineLength)
	{
		global::System.Diagnostics.Debug.Assert(IsGrouping);

		int targetIndex = currItemIndex;

		int groupIndex = 0;
		int indexInGroup = 0;
		int itemsCountInGroup = 0;

		int totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();
		GetLayoutDataInfoProviderNoRef.GetGroupInformationFromItemIndex(
			currItemIndex,
			out groupIndex,
			out indexInGroup,
			out itemsCountInGroup);

		int targetIndexInGroup = indexInGroup + maxLineLength * step;

		global::System.Diagnostics.Debug.Assert(step == 1 || step == -1);

		// Next index is within a line in this group.
		if (0 <= targetIndexInGroup && (int)(targetIndexInGroup / maxLineLength) <= (int)((itemsCountInGroup - 1) / maxLineLength))
		{
			int startItemIndex = currItemIndex - indexInGroup;
			targetIndexInGroup = Math.Min(targetIndexInGroup, itemsCountInGroup - 1);
			targetIndex = targetIndexInGroup + startItemIndex;
		}
		else
		{
			int targetElementRowOrCoumn = indexInGroup % maxLineLength;

			// Finds a non-empty group.
			while (0 <= (groupIndex + step) && (groupIndex + step) < totalGroups)
			{
				groupIndex += step;
				int startItemIndex;

				GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
					groupIndex,
					out startItemIndex,
					out itemsCountInGroup);

				if (itemsCountInGroup > 0)
				{
					targetIndexInGroup = Math.Min(
						itemsCountInGroup - 1,
						targetElementRowOrCoumn - (step == 1 ? 0 : (itemsCountInGroup % maxLineLength) - itemsCountInGroup));

					targetIndex = targetIndexInGroup + startItemIndex;
					break;
				}
			}
		}

		return targetIndex;
	}

	private int TryGetTargetHeaderIndexWithItemNavigation(
		int currentItemIndex,
		int targetItemIndex,
		int step,
		int maxLineLength)
	{
		global::System.Diagnostics.Debug.Assert(IsGrouping);

		int targetGroupHeaderIndex = -1;

		if (step != 0)
		{
			int currIndexOfGroup = 0;
			int currIndexInsideOfGroup = 0;
			int currItemCountInGroup = 0;
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromItemIndex(currentItemIndex, out currIndexOfGroup, out currIndexInsideOfGroup, out currItemCountInGroup);

			int targetIndexOfGroup = 0;
			int targetIndexInsideOfGroup = 0;
			int targetItemCountInGroup = 0;
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromItemIndex(targetItemIndex, out targetIndexOfGroup, out targetIndexInsideOfGroup, out targetItemCountInGroup);

			if (currentItemIndex == targetItemIndex)
			{
				int lineInGroup = (int)(currIndexInsideOfGroup / maxLineLength);
				int numLinesInGroup = (int)MathF.Ceiling((float)currItemCountInGroup / maxLineLength);

				// We have at least 1 item in this group, so the number of lines should be greater than 0.
				global::System.Diagnostics.Debug.Assert(numLinesInGroup > 0);

				bool isInFirstLineInGroup = (lineInGroup == 0);
				bool isInFirstGroupWithItems = (currentItemIndex == currIndexInsideOfGroup);

				// Handle the special-case of going up/back from the first line in the first group
				// with items.
				if (isInFirstLineInGroup && isInFirstGroupWithItems && step < 0)
				{
					targetGroupHeaderIndex = currIndexOfGroup;
				}
				else
				{
					int totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

					int totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

					// Handle the special-case of going down from the last line in the last group with items.
					bool isInLastLineInGroup = (lineInGroup == (numLinesInGroup - 1));
					bool isInLastGroupWithItems = (currentItemIndex == (totalItems - currItemCountInGroup + currIndexInsideOfGroup));

					if (isInLastLineInGroup && isInLastGroupWithItems && step > 0 && currIndexOfGroup < (totalGroups - 1))
					{
						targetGroupHeaderIndex = currIndexOfGroup + 1;
					}
				}
			}
			else
			{
				// Evaluate whether the new target item index would cross a group boundary
				// when compared to the previous item index.
				if (currIndexOfGroup != targetIndexOfGroup)
				{
					// If advancing forward, don't just set the new target group index to targetIndexOfGroup,
					// instead just increment the value of currIndexOfGroup.  This is so we don't skip
					// group headers that have no items.
					targetGroupHeaderIndex = (step < 0 ? currIndexOfGroup : currIndexOfGroup + step);
				}
			}
		}

		return targetGroupHeaderIndex;
	}

	private int TryGetTargetItemIndexWithHeaderNavigation(
		int currentGroupIndex,
		int targetGroupIndex,
		int step,
		int maxLineLength,
		int itemIndexHintForHeaderNavigation)
	{
		global::System.Diagnostics.Debug.Assert(IsGrouping);

		int targetItemIndex = -1;

		if (step != 0)
		{
			int totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

			int currGroupStartItemIndex = 0;
			int currGroupItemCountInGroup = 0;
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(currentGroupIndex, out currGroupStartItemIndex, out currGroupItemCountInGroup);

			bool isInLastGroupWithItems = currGroupItemCountInGroup > 0 && (totalItems == (currGroupStartItemIndex + currGroupItemCountInGroup));

			if (currentGroupIndex == targetGroupIndex && isInLastGroupWithItems && step > 0)
			{
				// Handle the special-case of going down/forward from the group header for the last group
				// with items.

				if ((itemIndexHintForHeaderNavigation >= currGroupStartItemIndex) &&
					(itemIndexHintForHeaderNavigation < (currGroupStartItemIndex + currGroupItemCountInGroup)))
				{
					// If we're going back into the group that contains our hint item index,
					// then set it as the target index.
					targetItemIndex = itemIndexHintForHeaderNavigation;
				}
				else
				{
					if (itemIndexHintForHeaderNavigation != -1)
					{
						targetItemIndex = GetTargetItemIndexInNextLineWithGrouping(itemIndexHintForHeaderNavigation, step, maxLineLength);
					}
					else
					{
						targetItemIndex = currGroupStartItemIndex;
					}
				}
			}
			else
			{
				int targetGroupStartItemIndex = 0;
				int targetGroupItemCountInGroup = 0;
				GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(targetGroupIndex, out targetGroupStartItemIndex, out targetGroupItemCountInGroup);

				// If the new target index would jump over any items, then determine a new
				// target item index.  This is where we use itemIndexHintForHeaderNavigation that was
				// cached after a group header got focus.  It allows us to preserve navigation
				// down a column or across a row, depending on the orientation.
				if (targetGroupIndex < currentGroupIndex && targetGroupItemCountInGroup > 0)
				{
					// Move backwards to a new group.
					if ((itemIndexHintForHeaderNavigation >= targetGroupStartItemIndex) &&
						(itemIndexHintForHeaderNavigation < (targetGroupStartItemIndex + targetGroupItemCountInGroup)))
					{
						// If we're going back into the group that contains our hint item index,
						// then set it as the target index.
						targetItemIndex = itemIndexHintForHeaderNavigation;
					}
					else
					{
						// If we have a item index hint, then find the item in the next line using that as a reference,
						// otherwise focus the last item in the new group.
						if (itemIndexHintForHeaderNavigation != -1)
						{
							targetItemIndex = GetTargetItemIndexInNextLineWithGrouping(itemIndexHintForHeaderNavigation, step, maxLineLength);
						}
						else
						{

							targetItemIndex = targetGroupStartItemIndex + targetGroupItemCountInGroup - 1;
						}
					}
				}
				else if (targetGroupIndex > currentGroupIndex && currGroupItemCountInGroup > 0)
				{
					// Move forwards to a new group.
					if ((itemIndexHintForHeaderNavigation >= currGroupStartItemIndex) &&
						(itemIndexHintForHeaderNavigation < (currGroupStartItemIndex + currGroupItemCountInGroup)))
					{
						// If we're going back into the group that contains our hint item index,
						// then set it as the target index.
						targetItemIndex = itemIndexHintForHeaderNavigation;
					}
					else
					{
						// If we have a item index hint, then find the item in the next line using that as a reference,
						// otherwise focus the first item in the current group.
						if (itemIndexHintForHeaderNavigation != -1)
						{
							targetItemIndex = GetTargetItemIndexInNextLineWithGrouping(itemIndexHintForHeaderNavigation, step, maxLineLength);
						}
						else
						{
							targetItemIndex = currGroupStartItemIndex;
						}
					}
				}
			}
		}

		return targetItemIndex;
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

		// Is the stacking direction horizontal?
		bool isHorizontal = VirtualizationDirection == Orientation.Vertical;
		int maxStackingLine = DetermineMaxStackingLine(windowConstraint);

		int totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

		if (isHorizontal)
		{
			pIsLeftBoundary = (elementIndex % maxStackingLine == 0);
			pIsBottomBoundary = (elementIndex >= (int)(totalItems - maxStackingLine));
			pIsTopBoundary = (elementIndex < (int)(maxStackingLine));
			pIsRightBoundary = ((elementIndex + 1) % maxStackingLine == 0) || (elementIndex + 1 == totalItems);
		}
		else
		{
			pIsLeftBoundary = (elementIndex < (int)(maxStackingLine));
			pIsBottomBoundary = ((elementIndex + 1) % maxStackingLine == 0) || (elementIndex + 1 == totalItems);
			pIsTopBoundary = (elementIndex % maxStackingLine == 0);
			pIsRightBoundary = (elementIndex >= (int)(totalItems - maxStackingLine));
		}
	}

	#endregion

	#region Snap points related

	public bool GetRegularSnapPoints(
		out float pNearOffset,
		out float pFarOffset,
		out float pSpacing)
	{
		bool hasRegularSnapPoints = false;
		pNearOffset = 0.0f;
		pFarOffset = 0.0f;
		pSpacing = 0.0f;

		if (!IsGrouping)
		{
			hasRegularSnapPoints = true;
			pNearOffset = SizeInVirtualizingDirection(GetGroupPaddingAtStart());
			pFarOffset = SizeInVirtualizingDirection(GetGroupPaddingAtEnd());

			if (m_cellSizeSet)
			{
				pSpacing = SizeInVirtualizingDirection(m_cellSize);
			}
			else
			{
				pSpacing = 0.0f;
			}
		}

		return hasRegularSnapPoints;
	}

	public bool HasIrregularSnapPoints(
		ElementType elementType)
	{
		// Non-grouped IWG always reports regular snap points.
		// Grouped IWG always snaps to headers and containers.
		return IsGrouping;
	}

	#endregion

	public Rect GetElementTransitionsBounds(
		ElementType elementType,
		int elementIndex,
		Rect windowConstraint)
	{
		// Required sizes set
		global::System.Diagnostics.Debug.Assert(m_cellSizeSet && (!IsGrouping || m_headerSizeSet));

		Rect returnValue = default;
		int itemCount;

		if (elementType == ElementType.ItemContainer)
		{
			itemCount = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
		}
		else
		{
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromItemIndex(
				elementIndex,
				out _ /* pIndexOfGroup */,
				out _ /* pIndexInGroup */,
				out itemCount);
		}

		int stackingLines = Math.Min(DetermineMaxStackingLine(windowConstraint), itemCount);

		SetPointFromRectInNonVirtualizingDirection(ref returnValue, GetItemStackingPosition(0));
		SetSizeFromRectInNonVirtualizingDirection(ref returnValue, stackingLines * SizeInNonVirtualizingDirection(m_cellSize));

		return returnValue;
	}

	public double GetItemsPerPage(
		Rect window)
	{
		double itemsPerPage = 0.0;

		if (m_cellSizeSet &&
			!DoubleUtil.IsNaN(m_cellSize.Width) &&
			!DoubleUtil.IsNaN(m_cellSize.Height) &&
			DoubleUtil.GreaterThan(m_cellSize.Width, 0) &&
			DoubleUtil.GreaterThan(m_cellSize.Height, 0))
		{
			int rows = (int)DoubleUtil.Ceil((float)window.Height / (float)m_cellSize.Height);
			int cols = (int)DoubleUtil.Ceil((float)window.Width / (float)m_cellSize.Width);
			itemsPerPage = rows * cols;
		}

		return itemsPerPage;
	}

	private void DetermineLineInformation(
		Rect windowConstraint,
		int indexInGroup,
		out int pStackingLines,
		out int pVirtualizingLine,
		out int pStackingLine)
	{
		// a wrapgrid can deterministically calculate where the bounds are
		int stackingLines = DetermineMaxStackingLine(windowConstraint);
		int virtualizingLine = indexInGroup / stackingLines;
		int stackingLine = indexInGroup - virtualizingLine * stackingLines;

		pStackingLines = stackingLines;
		pVirtualizingLine = virtualizingLine;
		pStackingLine = stackingLine;
	}

	private int DetermineMaxStackingLine(Rect windowConstraint)
	{
		global::System.Diagnostics.Debug.Assert((!IsGrouping || m_headerSizeSet) && m_cellSizeSet);

		float availableStackingExtent = SizeFromRectInNonVirtualizingDirection(windowConstraint);

		// Account for the padding
		availableStackingExtent -= (SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()) + SizeInNonVirtualizingDirection(GetGroupPaddingAtEnd()));

		if (GroupHeaderStrategy == GroupHeaderStrategy.Parallel)
		{
			availableStackingExtent -= SizeInNonVirtualizingDirection(m_headerSize);
		}

		availableStackingExtent = Math.Max(availableStackingExtent, 0.0f);

		// The available size may be infinity, so to avoid overflow issues, do this calculation
		// in floating-point, casting back to an integer on the way out.
		float maxStackingLineFloat = availableStackingExtent / SizeInNonVirtualizingDirection(m_cellSize);
		maxStackingLineFloat = Math.Max(1.0f, maxStackingLineFloat);   // a minimum of 1 row, in case the window constraint was smaller than the cellsize
		if (m_maxRowsOrColumns > 0)
		{
			maxStackingLineFloat = Math.Min(maxStackingLineFloat, (float)m_maxRowsOrColumns);
		}
		maxStackingLineFloat = Math.Min(50000.0f, maxStackingLineFloat); // hardcoded maximum. This has been discussed with the team

		return (int)maxStackingLineFloat;
	}

	private float GetDistanceBetweenGroups()
	{
		// if we are laying out vertically, we are wrapping rows
		// and our groups will be horizontally aligned
		return SizeInVirtualizingDirection(GetGroupPaddingAtStart()) +
			SizeInVirtualizingDirection(GetGroupPaddingAtEnd());
	}

	private float GetVirtualizedGroupExtent(int itemsInGroup, int maxStackingLine, float headerExtent)
	{
		float result = GetVirtualizedExtentOfItems(itemsInGroup, maxStackingLine);

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

	private float GetAverageHeaderExtent()
	{
		// TODO: Track an actual average. Just return the first one for now
		global::System.Diagnostics.Debug.Assert(m_headerSizeSet);
		return SizeInVirtualizingDirection(m_headerSize);
	}

	private float GetVirtualizedExtentOfItems(int itemCount, int maxStackingLine)
	{
		global::System.Diagnostics.Debug.Assert(m_cellSizeSet || itemCount == 0);
		global::System.Diagnostics.Debug.Assert(maxStackingLine > 0);

		// Get virtualizing lines, rounding up the fractional ones
		float extent = 0;
		if (itemCount > 0)
		{
			int virtualizingLines = (itemCount + (maxStackingLine - 1)) / maxStackingLine;
			extent += SizeInVirtualizingDirection(m_cellSize) * virtualizingLines;
		}

		return extent;
	}

	private float GetItemStackingPosition(int stackingLine)
	{
		global::System.Diagnostics.Debug.Assert(m_cellSizeSet);

		float result = stackingLine * SizeInNonVirtualizingDirection(m_cellSize);
		result += SizeInNonVirtualizingDirection(GetGroupPaddingAtStart());

		if (GroupHeaderStrategy == GroupHeaderStrategy.Parallel)
		{
			global::System.Diagnostics.Debug.Assert(m_headerSizeSet);
			result += SizeInNonVirtualizingDirection(m_headerSize);
		}

		return result;
	}

	public void SetItemWidth(double itemWidth)
	{
		m_itemWidthFromPanel = itemWidth;
		m_cellSize.Width = (float)itemWidth;

		// if we got a new itemWidth which is NAN,
		// make sure we re-measure the first item again next
		// time around. Caller needs to call InvalidateMeasure
		// to make sure this happens.
		m_cellSizeSet = IsItemSizePropertySet();
	}

	public void SetItemHeight(double itemHeight)
	{
		m_itemHeightFromPanel = itemHeight;
		m_cellSize.Height = (float)itemHeight;

		// if we got a new itemHeight which is NAN,
		// make sure we re-measure the first item again next
		// time around. Caller needs to call InvalidateMeasure
		// to make sure this happens.
		m_cellSizeSet = IsItemSizePropertySet();
	}

	private bool IsItemWidthPropertySet()
	{
		return !double.IsNaN(m_itemWidthFromPanel) && m_itemWidthFromPanel > 0;
	}

	private bool IsItemHeightPropertySet()
	{
		return !double.IsNaN(m_itemHeightFromPanel) && m_itemHeightFromPanel > 0;
	}

	private bool IsItemSizePropertySet()
	{
		return IsItemWidthPropertySet() && IsItemHeightPropertySet();
	}

	public void InvalidateGroupCache()
	{
		m_cachedGroupLocations.Clear();
		m_cachedStackingLines = 0;
	}

	public void RegisterSpecialContainerSize(int itemIndex, Size containerDesiredSize)
	{
		global::System.Diagnostics.Debug.Assert(itemIndex == c_specialItemIndex);
		containerDesiredSize.Width = Math.Max((float)containerDesiredSize.Width, 1.0f);
		containerDesiredSize.Height = Math.Max((float)containerDesiredSize.Height, 1.0f);

		if (!IsItemWidthPropertySet())
		{
			m_cellSize.Width = containerDesiredSize.Width;
		}

		if (!IsItemHeightPropertySet())
		{
			m_cellSize.Height = containerDesiredSize.Height;
		}

		m_cellSizeSet = true;
	}

	public void RegisterSpecialHeaderSize(int groupIndex, Size headerDesiredSize)
	{
		global::System.Diagnostics.Debug.Assert(groupIndex == c_specialGroupIndex);
		headerDesiredSize.Width = Math.Max((float)headerDesiredSize.Width, 1.0f);
		headerDesiredSize.Height = Math.Max((float)headerDesiredSize.Height, 1.0f);
		m_headerSize = headerDesiredSize;
		m_headerSizeSet = true;
	}

	//
	// Private
	//

	private Rect GetContainerBounds(
		int indexInItems,
		int indexInGroup,
		Size containerDesiredSize,
		LayoutReference referenceInformation,
		Rect windowConstraint)
	{
		Rect bounds = default;
		int stackingLines = 0;
		int stackingLine = 0;
		int virtualizingLine = 0;

		// Required sizes set
		global::System.Diagnostics.Debug.Assert(m_cellSizeSet && (!IsGrouping || m_headerSizeSet));

		// input
		float distanceBetweenGroups = GetDistanceBetweenGroups();
		DetermineLineInformation(windowConstraint, indexInGroup, out stackingLines, out virtualizingLine, out stackingLine);

		if (!IsGrouping)
		{
			// If nongrouping, this is exact and we don't even need the reference position
			SetPointFromRectInVirtualizingDirection(ref bounds, SizeInVirtualizingDirection(GetGroupPaddingAtStart()) +
				(virtualizingLine * SizeInVirtualizingDirection(m_cellSize)));
		}
		else
		{
			if (referenceInformation.ReferenceIsHeader)
			{
				// mismatch
				global::System.Diagnostics.Debug.Assert(referenceInformation.RelativeLocation != ReferenceIdentity.Myself);

				if (referenceInformation.RelativeLocation == ReferenceIdentity.BeforeMe)
				{
					global::System.Diagnostics.Debug.Assert(indexInGroup == 0); // if not, this is an invalid calculation

					// parallel layout: so take the left edge of the header and the bottom to start off
					SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
					if (GroupHeaderStrategy == GroupHeaderStrategy.Inline)
					{
						// If our headers are inline, let's make room for the size of the header as well
						SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(bounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
					}
				}
				else
				{
					// I'm going to be the last container in some new group that we will also be creating
					SetPointFromRectInVirtualizingDirection(ref bounds,
						PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) - SizeInVirtualizingDirection(m_cellSize) - distanceBetweenGroups);
				}
			}
			else
			{
				// we got a sibling container
				if (referenceInformation.RelativeLocation == ReferenceIdentity.BeforeMe
					&& stackingLine == 0)
				{
					// new stacking line
					SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeInVirtualizingDirection(m_cellSize));
				}
				else if (referenceInformation.RelativeLocation == ReferenceIdentity.AfterMe
					&& stackingLine == stackingLines - 1)
				{
					// previous stackingline
					SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) - SizeInVirtualizingDirection(m_cellSize));
				}
				else
				{
					// same column
					SetPointFromRectInVirtualizingDirection(ref bounds, PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds));
				}
			}
		}

		SetPointFromRectInNonVirtualizingDirection(ref bounds, GetItemStackingPosition(stackingLine));
		bounds.Width = m_cellSize.Width;
		bounds.Height = m_cellSize.Height;

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

		// TODO: Both the panel and the layout need to be aware that regardless of "special" status, header 0 and container 0 need to be placed in a specific location

		// the group padding logic:
		// When we are asked to give a total size, that should include the padding above and below us.
		// we will make sure to apply padding in between groups

		// Required sizes set
		global::System.Diagnostics.Debug.Assert(IsGrouping && m_headerSizeSet);

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
						if (DirectUI.RectUtil.GetIsEmpty(referenceInformation.HeaderBounds))
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
						if (DirectUI.RectUtil.GetIsEmpty(referenceInformation.HeaderBounds))
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
		SetSizeFromRectInNonVirtualizingDirection(ref bounds, SizeInNonVirtualizingDirection(headerDesiredSize));    // not constrained

		return bounds;
	}

	// Estimate how many items we need to traverse to get from our reference point to a suitable anchor item for the window.
	private void EstimateItemIndexFromWindow(
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window,
		out Rect pTargetRect,
		out int result)
	{
		pTargetRect = default;

		int totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

		int maxStackingLines = (totalItems > 0) ? DetermineMaxStackingLine(window) : 1;
		result = -1;

		if (!IsGrouping)
		{
			// If we are non-grouped, this is an exact calculation
			global::System.Diagnostics.Debug.Assert(maxStackingLines > 0);
			global::System.Diagnostics.Debug.Assert(m_cellSizeSet);

			// How many virtualizing lines does it take to get from the start to here?
			float distanceFromStart = Math.Max(0.0f, PointFromRectInVirtualizingDirection(window) - SizeInVirtualizingDirection(GetGroupPaddingAtStart()));
			int virtualizingLineDistance = (int)(distanceFromStart / SizeInVirtualizingDirection(m_cellSize));

			// Calculate the index and clamp to valid range
			int targetIndex = virtualizingLineDistance * maxStackingLines;
			targetIndex = Math.Max(0, targetIndex);
			targetIndex = Math.Min(targetIndex, totalItems - 1);

			// With the final index, calculate its position
			int ignored;
			int virtualizingLine;
			int stackingLine;
			DetermineLineInformation(window, targetIndex, out ignored, out virtualizingLine, out stackingLine);

			result = targetIndex;
			SetPointFromRectInVirtualizingDirection(ref pTargetRect, virtualizingLine * SizeInVirtualizingDirection(m_cellSize)
				+ SizeInVirtualizingDirection(GetGroupPaddingAtStart()));
			SetPointFromRectInNonVirtualizingDirection(ref pTargetRect, GetItemStackingPosition(stackingLine));
		}
		else
		{
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
					headerExtent = GetAverageHeaderExtent();
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

			int virtualizingLinesDelta = 0;
			global::System.Diagnostics.Debug.Assert(m_cellSizeSet);
			float itemExtent = Math.Max(1.0f, SizeInVirtualizingDirection(m_cellSize));

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
							virtualizingLinesDelta = (int)MathF.Floor(distance / itemExtent);
						}
						else
						{
							virtualizingLinesDelta = 0;
						}
					}
					break;

				case RelativePosition.After:
					{
						// Gather information about the distance to traverse
						float nearWindowEdge = PointFromRectInVirtualizingDirection(window) + SizeFromRectInVirtualizingDirection(window);
						float distance = nearWindowEdge - virtualizedReferencePoint;
						if (distance < 0 && !referenceIsHeader)
						{
							virtualizingLinesDelta = (int)MathF.Floor(distance / itemExtent);
						}
						else
						{
							// If we end up estimating backwards from a header, just use the first item in the group
							virtualizingLinesDelta = 0;
						}
					}
					break;

				default:
					virtualizingLinesDelta = 0;
					break;
			}

			// Now that we know how many virtualizing lines to traverse, let's get an actual item delta
			int targetStackingLine = 0;
			if (virtualizingLinesDelta != 0)
			{
				// Figure out how many items our line delta takes us
				int targetItemIndex = referenceIndex + (maxStackingLines * virtualizingLinesDelta);

				// Let's just default to the first element in the target stack
				int referenceStackingLine;
				int referenceVirtualizingLine;
				int ignored;
				DetermineLineInformation(window, referenceIndexInGroup, out ignored, out referenceVirtualizingLine, out referenceStackingLine);
				targetItemIndex -= referenceStackingLine;

				// Bind the target item to the total item size
				targetItemIndex = Math.Max(0, targetItemIndex);
				targetItemIndex = Math.Min(targetItemIndex, totalItems - 1);

				// Now, we'll just make sure that in case we clipped the item index, we have the correct virtualizing offset
				int targetVirtualizingLine;
				int targetIndexInGroup = targetItemIndex - referenceIndex + referenceIndexInGroup;
				DetermineLineInformation(window, targetIndexInGroup, out ignored, out targetVirtualizingLine, out targetStackingLine);
				virtualizingLinesDelta = targetVirtualizingLine - referenceVirtualizingLine;
				calculatedPosition = virtualizedReferencePoint + (virtualizingLinesDelta * itemExtent);
				result = targetItemIndex;
			}
			else
			{
				// We'll just re-use the reference element
				int ignored;
				DetermineLineInformation(window, referenceIndexInGroup, out ignored, out ignored, out targetStackingLine);
				calculatedPosition = virtualizedReferencePoint;
				result = referenceIndex;
			}

			SetPointFromRectInVirtualizingDirection(ref pTargetRect, calculatedPosition);
			SetPointFromRectInNonVirtualizingDirection(ref pTargetRect, GetItemStackingPosition(targetStackingLine));
		}

		pTargetRect.Width = m_cellSize.Width;
		pTargetRect.Height = m_cellSize.Height;
	}

	// Estimate how many groups we need to traverse to get from our reference point to a suitable
	// anchor group for the window, using items-per-group to estimate an average group extent.
	private void EstimateGroupIndexFromWindow(
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window,
		out Rect pTargetRect,
		out int targetGroupIndex)
	{
		int totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
		int totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

		global::System.Diagnostics.Debug.Assert(0 < totalGroups);

		int maxStackingLines = (totalItems > 0) ? DetermineMaxStackingLine(window) : 1;

		float averageHeaderExtent = GetAverageHeaderExtent();
		float referenceHeaderSize = (SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) > 0) ?
			SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) : averageHeaderExtent;

		float nearWindowEdge = PointFromRectInVirtualizingDirection(window);
		float farWindowEdge = nearWindowEdge + SizeFromRectInVirtualizingDirection(window);
		global::System.Diagnostics.Debug.Assert(nearWindowEdge <= farWindowEdge);

		float calculatedPosition = PointFromRectInVirtualizingDirection(headerReference.ElementBounds);
		targetGroupIndex = headerReference.ElementIndex;

		if (calculatedPosition + referenceHeaderSize < nearWindowEdge)
		{
			bool found = false;
			// Our header is before of the window
			// Need to walk forward to reach it, making sure we step back if a large group overshoots the window entirely
			// To perform the backtrack in this case, we first calculate a "candidate position", accepting it only if we didn't overshoot
			int itemsInGroup;
			GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
				targetGroupIndex,
				out _ /* pStartItemIndex */,
				out itemsInGroup);

			// Since we have this header's size and group count, let's just use it first before jumping off into averages
			float candidatePosition = calculatedPosition + GetVirtualizedGroupExtent(itemsInGroup, maxStackingLines, referenceHeaderSize);

			// Bail out if we encounter the last group, as we can't go farther than that.
			while (!found && targetGroupIndex + 1 < totalGroups)
			{
				if (farWindowEdge < candidatePosition)
				{
					// That group was big! We overshot the window, so let's not move to the next group and use the current one
					found = true;
				}
				else if (candidatePosition + averageHeaderExtent < nearWindowEdge)
				{
					// We're going to move to the next group and continue
					++targetGroupIndex;
					calculatedPosition = candidatePosition;
					GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
						targetGroupIndex,
						out _ /* pStartItemIndex */,
						out itemsInGroup);
					candidatePosition = calculatedPosition + GetVirtualizedGroupExtent(itemsInGroup, maxStackingLines, averageHeaderExtent);
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

				calculatedPosition -= GetVirtualizedGroupExtent(itemsInGroup, maxStackingLines, averageHeaderExtent);

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
			SetSizeFromRectInVirtualizingDirection(ref targetRect, GetAverageHeaderExtent());
		}
		SetSizeFromRectInNonVirtualizingDirection(ref targetRect, SizeInNonVirtualizingDirection(m_headerSize));
		pTargetRect = targetRect;
	}

	// Estimate the location of an anchor item, given an item index
	private Rect EstimateContainerLocation(
		int targetItemIndex,
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window)
	{
		int totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

		global::System.Diagnostics.Debug.Assert(0 <= targetItemIndex && targetItemIndex < totalItems);

		global::System.Diagnostics.Debug.Assert(m_cellSizeSet);

		Rect result = default;

		// Put some sanity bounds on the item index
		targetItemIndex = Math.Max(0, targetItemIndex);
		targetItemIndex = Math.Min(targetItemIndex, totalItems - 1);

		// If we are nongrouped, we can do an exact calculation here and short-circuit
		if (!IsGrouping)
		{
			int maxStackingLines;
			int virtualizingLine;
			int stackingLine;

			DetermineLineInformation(window, targetItemIndex, out maxStackingLines, out virtualizingLine, out stackingLine);
			global::System.Diagnostics.Debug.Assert(maxStackingLines > 0);

			SetPointFromRectInVirtualizingDirection(ref result, virtualizingLine * SizeInVirtualizingDirection(m_cellSize)
				+ SizeInVirtualizingDirection(GetGroupPaddingAtStart()));

			SetPointFromRectInNonVirtualizingDirection(ref result, GetItemStackingPosition(stackingLine));
		}
		else
		{
			// Determine which reference we will use
			// For now, if both are provided, we only use the header
			Rect referenceRect;
			int referenceIndex;
			int referenceIndexInGroup; // Used to determine virtualizing and stacking line locations
			float headerAdjustment;
			bool referenceIsHeader;
			if (headerReference.ElementIndex == -1)
			{
				// If using an item as our reference, the indexInGroup is the same as the item index
				referenceIndex = containerReference.ElementIndex;
				referenceIndexInGroup = referenceIndex;
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
				referenceIndexInGroup = 0; // Treat the header as being item 0 in the group
				referenceRect = headerReference.ElementBounds;
				float headerExtent = SizeFromRectInVirtualizingDirection(headerReference.ElementBounds);
				if (headerExtent <= 0)
				{
					headerExtent = GetAverageHeaderExtent();
				}
				// If we have inline headers, take the size into account before we start counting items
				if (GroupHeaderStrategy == GroupHeaderStrategy.Inline && targetItemIndex >= referenceIndex)
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

			// Get our virtualizing/stacking line info for both the reference and the target index
			int ignored;
			int referenceVirtualizingLine;
			int targetVirtualizingLine;
			int targetStackingLine;
			int targetIndexInGroup = targetItemIndex - referenceIndex + referenceIndexInGroup;
			DetermineLineInformation(window, referenceIndexInGroup, out ignored, out referenceVirtualizingLine, out ignored);
			DetermineLineInformation(window, targetIndexInGroup, out ignored, out targetVirtualizingLine, out targetStackingLine);
			int virtualizingLineDelta = targetVirtualizingLine - referenceVirtualizingLine;

			SetPointFromRectInVirtualizingDirection(ref result, virtualizedReferencePoint + (virtualizingLineDelta * SizeInVirtualizingDirection(m_cellSize)));
			SetPointFromRectInNonVirtualizingDirection(ref result, GetItemStackingPosition(targetStackingLine));
		}

		result.Width = m_cellSize.Width;
		result.Height = m_cellSize.Height;

		return result;
	}

	// Estimate the location of an anchor group, using items-per-group to estimate an average group extent.
	private Rect EstimateHeaderLocation(
		int targetGroupIndex,
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window)
	{
		Rect targetRect = default;

		int totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
		int totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

		global::System.Diagnostics.Debug.Assert(0 < totalGroups);
		global::System.Diagnostics.Debug.Assert(0 <= targetGroupIndex && targetGroupIndex < (int)totalGroups);

		int maxStackingLines = (totalItems > 0) ? DetermineMaxStackingLine(window) : 1;
		float referenceHeaderSize = (SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) > 0) ?
			SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) : GetAverageHeaderExtent();

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
				// We're using this item to estimate the placement of an adjacent header
				int itemIndexInGroup = containerReference.ElementIndex - firstItemInGroup;
				float itemReference = PointFromRectInVirtualizingDirection(containerReference.ElementBounds);

				if (targetGroupIndex == headerReference.ElementIndex)
				{
					// We're looking for the header immediately before our item
					int virtualizingLinesDelta = -(itemIndexInGroup / maxStackingLines);
					calculatedPosition = itemReference + virtualizingLinesDelta * SizeInVirtualizingDirection(m_cellSize);

					if (GroupHeaderStrategy == GroupHeaderStrategy.Inline)
					{
						// Make room for an inline header
						calculatedPosition -= referenceHeaderSize;
					}
				}
				else
				{
					// We're looking for the header immediately after our item
					int virtualizingLinesDelta = ((itemCountInGroup - 1) / maxStackingLines) - (itemIndexInGroup / maxStackingLines);
					calculatedPosition = itemReference + virtualizingLinesDelta * SizeInVirtualizingDirection(m_cellSize) + GetDistanceBetweenGroups();
				}
			}
			else
			{
				// We're using this header to estimate the placement of an adjacent header
				calculatedPosition = PointFromRectInVirtualizingDirection(headerReference.ElementBounds);
				if (targetGroupIndex == headerReference.ElementIndex + 1)
				{
					calculatedPosition += GetVirtualizedGroupExtent(itemCountInGroup, maxStackingLines, referenceHeaderSize);
				}
			}
		}
		else
		{
			// Going to iterate through the group sizes and use them to come up with an estimate
			float averageHeaderExtent = GetAverageHeaderExtent();
			float virtualizedReferencePoint = PointFromRectInVirtualizingDirection(headerReference.ElementBounds);
			calculatedPosition = virtualizedReferencePoint;

			// Put a sanity bound on the group index, for FRE builds
			targetGroupIndex = Math.Max(0, targetGroupIndex);
			targetGroupIndex = Math.Min(targetGroupIndex, (int)(totalGroups - 1));

			int currentGroupIndex = headerReference.ElementIndex;

			// If we're going forward, we have this group header's actual size, so we may as well use it
			if (currentGroupIndex < targetGroupIndex)
			{
				int itemCountInGroup;
				GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
					headerReference.ElementIndex,
					out _ /* pStartItemIndex */,
					out itemCountInGroup);

				calculatedPosition += GetVirtualizedGroupExtent(itemCountInGroup, maxStackingLines, referenceHeaderSize);
				++currentGroupIndex;
			}

			while (currentGroupIndex < targetGroupIndex)
			{
				int itemsInGroup;
				GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
					currentGroupIndex,
					out _ /* pStartItemIndex */,
					out itemsInGroup);
				calculatedPosition += GetVirtualizedGroupExtent(itemsInGroup, maxStackingLines, averageHeaderExtent);
				++currentGroupIndex;
			}

			while (currentGroupIndex > targetGroupIndex)
			{
				int itemsInGroup;
				GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
					currentGroupIndex,
					out _ /* pStartItemIndex */,
					out itemsInGroup);
				calculatedPosition -= GetVirtualizedGroupExtent(itemsInGroup, maxStackingLines, averageHeaderExtent);
				--currentGroupIndex;
			}
		}

		SetPointFromRectInVirtualizingDirection(ref targetRect, calculatedPosition);
		SetPointFromRectInNonVirtualizingDirection(ref targetRect, SizeInNonVirtualizingDirection(GetGroupPaddingAtStart()));
		if (targetGroupIndex == headerReference.ElementIndex)
		{
			SetSizeFromRectInVirtualizingDirection(ref targetRect, referenceHeaderSize);
		}
		else
		{
			SetSizeFromRectInVirtualizingDirection(ref targetRect, GetAverageHeaderExtent());
		}
		SetSizeFromRectInNonVirtualizingDirection(ref targetRect, SizeInNonVirtualizingDirection(m_headerSize));

		return targetRect;
	}

	private Size EstimateNonGroupedExtent(
		EstimationReference containerReference,
		Rect windowConstraint)
	{
		int totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

		// expects to have a cellsize
		global::System.Diagnostics.Debug.Assert(m_cellSizeSet);

		Size extent = default;

		int maxStackingLine = DetermineMaxStackingLine(windowConstraint);
		global::System.Diagnostics.Debug.Assert(maxStackingLine > 0);

		// All the containers are the same size, so this is easy
		SetSizeInVirtualizingDirection(ref extent, GetVirtualizedExtentOfItems(totalItems, maxStackingLine) +
			SizeInVirtualizingDirection(GetGroupPaddingAtStart()) + SizeInVirtualizingDirection(GetGroupPaddingAtEnd()));
		SetSizeInNonVirtualizingDirection(ref extent, GetItemStackingPosition(Math.Min(maxStackingLine, totalItems)) + SizeInNonVirtualizingDirection(GetGroupPaddingAtEnd()));

		return extent;
	}

	private Size EstimateGroupedExtent(
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect windowConstraint)
	{
		Size extent = default;
		int totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
		int totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

		float referenceHeaderExtent = (SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) > 0) ?
			SizeFromRectInVirtualizingDirection(headerReference.ElementBounds) : GetAverageHeaderExtent();

		int maxStackingLines = (totalItems > 0) ? DetermineMaxStackingLine(windowConstraint) : 1;

		EnsureGroupCache(maxStackingLines);

		int itemCountInGroup;
		GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
			headerReference.ElementIndex,
			out _ /* pStartItemIndex */,
			out itemCountInGroup);

		// The last realized element is a header, so we just do group extent estimates from here
		// Starting point is the header location, minus the left/top padding
		float referencePoint = PointFromRectInVirtualizingDirection(headerReference.ElementBounds) - SizeInVirtualizingDirection(GetGroupPaddingAtStart());
		float endOfCurrentGroup = referencePoint + GetVirtualizedGroupExtent(itemCountInGroup, maxStackingLines, referenceHeaderExtent);

		// Start with our initial guess, based on average header sizes, and adjust from there
		global::System.Diagnostics.Debug.Assert(m_cachedGroupLocations.Count != 0);
		float estimatedExtent = m_cachedGroupLocations[m_cachedGroupLocations.Count - 1];

		// Our cached estimates were based on estimated header sizes. Now that we've laid out to this point, we can see the difference
		// between where we thought we'd be versus where we actually are, and apply that difference to where we think the last group
		// will be. In other words, we'll keep te estimates of the as-yet unrealized groups, and add them to our current location
		float estimatedEndOfCurrentGroup = m_cachedGroupLocations[headerReference.ElementIndex];
		estimatedExtent += (endOfCurrentGroup - estimatedEndOfCurrentGroup);

		SetSizeInVirtualizingDirection(ref extent, estimatedExtent);
		if (totalItems > 0)
		{
			SetSizeInNonVirtualizingDirection(ref extent, GetItemStackingPosition(Math.Min(maxStackingLines, totalItems)) + SizeInNonVirtualizingDirection(GetGroupPaddingAtEnd()));
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(m_headerSizeSet);
			SetSizeInNonVirtualizingDirection(ref extent, SizeInNonVirtualizingDirection(m_headerSize) + SizeInNonVirtualizingDirection(GetGroupPaddingAtEnd()));
		}

		return extent;
	}

	// Maintain a cache of where all groups' extent would end, so that we don't have to walk them all every frame
	private void EnsureGroupCache(
		int maxStackingLines)
	{
		int groupCount = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

		if (groupCount != (int)m_cachedGroupLocations.Count ||
			maxStackingLines != m_cachedStackingLines ||
			!DoubleUtil.AreClose(m_cellSize.Width, m_cachedCellSize.Width) ||
			!DoubleUtil.AreClose(m_cellSize.Height, m_cachedCellSize.Height))
		{
			m_cachedStackingLines = maxStackingLines;
			m_cachedCellSize = m_cellSize;

			m_cachedGroupLocations.Clear();
			m_cachedGroupLocations.EnsureCapacity(groupCount);

			float accumulatedPosition = 0;

			float averageHeaderSize = GetAverageHeaderExtent();

			for (int i = 0; i < groupCount; ++i)
			{
				int itemsInGroup;
				GetLayoutDataInfoProviderNoRef.GetGroupInformationFromGroupIndex(
					i,
					out _ /* pStartItemIndex */,
					out itemsInGroup);
				accumulatedPosition += GetVirtualizedGroupExtent(itemsInGroup, maxStackingLines, averageHeaderSize);
				m_cachedGroupLocations.Add(accumulatedPosition);
			}
		}
	}
}
