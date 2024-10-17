// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


// Work around disruptive max/min macros
#undef max
#undef min

using System;
using Windows.Foundation;
using DirectUI;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarLayoutStrategyImpl
	{
		public CalendarLayoutStrategyImpl()
			: base(false, true /* isWrapping */)
		{
			m_cellSize = new Size(1, 1);
			m_cellMinSize = new Size(0, 0);
			m_rows = 1;
			m_cols = 1;
		}

		#region Layout related methods

		// returns the size we should use to measure a container or header with
		// itemIndex - indicates an index of valid item or -1 for general, non-special items
		public Size GetElementMeasureSize(
				ElementType elementType,
				int elementIndex,
				Rect windowConstraint)
		{
			if (elementType == ElementType.ItemContainer)
			{
				return m_cellSize;
			}
			else
			{
				// we don't support header but MCBP still asks for header size.
				return new Size(-1, -1);
			}
		}

		public Rect
			GetElementBounds(
				ElementType elementType,
				int elementIndex,
				Size containerDesiredSize,
				LayoutReference referenceInformation,
				Rect windowConstraint)
		{
			global::System.Diagnostics.Debug.Assert(elementType == ElementType.ItemContainer);

			// Nongrouping, this is exact and we don't even need the reference position
			return GetItemBounds(elementIndex);
		}

		public Rect
			GetElementArrangeBounds(
				ElementType elementType,
				int elementIndex,
				Rect containerBounds,
				Rect windowConstraint,
				Size finalSize)
		{
			global::System.Diagnostics.Debug.Assert(elementType == ElementType.ItemContainer);
			// For calendar item, the arrange bound is same as measure bound so we can always call GetItemBounds
			// to get the arrange bounds, but we can simply use measure bound (containerBounds) because they are same.

			// However for garbage we can't call GetItemBounds because garbage index is -1 which will cause the 
			// rect computed from GetItemBounds is just one item rect away from the first item. (when we pan fast or when
			// the first item is not at {0,0}, we can see the garbage).
			// fortunately for garbage we can use measure bound (containerBounds) as its arrange bound as well, because
			// garbage item's measure bound is far away from visible window and we don't care the arrange size of a garbage item.
#if DEBUG
			if (elementIndex >= 0)
			{
				// make sure the measure bound is arrange bound for non-garbage.
				var bounds = GetItemBounds(elementIndex);
				global::System.Diagnostics.Debug.Assert(bounds.X == containerBounds.X
					&& bounds.Y == containerBounds.Y
					&& bounds.Width == containerBounds.Width
					&& bounds.Height == containerBounds.Height);
			}
#endif

			return containerBounds;
		}

		public bool ShouldContinueFillingUpSpace(
			ElementType elementType,
			int elementIndex,
			LayoutReference referenceInformation,
			Rect windowToFill)
		{
			bool shouldContinue = false;

			global::System.Diagnostics.Debug.Assert(elementType == ElementType.ItemContainer);

			if (referenceInformation.RelativeLocation == ReferenceIdentity.Myself)
			{
				// always do yourself
				shouldContinue = true;
			}
			else
			{
				int stackingLines = 0;
				int virtualizingLine = 0;
				int stackingLine = 0;

				int visualIndex = m_indexCorrectionTable.ActualIndexToVisualIndex(elementIndex);
				DetermineLineInformation(visualIndex, out stackingLines, out virtualizingLine, out stackingLine);

				if (referenceInformation.RelativeLocation == ReferenceIdentity.BeforeMe)
				{
					if (stackingLine == 0)
					{
						// we're in a new row, so is there room to the bottom?
						shouldContinue = PointFromRectInVirtualizingDirection(windowToFill) + SizeFromRectInVirtualizingDirection(windowToFill)
							> PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds) + SizeFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
					}
					else
					{
						shouldContinue = PointFromRectInVirtualizingDirection(windowToFill) + SizeFromRectInVirtualizingDirection(windowToFill) > PointFromRectInVirtualizingDirection(referenceInformation.ReferenceBounds);
					}
				}
				else // AfterMe
				{
					if (stackingLine == stackingLines - 1)
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

			return shouldContinue;
		}

		#endregion

		#region Estimation and virtualization related methods.

		// Estimate how many items we need to traverse to get from our reference point to a suitable anchor item for the window.
		public void
			EstimateElementIndex(
				ElementType elementType,
				EstimationReference headerReference,
				EstimationReference containerReference,
				Rect window,
				out Rect pTargetRect,
				out int pReturnValue)
		{
			pTargetRect = default;
			pReturnValue = -1;
			int totalItems = 0;

			global::System.Diagnostics.Debug.Assert(elementType == ElementType.ItemContainer);
			totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();

			int maxStackingLines = (totalItems > 0) ? DetermineMaxStackingLine() : 1;

			// We are non-grouped, this is an exact calculation
			global::System.Diagnostics.Debug.Assert(maxStackingLines > 0);

			// How many virtualizing lines does it take to get from the start to here?
			float virtualizingDistanceFromStart = Math.Max(0.0f, PointFromRectInVirtualizingDirection(window));
			int virtualizingLineDistance = (int)(virtualizingDistanceFromStart / SizeInVirtualizingDirection(m_cellSize));

			float stackingDistanceFromStart = Math.Max(0.0f, PointFromRectInNonVirtualizingDirection(window));
			int stackingLineDistance = (int)(stackingDistanceFromStart / SizeInNonVirtualizingDirection(m_cellSize));

			int targetVisualIndex = virtualizingLineDistance * maxStackingLines + stackingLineDistance;

			// clamp targetvisualIndex.

			int firstVisualIndex = m_indexCorrectionTable.ActualIndexToVisualIndex(0);
			int lastVisualIndex = m_indexCorrectionTable.ActualIndexToVisualIndex(totalItems - 1);
			targetVisualIndex = Math.Min(targetVisualIndex, lastVisualIndex);
			targetVisualIndex = Math.Max(targetVisualIndex, firstVisualIndex);

			// With the final index, calculate its position
			int ignored;
			int virtualizingLine;
			int stackingLine;
			DetermineLineInformation(targetVisualIndex, out ignored, out virtualizingLine, out stackingLine);

			pReturnValue = m_indexCorrectionTable.VisualIndexToActualIndex(targetVisualIndex);

			SetPointFromRectInVirtualizingDirection(ref pTargetRect, virtualizingLine * SizeInVirtualizingDirection(m_cellSize));
			SetPointFromRectInNonVirtualizingDirection(ref pTargetRect, GetItemStackingPosition(stackingLine));

			return;
		}

		public void EstimateElementBounds(
			ElementType elementType,
			int elementIndex,
			EstimationReference headerReference,
			EstimationReference containerReference,
			Rect window,
			out Rect pReturnValue)
		{
			pReturnValue = default;

			int totalItems;

			global::System.Diagnostics.Debug.Assert(elementType == ElementType.ItemContainer);
			totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
			global::System.Diagnostics.Debug.Assert(0 <= elementIndex && elementIndex < totalItems);

			int maxStackingLines;
			int virtualizingLine;
			int stackingLine;

			var visualIndex = m_indexCorrectionTable.ActualIndexToVisualIndex(elementIndex);
			DetermineLineInformation(visualIndex, out maxStackingLines, out virtualizingLine, out stackingLine);
			global::System.Diagnostics.Debug.Assert(maxStackingLines > 0);

			SetPointFromRectInVirtualizingDirection(ref pReturnValue, virtualizingLine * SizeInVirtualizingDirection(m_cellSize));
			SetPointFromRectInNonVirtualizingDirection(ref pReturnValue, GetItemStackingPosition(stackingLine));
			pReturnValue.Width = m_cellSize.Width;
			pReturnValue.Height = m_cellSize.Height;

			return;
		}

		public void EstimatePanelExtent(
			EstimationReference lastHeaderReference,
			EstimationReference lastContainerReference,
			Rect windowConstraint,
			out Size pExtent)
		{
			pExtent = default;

			int totalItems = 0;
			totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();


			int maxStackingLine = DetermineMaxStackingLine();
			global::System.Diagnostics.Debug.Assert(maxStackingLine > 0);

			//actual panel size
			SetSizeInVirtualizingDirection(ref pExtent, GetVirtualizedExtentOfItems(totalItems, maxStackingLine));
			SetSizeInNonVirtualizingDirection(ref pExtent, GetItemStackingPosition(Math.Min(maxStackingLine, totalItems)));

			return;
		}

		#endregion

		#region IItemLookupPanel related

		// Based on current element's index/type and action, return the next element index/type.
		public void GetTargetIndexFromNavigationAction(
			ElementType elementType,
			int elementIndex,
			KeyNavigationAction action,
			Rect windowConstraint,
			out ElementType pTargetElementType,
			out int pTargetElementIndex)
		{
			int totalItems;
			int totalGroups;
			totalItems = GetLayoutDataInfoProviderNoRef.GetTotalItemCount();
			totalGroups = GetLayoutDataInfoProviderNoRef.GetTotalGroupCount();

			global::System.Diagnostics.Debug.Assert(0 <= elementIndex && elementIndex < totalItems);
			global::System.Diagnostics.Debug.Assert(elementType == ElementType.ItemContainer);

			pTargetElementType = ElementType.ItemContainer;

			if (action != KeyNavigationAction.Left &&
				action != KeyNavigationAction.Right &&
				action != KeyNavigationAction.Up &&
				action != KeyNavigationAction.Down)
			{
				throw new ArgumentException(nameof(action));
			}

			int step = (action == KeyNavigationAction.Left || action == KeyNavigationAction.Up) ? -1 : 1;
			pTargetElementIndex = elementIndex;

			// Easy case: the action is along layout orientation, therefore handle it.
			if ((VirtualizationDirection == Orientation.Vertical && (action == KeyNavigationAction.Left || action == KeyNavigationAction.Right)) ||
				(VirtualizationDirection == Orientation.Horizontal && (action == KeyNavigationAction.Up || action == KeyNavigationAction.Down)))
			{
				pTargetElementIndex = Math.Min(Math.Max(elementIndex + step, 0), totalItems - 1);
			}
			// The action is not in layout direction.  We must jump to the next item in the same row/column.
			// We are not grouping, easy.
			else
			{
				// TODO: verify startAt
				int maxLineLength = DetermineMaxStackingLine();
				int nextIndex = elementIndex + step * maxLineLength;
				if (0 <= nextIndex && nextIndex < totalItems)
				{
					pTargetElementIndex = nextIndex;
				}
			}

			global::System.Diagnostics.Debug.Assert(0 <= pTargetElementIndex && pTargetElementIndex < totalItems);

			return;
		}

		#endregion

		#region Snap points related

		public bool GetRegularSnapPoints(
			out float pNearOffset,
			out float pFarOffset,
			out float pSpacing)
		{
			pNearOffset = 0.0f;
			pFarOffset = 0.0f;
			pSpacing = SizeInVirtualizingDirection(m_cellSize);

			// when there is no snapPointFilterFunction, we have regular snap points.
			return m_snapPointFilterFunction is null; /*hasRegularSnapPoints */;
		}

		public bool HasIrregularSnapPoints(
			ElementType elementType)
		{
			return m_snapPointFilterFunction is null;
		}

		public void HasSnapPointOnElement(
			ElementType elementType,
			int elementIndex,
			out bool hasSnapPointOnElement)
		{
			hasSnapPointOnElement = false;
			global::System.Diagnostics.Debug.Assert(m_snapPointFilterFunction is { });

			hasSnapPointOnElement = m_snapPointFilterFunction(elementIndex);

			return;
		}

		#endregion

		private void DetermineLineInformation(int visualIndex, out int pStackingLines, out int pVirtualizingLine, out int pStackingLine)
		{
			int stackingLines = DetermineMaxStackingLine();

			int virtualizingLine = visualIndex / stackingLines;
			int stackingLine = visualIndex - virtualizingLine * stackingLines;

			pStackingLines = stackingLines;
			pVirtualizingLine = virtualizingLine;
			pStackingLine = stackingLine;
		}

		private int DetermineMaxStackingLine()
		{
			switch (VirtualizationDirection)
			{
				case Orientation.Horizontal:
					return m_rows;
				case Orientation.Vertical:
					return m_cols;
				default:
					global::System.Diagnostics.Debug.Assert(false);
					return 0;
			}
		}

		private float GetVirtualizedExtentOfItems(int itemCount, int maxStackingLine)
		{
			global::System.Diagnostics.Debug.Assert(maxStackingLine > 0);

			// Get virtualizing lines, rounding up the fractional ones
			float extent = 0;

			if (itemCount > 0)
			{
				int lastIndex = itemCount - 1;
				int lastVisualIndex = m_indexCorrectionTable.ActualIndexToVisualIndex(lastIndex);
				int virtualizingLines = lastVisualIndex / maxStackingLine + 1;
				extent += SizeInVirtualizingDirection(m_cellSize) * virtualizingLines;
			}

			return extent;
		}

		private float GetItemStackingPosition(int stackingLine)
		{
			float result = stackingLine * SizeInNonVirtualizingDirection(m_cellSize);

			return result;
		}

		// check if our items can perfectly fit in the given viewport
		// if not, we'll update the cell size and ask for an additional measure pass.
		public void SetViewportSize(Size size, out bool pNeedsRemeasure)
		{
			pNeedsRemeasure = false;

			float newWidth = (float)size.Width / m_cols;
			float newHeight = (float)size.Height / m_rows;


			// The newSize should be always greater than or equal to the minSize. 
			// However under some scale factors, we need to use "close to" to replace "equal to".
			float epsilon = 0.0001f;
			global::System.Diagnostics.Debug.Assert(newWidth > m_cellMinSize.Width || DoubleUtil.AreWithinTolerance(newWidth, m_cellMinSize.Width, epsilon));
			global::System.Diagnostics.Debug.Assert(newHeight > m_cellMinSize.Height || DoubleUtil.AreWithinTolerance(newHeight, m_cellMinSize.Height, epsilon));

			if (newWidth != m_cellSize.Width || newHeight != m_cellSize.Height)
			{
				// Always make sure that we do not end up with 0 width or height. That can
				// end up devirtualizing the entire calendarview.
				m_cellSize.Width = newWidth == 0 ? m_cellMinSize.Width : newWidth;
				m_cellSize.Height = newHeight == 0 ? m_cellMinSize.Height : newHeight;
				pNeedsRemeasure = true;
			}
		}

		// our Parent(SCP)'s  desired size is determined by the min cell size.
		public Size GetDesiredViewportSize()
		{
			Size desiredViewportSize = new Size(m_cellMinSize.Width * m_cols, m_cellMinSize.Height * m_rows);
			return desiredViewportSize;
		}

		public void SetItemMinimumSize(Size size, out bool pNeedsRemeasure)
		{
			pNeedsRemeasure = false;

			if (m_cellMinSize.Width != size.Width || m_cellMinSize.Height != size.Height)
			{
				m_cellMinSize = size;
				// once cell minsize is updated, we also update cell size and ask for an additional measure pass.
				m_cellSize = m_cellMinSize;
				pNeedsRemeasure = true;
			}
		}

		public Rect GetItemBounds(int index)
		{
			Rect bounds = default;
			int stackingLines = 0;
			int stackingLine = 0;
			int virtualizingLine = 0;

			int visualIndex = m_indexCorrectionTable.ActualIndexToVisualIndex(index);
			DetermineLineInformation(visualIndex, out stackingLines, out virtualizingLine, out stackingLine);

			SetPointFromRectInVirtualizingDirection(ref bounds, virtualizingLine * SizeInVirtualizingDirection(m_cellSize));
			SetPointFromRectInNonVirtualizingDirection(ref bounds, GetItemStackingPosition(stackingLine));
			bounds.Width = m_cellSize.Width;
			bounds.Height = m_cellSize.Height;

			return bounds;
		}

		partial struct IndexCorrectionTable
		{
			public void SetCorrectionEntryForSkippedDay(int index, int correction)
			{
				global::System.Diagnostics.Debug.Assert(index >= 0);
				global::System.Diagnostics.Debug.Assert(correction >= 0); // it is a skip day, so the correction must be non-negative number.
				m_indexCorrectionTable[1].first = index;
				m_indexCorrectionTable[1].second = correction;
			}

			public void SetCorrectionEntryForElementStartAt(int correction)
			{
				global::System.Diagnostics.Debug.Assert(correction >= 0);
				m_indexCorrectionTable[0].first = 0; // this is always 0, which means this correction applies for all items.
				m_indexCorrectionTable[0].second = correction;
			}

			public int VisualIndexToActualIndex(int visualIndex)
			{
				global::System.Diagnostics.Debug.Assert(m_indexCorrectionTable[0].first <= m_indexCorrectionTable[1].first); // always in order.
				int actualIndex = visualIndex;
				foreach (var entry in m_indexCorrectionTable)
				{
					if (actualIndex >= entry.first)
					{
						global::System.Diagnostics.Debug.Assert(actualIndex >= entry.first + entry.second);
						actualIndex -= entry.second;
					}
					else
					{
						break;
					}
				}

				return actualIndex;
			}

			public int ActualIndexToVisualIndex(int actualIndex)
			{
				global::System.Diagnostics.Debug.Assert(m_indexCorrectionTable[0].first <= m_indexCorrectionTable[1].first); // always in order.
				int visualIndex = actualIndex;
				foreach (var entry in m_indexCorrectionTable)
				{
					if (actualIndex >= entry.first)
					{
						visualIndex += entry.second;
					}
					else
					{
						break;
					}
				}
				return visualIndex;
			}
		}
	}
}
