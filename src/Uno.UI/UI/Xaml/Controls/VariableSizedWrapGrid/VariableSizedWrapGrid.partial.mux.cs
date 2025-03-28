// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using Windows.Foundation;
using DirectUI;

namespace Windows.UI.Xaml.Controls;

//  Abstract:
//      VariableSizeWrapGrid definition.
//      VariableSizeWrapGrid is used to create variable size tiles inside the grid.
//
//      It has OccupancyBlock and OccupancyMap nested classes
//      OccupancyMap: This class is used to arrange all items of variableSizeGrid
//      Since the size of OccupancyMap can be Infinite, OccupancyBlock is used to
//      allocate block of Memory
//
//      OccupncyMap always arrange items vertically(row), whether user sets
//      Orientation=Vertical or Horizontal. OccupancyMap's public APIs take care of
//      providing correct Orientation. Similarly OccupancyBlock has fixed rows while
//      it is growing on Column direction
//
//      Both classes are nested inside the VariableSizedWrapGrid class
partial class VariableSizedWrapGrid : IOrientedPanel
{
	private static int ToAdjustedIndex(int value) => value + 1;
	private static int FromAdjustedIndex(int value) => value - 1;

	/// <summary>
	/// Handle the custom property changed event and call the OnPropertyChanged2
	/// methods.
	/// </summary>
	/// <param name="args">Args.</param>
	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == ItemHeightProperty ||
			args.Property == ItemWidthProperty ||
			args.Property == ColumnSpanProperty ||
			args.Property == RowSpanProperty ||
			args.Property == OrientationProperty ||
			args.Property == HorizontalChildrenAlignmentProperty ||
			args.Property == VerticalChildrenAlignmentProperty ||
			args.Property == MaximumRowsOrColumnsProperty)
		{
			InvalidateMeasure();
		}
	}

	// ComputeBounds
	private void ComputeBounds(Size availableSize, out OrientedSize pItemSize, out OrientedSize pMaximumPanelSize, out bool pAreItemsOriented)
	{
		OrientedSize maximumSize = default;
		bool hasFixedWidth = false;
		bool hasFixedHeight = false;
		OrientedSize itemSize = default;

		UIElement? spFirstItem = null;
		Size firstItemDesiredSize = new(0.0, 0.0);
		Size firstItemMeasureSize = new(0.0, 0.0);
		int firstItemRowSpan = 1;
		int firstItemColumnSpan = 1;

		var orientation = Orientation;
		var itemWidth = ItemWidth;
		var itemHeight = ItemHeight;

		maximumSize.Orientation = orientation;
		maximumSize.Width = availableSize.Width;
		maximumSize.Height = availableSize.Height;

		hasFixedWidth = !DoubleUtil.IsNaN(itemWidth);
		hasFixedHeight = !DoubleUtil.IsNaN(itemHeight);
		itemSize.Orientation = orientation;
		itemSize.Width = hasFixedWidth ? itemWidth : availableSize.Width;
		itemSize.Height = hasFixedHeight ? itemHeight : availableSize.Height;
		m_itemSize = itemSize.AsUnorientedSize();
		var spChildren = Children;
		var itemCount = spChildren.Count;

		if (itemCount > 0)
		{
			// We always need to measure the first item.
			spFirstItem = spChildren[0];
			GetRowAndColumnSpan(spFirstItem, out firstItemRowSpan, out firstItemColumnSpan);
			// Calculate the size that first item will be measured
			firstItemMeasureSize.Width = hasFixedWidth ? (m_itemSize.Width * firstItemColumnSpan) : m_itemSize.Width;
			firstItemMeasureSize.Height = hasFixedHeight ? (m_itemSize.Height * firstItemRowSpan) : m_itemSize.Height;
			spFirstItem.Measure(firstItemMeasureSize);
			firstItemDesiredSize = spFirstItem.DesiredSize;
		}

		// If item sizes aren't specified, use the size of the first item
		if (!hasFixedWidth || !hasFixedHeight)
		{
			if (!hasFixedWidth)
			{
				itemSize.Width = firstItemDesiredSize.Width;
			}

			if (!hasFixedHeight)
			{
				itemSize.Height = firstItemDesiredSize.Height;
			}
		}
		pAreItemsOriented = orientation != Orientation.Vertical;
		pItemSize = itemSize;
		pMaximumPanelSize = maximumSize;
	}

	// MeasureOverride, this creates OccupancyMap and find the size of the Map
	// which is occupied
	protected override Size MeasureOverride(Size availableSize)
	{
		Orientation orientation = Orientation.Vertical;
		OrientedSize maximumSize = default;
		OrientedSize itemSize = default;
		OrientedSize totalSize = default;
		Size size = default;
		bool isHorizontalOrientation = false;
		OrientedSize startingSize;
		OrientedSize justificationSize;

		m_itemsPerLine = 0;
		m_pRowSpans = null;
		m_pColumnSpans = null;

		Size pReturnValue = default;

		ComputeBounds(availableSize, out itemSize, out maximumSize, out isHorizontalOrientation);
		m_itemSize = itemSize.AsUnorientedSize();
		var spChildren = Children;
		var itemCount = spChildren.Count;
		CalculateOccupancyMap(itemCount, spChildren, isHorizontalOrientation, ref itemSize, ref maximumSize, out m_itemsPerLine, out m_lineCount);
		m_itemSize = itemSize.AsUnorientedSize();

		// Measure the 2nd and later children (ComputeBounds measures the first one).
		for (int index = 1; index < itemCount; index++)
		{
			UIElement spItem = spChildren[index];
			size.Width = m_itemSize.Width * m_pColumnSpans![index];
			size.Height = m_itemSize.Height * m_pRowSpans![index];
			spItem.Measure(size);
		}

		// Compute and return the total amount of size used
		if (isHorizontalOrientation)
		{
			orientation = Orientation.Horizontal;
		}

		ComputeHorizontalAndVerticalAlignment(availableSize, out startingSize, out justificationSize);
		totalSize.Orientation = orientation;

		totalSize.Direct =
			itemSize.Direct * m_itemsPerLine +
			justificationSize.Direct * (m_itemsPerLine + 1) +
			startingSize.Direct * 2.0;

		totalSize.Indirect =
			itemSize.Indirect * m_lineCount +
			justificationSize.Indirect * (m_lineCount + 1) +
			startingSize.Indirect * 2.0;

		pReturnValue = totalSize.AsUnorientedSize();

		m_pRowSpans = null;
		m_pColumnSpans = null;

		return pReturnValue;
	}

	// This method creare Occupancy Map, layout children and find Occupied size
	private void CalculateOccupancyMap(
		int itemCount,
	 	UIElementCollection spChildren,
		bool isHorizontalOrientation,
		ref OrientedSize pItemSize,
		ref OrientedSize pMaximumSize,
		out int pItemsPerLine,
		out int pLineCount)
	{
		int index = 0;
		int maxRow = 0;
		int maxCol = 0;
		int rowTileCount = 0;
		int columnTileCount = 0;
		int rowSpan = 0;
		int columnSpan = 0;
		bool isPanelFull = false;
		UIElement? spItem = null;

		if (itemCount <= 0)
		{
			pLineCount = 0;
			pItemsPerLine = 0;
			return;
		}

		var maxItemsPerLine = MaximumRowsOrColumns;

		// Find out maxCol and maxRow
		m_pRowSpans = new int[itemCount];
		m_pColumnSpans = new int[itemCount];

		for (index = 0; index < itemCount; index++)
		{
			spItem = spChildren[index];

			GetRowAndColumnSpan(spItem, out rowSpan, out columnSpan);
			m_pRowSpans[index] = rowSpan;
			m_pColumnSpans[index] = columnSpan;

			//This may go out of int size, is total items are very large
			rowTileCount += (int)rowSpan;
			columnTileCount += (int)columnSpan;
		}

		// Determine the number of items that will fit per line
		pItemsPerLine = DoubleUtil.IsInfinity(pMaximumSize.Direct) ?
			(isHorizontalOrientation ? columnTileCount : rowTileCount) :
			(int)(pMaximumSize.Direct / pItemSize.Direct);
		pItemsPerLine = Math.Max(1, pItemsPerLine);
		if (maxItemsPerLine > 0)
		{
			pItemsPerLine = Math.Min(pItemsPerLine, (int)(maxItemsPerLine));
		}

		// if growing size is Infinity, we will use tileCount for maxCol
		// else we will use maxSize.Direct/itemSize.Direct
		maxCol = isHorizontalOrientation ? rowTileCount : columnTileCount;
		if (!DoubleUtil.IsInfinity(pMaximumSize.Indirect))
		{
			maxCol = Math.Min((int)(Math.Floor(pMaximumSize.Indirect / pItemSize.Indirect)), maxCol);
		}
		maxCol = Math.Max(1, maxCol);

		maxRow = pItemsPerLine;

		m_pMap = OccupancyMap.CreateOccupancyMap(maxRow, maxCol, isHorizontalOrientation, m_itemSize, itemCount);

		//Now fill the items in OccupancyMap
		for (int i = 0; i < itemCount; i++)
		{
			rowSpan = m_pRowSpans[i];
			columnSpan = m_pColumnSpans[i];
			m_pMap.FindAndFillNextAvailableRect(rowSpan, columnSpan, (int)ToAdjustedIndex(i), out isPanelFull);
			if (isPanelFull)
			{
				m_pMap.UpdateElementCount((int)i);
				break;
			}
		}

		// Find Used GridSize
		m_pMap.FindUsedGridSize(out pLineCount, out pItemsPerLine);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		Size returnValue = default;
		Size startingSize = new(0.0, 0.0);
		Size justificationSize = new(0.0, 0.0);
		OrientedSize orientedStartingSize;
		OrientedSize orientedJustificationSize;
		Size totalJustificationSize = new(0.0, 0.0);

		var orientation = Orientation;
		var spChildren = Children;
		var itemCount = spChildren.Count;

		returnValue = finalSize;
		// if itemCount > 0 then only we need to validate m_pMap
		if (itemCount > 0)
		{
			if (m_pMap is null)
			{
				throw new InvalidOperationException("Occupancy map cannot be null");
			}

			ComputeHorizontalAndVerticalAlignment(finalSize, out orientedStartingSize, out orientedJustificationSize);
			justificationSize = orientedJustificationSize.AsUnorientedSize();
			startingSize = orientedStartingSize.AsUnorientedSize();

			// gothrough each Children and Arrange Them
			for (int i = 0; i < itemCount; i++)
			{
				bool isHandled = false;
				UIElement? spItem = null;
				Rect elementRect = new(0, 0, 0, 0);
				spItem = spChildren[i];

				// If Map was full before laying down all items, then there is possibility that
				// perticular element is not in the map, in that case we won't arrange remaining items
				m_pMap.SetCurrentItem(i, out isHandled);
				if (!isHandled)
				{
					break;
				}

				elementRect = m_pMap.GetCurrentItemRect();

				// If we have MaximumRowsOrColumns=4, enough space for 8 rows, and user passes a 5x5 tile, we should still show 5x5 tile
				// The elementRect calculated by OccupancyMap will force clipping if total RowSpan > MaximumRowsOrColumns
				// Force Clipping only  occurs when the item starts from rowIndex = 0 or ColIndex=0 based on Orientation
				// The following code avoid the force clipping
				if (elementRect.Y == 0 || elementRect.X == 0)
				{
					GetRowAndColumnSpan(spItem, out var rowSpan, out var colSpan);

					// Check the possibility of force Clipping
					if (orientation == Orientation.Vertical
						&& elementRect.Y == 0
						&& rowSpan >= m_itemsPerLine)
					{
						double height = Math.Min(rowSpan * m_itemSize.Height, finalSize.Height);
						elementRect.Height = (float)(Math.Max(elementRect.Height, height));
					}
					else if (orientation == Orientation.Horizontal
						&& elementRect.X == 0
						&& colSpan >= m_itemsPerLine)
					{
						double width = Math.Min(colSpan * m_itemSize.Width, finalSize.Width);
						elementRect.Width = (float)(Math.Max(elementRect.Width, width));
					}
				}

				if (i % m_itemsPerLine == 0)
				{
					if (orientation == Orientation.Horizontal)
					{
						totalJustificationSize.Width = 0;
						totalJustificationSize.Height = justificationSize.Height * ((int)(i / m_itemsPerLine) + 1);
					}
					else
					{
						totalJustificationSize.Height = 0;
						totalJustificationSize.Width = justificationSize.Width * ((int)(i / m_itemsPerLine) + 1);
					}
				}

				if (orientation == Orientation.Horizontal)
				{
					totalJustificationSize.Width += justificationSize.Width;
				}
				else
				{
					totalJustificationSize.Height += justificationSize.Height;
				}

				elementRect.X += startingSize.Width + totalJustificationSize.Width;
				elementRect.Y += startingSize.Height + totalJustificationSize.Height;

				spItem.Arrange(elementRect);
			}
		}
		return returnValue;
	}

	/// <summary>
	/// For a given UIElement, this method returns ColumnSpan and RowSpan
	/// </summary>
	/// <param name="item">Item.</param>
	/// <param name="rowSpan">Row span.</param>
	/// <param name="columnSpan">Column span.</param>
	private void GetRowAndColumnSpan(UIElement item, out int rowSpan, out int columnSpan)
	{
		rowSpan = GetRowSpan(item);
		columnSpan = GetColumnSpan(item);

		if (columnSpan <= 0)
		{
			columnSpan = 1;
		}

		if (rowSpan <= 0)
		{
			rowSpan = 1;
		}
	}

	internal bool SupportsKeyNavigationAction(KeyNavigationAction action)
	{
		// Let the Selector handle Next/Previous.
		return
			(action != KeyNavigationAction.Next) &&
			(action != KeyNavigationAction.Previous);
	}

	// Calls the GetTargetIndexFromNavigationAction from OccupancyMap
	internal void GetTargetIndexFromNavigationAction(
		int sourceIndex,
		ElementType sourceType,
		KeyNavigationAction action,
		bool allowWrap,
		int itemIndexHintForHeaderNavigation,
		out int computedTargetIndex,
		out ElementType pComputedTargetElementType,
		out bool actionValidForSourceIndex)
	{
		//TODO:MZ: Should this be set?
		pComputedTargetElementType = default;

		int newElementIndex = 0;

		computedTargetIndex = 0;
		actionValidForSourceIndex = false;
		m_pMap.GetTargetIndexFromNavigationAction(sourceIndex, action, allowWrap, out newElementIndex, out actionValidForSourceIndex);
		if (actionValidForSourceIndex)
		{
			// since internally we store sourceIndex + 1, need to -1 to
			// return correct sourceIndex
			computedTargetIndex = (int)FromAdjustedIndex((int)newElementIndex);
		}
	}

	// Computes the bounds used to layout the items.
	private void ComputeHorizontalAndVerticalAlignment(
		// The available size we have to layout the child items
		Size availableSize,
	   // The calculated amount of space we'll reserve from the top/left corner for
	   // alignment (this is really being used more like an OrientedPoint, but
	   // there's no need to create a separate struct just for this)
	   out OrientedSize pStartingSize,
	   // The calculated amount of space we'll reserve between each item for
	   // alignment if we're justifying the items
	   out OrientedSize pJustificationSize)
	{
		OrientedSize maximumSize = default;
		double startingOffset = 0.0;
		double justificationOffset = 0.0;

		var orientation = Orientation;
		bool isHorizontal = Orientation == Orientation.Horizontal;

		// Orient the maximum size
		maximumSize.Orientation = orientation;
		maximumSize.Width = availableSize.Width;
		maximumSize.Height = availableSize.Height;

		pStartingSize = default;
		pJustificationSize = default;
		pStartingSize.Orientation = orientation;
		pJustificationSize.Orientation = orientation;
		var horizontalAlign = HorizontalChildrenAlignment;
		var verticalAlign = VerticalChildrenAlignment;

		// Determine how to adjust the items for horizontal content alignment
		WrapGrid.ComputeAlignmentOffsets(
			isHorizontal ? (int)verticalAlign : (int)horizontalAlign,
			maximumSize.Width,
			m_itemSize.Width * (isHorizontal ? m_itemsPerLine : m_lineCount),
			isHorizontal ? m_itemsPerLine : m_lineCount,
			out startingOffset,
			out justificationOffset);
		pStartingSize.Width = startingOffset;
		pJustificationSize.Width = justificationOffset;

		// Determine how to adjust the items for vertical content alignment
		WrapGrid.ComputeAlignmentOffsets(
			isHorizontal ? (int)horizontalAlign : (int)verticalAlign,
			maximumSize.Height,
			m_itemSize.Height * (isHorizontal ? m_lineCount : m_itemsPerLine),
			isHorizontal ? m_lineCount : m_itemsPerLine,
			out startingOffset,
			out justificationOffset);
		pStartingSize.Height = startingOffset;
		pJustificationSize.Height = justificationOffset;
	}

	// Logical Orientation override
	Orientation IOrientedPanel.LogicalOrientation => Orientation;

	// Physical Orientation override
	Orientation IOrientedPanel.PhysicalOrientation
	{
		get
		{
			Orientation orientation = Orientation;
			if (orientation == Orientation.Vertical)
			{
				return Orientation.Horizontal;
			}
			else
			{
				return Orientation.Vertical;
			}
		}
	}
}
