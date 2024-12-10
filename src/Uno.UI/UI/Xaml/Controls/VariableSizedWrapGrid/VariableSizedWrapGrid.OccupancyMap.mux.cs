#nullable enable

using System;
using Windows.Foundation;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//  Abstract:
//      Represents a OccupancyBlock and OccupancyMap
//      OccupancyMap: This class is used to arrange all items of variableSizeGrid
//      Since the size of OccupancyMap can be Infinite, OccupancyBlock is used to 
//      allocate block of Memory
//
//      OccupncyMap always arrange items vertically(row), whether user sets 
//      Orientation=Vertical or Horizontal. OccupancyMap's public APIs take care of
//      providing correct Orientation. Similarly OccupancyBlock has fixed rows while
//      it is gorwing on Column direction
//      Both classes are nested inside the VariableSizedWrapGrid class

namespace Windows.UI.Xaml.Controls;

partial class VariableSizedWrapGrid
{
	partial class OccupancyBlock
	{
		private OccupancyBlock()
		{
		}

		/// <summary>
		/// Static Method, Creates an instance of OccupancyBlock.
		/// </summary>
		/// <param name="maxRow">Max rows.</param>
		/// <param name="maxCol">Max columns.</param>
		/// <returns>Occupancy block.</returns>
		internal static OccupancyBlock CreateOccupancyBlock(int maxRow, int maxCol)
		{
			int columnSize = 0;
			OccupancyBlock? pOccupancyBlock = new();

			pOccupancyBlock.m_maxRow = maxRow;
			pOccupancyBlock.m_maxCol = maxCol;

			// Allocates memeory to block
			// Row is fixed size while column is either BlockSize or Column size
			// whicheve is minimum
			columnSize = maxCol > BLOCK_SIZE ? BLOCK_SIZE : maxCol;
			pOccupancyBlock.m_pOccupancyArray = new int[maxRow][];
			for (int i = 0; i < maxRow; i++)
			{
				pOccupancyBlock.m_pOccupancyArray[i] = new int[columnSize];
			}

			return pOccupancyBlock;

		}

		/// <summary>
		/// Saves elementIndex in 2 dimensional OccupancyArray.
		/// </summary>
		/// <param name="rowIndex"></param>
		/// <param name="colIndex"></param>
		/// <param name="elementIndex"></param>
		/// <exception cref="ArgumentOutOfRangeException">When column or row index is out of range.</exception>
		internal void SetItem(int rowIndex, int colIndex, int elementIndex)
		{
			if (colIndex >= m_maxCol)
			{
				throw new ArgumentOutOfRangeException(nameof(colIndex));
			}
			if (rowIndex >= m_maxRow)
			{
				throw new ArgumentOutOfRangeException(nameof(rowIndex));
			}

			var pBlock = GetBlock(colIndex);
			pBlock.m_pOccupancyArray![rowIndex][colIndex] = elementIndex;
		}

		/// <summary>
		/// Checks whether the given position is already occupied or not.
		/// </summary>
		/// <param name="rowIndex">Row index.</param>
		/// <param name="colIndex">Column index.</param>
		/// <returns>A value indicating whether the position is occupied.</returns>
		internal bool IsOccupied(int rowIndex, int colIndex)
		{
			int elementIndex = GetItem(rowIndex, colIndex);
			return elementIndex != 0;
		}

		internal int GetItem(int rowIndex, int colIndex)
		{
			if (colIndex >= m_maxCol)
			{
				throw new ArgumentOutOfRangeException(nameof(colIndex));
			}
			if (rowIndex >= m_maxRow)
			{
				throw new ArgumentOutOfRangeException(nameof(colIndex));
			}

			var pBlock = GetBlock(colIndex);
			return pBlock.m_pOccupancyArray![rowIndex][colIndex];
		}

		/// <summary>
		/// For a given ColIndex, this methods returns the Block which contains this colIndex.
		/// </summary>
		/// <param name="pColIndex">Column index.</param>
		/// <returns>Occupancy block.</returns>
		private OccupancyBlock GetBlock(int pColIndex)
		{
			int maxCol = m_maxCol;

			var pBlock = this;
			while (pColIndex >= BLOCK_SIZE)
			{
				pColIndex = pColIndex - BLOCK_SIZE;
				if (pBlock.m_pNextBlock is null)
				{
					maxCol = maxCol - BLOCK_SIZE;
					pBlock.m_pNextBlock = CreateOccupancyBlock(m_maxRow, maxCol);
				}
				pBlock = pBlock.m_pNextBlock;
			}

			return pBlock;
		}
	}

	partial class OccupancyMap
	{
		private OccupancyMap(OccupancyBlock occupancyBlock, MapLocation[] elementLocation)
		{
			m_pOccupancyBlock = occupancyBlock;
			m_pElementLocation = elementLocation;
		}

		/// <summary>
		/// Creates OccupancyMap.
		/// </summary>
		/// <param name="maxRow">Max rows.</param>
		/// <param name="maxCol">Max columns.</param>
		/// <param name="isHorizontalOrientation">Is horizontal?</param>
		/// <param name="itemSize">Item size.</param>
		/// <param name="itemCount">Item count.</param>
		/// <returns>Occupancy map.</returns>
		internal static OccupancyMap CreateOccupancyMap(
			int maxRow,
			int maxCol,
			bool isHorizontalOrientation,
			Size itemSize,
			int itemCount)
		{
			var pOccupancyMap = new OccupancyMap(OccupancyBlock.CreateOccupancyBlock(maxRow, maxCol), new MapLocation[itemCount]);
			pOccupancyMap.m_isHorizontalOrientation = isHorizontalOrientation;
			pOccupancyMap.m_maxCol = maxCol;
			pOccupancyMap.m_maxRow = maxRow;
			pOccupancyMap.m_itemSize = itemSize;
			pOccupancyMap.m_elementCount = itemCount;

			return pOccupancyMap;
		}

		/// <summary>
		/// For a given Row and Column Index, this returns the ElementIndex.
		/// </summary>
		/// <returns>Element index.</returns>
		private int GetCurrentElementIndex()
		{
			return m_pOccupancyBlock.GetItem(m_currentRowIndex, m_currentColumnIndex);
		}

		internal void UpdateElementCount(int elementCount)
		{
			m_elementCount = elementCount;
		}

		// This will set currentItem Indexes for given elementIndex
		// if not succeeded, this will set current Indexes to 0
		internal void SetCurrentItem(
			int elementIndex,
			out bool pIsHandled)
		{

			pIsHandled = false;
			m_currentColumnIndex = 0;
			m_currentRowIndex = 0;

			if (m_elementCount > elementIndex)
			{
				MapLocation point = m_pElementLocation[elementIndex];
				m_currentColumnIndex = point.ColIndex;
				m_currentRowIndex = point.RowIndex;
				pIsHandled = true;
			}
		}

		// This method find the CurrentItem rectangle
		// it uses currentItemIndexes and find the row and column span
		// and uses them to find ItemRectangle 
		internal Rect GetCurrentItemRect()
		{
			int colLeftIndex = m_currentColumnIndex;
			int colRightIndex = m_currentColumnIndex;
			int rowTopIndex = m_currentRowIndex;
			int rowBottomIndex = m_currentRowIndex;

			var pElementRect = new Rect();

			// Initialize pElementRect to 0
			pElementRect.X = 0;
			pElementRect.Y = 0;
			pElementRect.Width = 0;
			pElementRect.Height = 0;
			var value = GetCurrentElementIndex();

			// Find Column Left Index
			var outValue = value;
			while (value == outValue)
			{
				colLeftIndex -= 1;
				if (colLeftIndex < 0)
				{
					break;
				}
				outValue = m_pOccupancyBlock.GetItem(m_currentRowIndex, colLeftIndex);
			}
			colLeftIndex += 1;

			// Find Column Right Index
			outValue = value;
			while (value == outValue)
			{
				colRightIndex += 1;
				if (colRightIndex >= (int)(m_maxCol))
				{
					break;
				}

				outValue = m_pOccupancyBlock.GetItem(m_currentRowIndex, colRightIndex);
			}
			colRightIndex -= 1;

			// Find Row Top Index
			outValue = value;
			while (value == outValue)
			{
				rowTopIndex -= 1;
				if (rowTopIndex < 0)
				{
					break;
				}
				outValue = m_pOccupancyBlock.GetItem(rowTopIndex, m_currentColumnIndex);
			}
			rowTopIndex += 1;

			// Find Row Bottom Index
			outValue = value;
			while (value == outValue)
			{
				rowBottomIndex += 1;
				if (rowBottomIndex >= (int)m_maxRow)
				{
					break;
				}
				outValue = m_pOccupancyBlock.GetItem(rowBottomIndex, m_currentColumnIndex);
			}
			rowBottomIndex -= 1;

			// Calculate Row and Column Span
			var colSpan = colRightIndex - colLeftIndex + 1;
			var rowSpan = rowBottomIndex - rowTopIndex + 1;
			if (m_isHorizontalOrientation)
			{
				pElementRect.X = m_itemSize.Width * m_currentRowIndex;
				pElementRect.Y = m_itemSize.Height * m_currentColumnIndex;
				pElementRect.Width = m_itemSize.Width * rowSpan;
				pElementRect.Height = m_itemSize.Height * colSpan;
			}
			else
			{
				pElementRect.X = m_itemSize.Width * m_currentColumnIndex;
				pElementRect.Y = m_itemSize.Height * m_currentRowIndex;
				pElementRect.Width = m_itemSize.Width * colSpan;
				pElementRect.Height = m_itemSize.Height * rowSpan;
			}

			return pElementRect;
		}

		/// <summary>
		/// This method occupies the current Rectangle.
		/// RowSpan and ColumnSpan will always striped.
		/// </summary>
		/// <param name="rowSpan">Row span.</param>
		/// <param name="columnSpan">Column span.</param>
		/// <param name="elementIndex">Element index.</param>
		private void FillCurrentItemRect(int rowSpan, int columnSpan, int elementIndex)
		{
			MapLocation point = default;

			for (int column = 0; column < columnSpan; ++column)
			{
				for (int row = 0; row < rowSpan; ++row)
				{
					if (m_currentColumnIndex + column < m_maxCol &&
						m_currentRowIndex + row < m_maxRow)
					{
						m_pOccupancyBlock.SetItem(m_currentRowIndex + row, m_currentColumnIndex + column, elementIndex);
					}
				}
			}

			// Saving the RowIndex and ColIndex for given element
			point.RowIndex = m_currentRowIndex;
			point.ColIndex = m_currentColumnIndex;
			m_pElementLocation[FromAdjustedIndex((int)elementIndex)] = point;

			m_currentRowIndex += rowSpan;
			if (m_currentRowIndex >= m_maxRow)
			{
				m_currentRowIndex = 0;
				m_currentColumnIndex += 1;
			}
		}

		/// <summary>
		/// This calls FindNextAvailableRectIndex to find next available size and occupies it.
		/// </summary>
		/// <param name="rowSpan">Row span.</param>
		/// <param name="columnSpan">Column span.</param>
		/// <param name="value">Value.</param>
		/// <param name="pIsMapFull">Is map full?</param>
		internal void FindAndFillNextAvailableRect(
			int rowSpan,
			int columnSpan,
			int value,
			out bool pIsMapFull)
		{
			int rowSpanOriented = rowSpan;
			int columnSpanOriented = columnSpan;

			if (m_isHorizontalOrientation)
			{
				rowSpanOriented = columnSpan;
				columnSpanOriented = rowSpan;
			}

			FindNextAvailableRectInternal(rowSpanOriented, columnSpanOriented, out pIsMapFull);
			if (!pIsMapFull)
			{
				FillCurrentItemRect(rowSpanOriented, columnSpanOriented, value);
			}
		}

		// This method gothrough the grid from current Item Index and file the next availalbe position which can
		// fit the RowSpan and ColumnSpan
		private void FindNextAvailableRectInternal(
			int rowSpan,
			int columnSpan,
			out bool pIsMapFull)
		{
			pIsMapFull = false;

			if (rowSpan > m_maxRow)
			{
				rowSpan = (int)m_maxRow;
			}

			if (columnSpan > m_maxCol)
			{
				columnSpan = (int)m_maxCol;
			}

			// for each cell (row, col), this loop checks whether the cell is available or not,
			// if it is not available, this goes to next cell
			// if it is available, this loops through rowSpan and Column spans and verify whether
			// required cells for row and column span are available or now
			while (m_currentColumnIndex < m_maxCol)
			{
				bool isOccupied = true;

				if (m_currentRowIndex == m_maxRow)
				{
					m_currentRowIndex = 0;
					m_currentColumnIndex += 1;
				}

				for (int j = 0; j < columnSpan; ++j)
				{
					// If currentIndex + colSpan is > maxCol, we need to reduce the ColSpan
					if (j + m_currentColumnIndex >= m_maxCol)
					{
						columnSpan = (int)(m_maxCol - m_currentColumnIndex);
						break;
					}

					// Go through all rows and check whether the position is available to not
					// If not, this will get out from RowSpan loop
					for (int i = 0; i < rowSpan; ++i)
					{
						isOccupied = true;
						if (m_currentRowIndex + i < m_maxRow && m_currentColumnIndex + j < m_maxCol)
						{
							isOccupied = m_pOccupancyBlock.IsOccupied(m_currentRowIndex + i, m_currentColumnIndex + j);
						}

						if (isOccupied)
						{
							break;
						}
					}

					if (isOccupied)
					{
						break;
					}
				}

				// If the current position is occupied, or it is out of maxRow or maxCol range, 
				// go to the next Position (increase rowIndex)
				// if rowIndex is out of maxRow, goto next column, first position
				if (isOccupied)
				{
					m_currentRowIndex++;
					if (m_currentRowIndex == m_maxRow || m_currentRowIndex + rowSpan > m_maxRow)
					{
						m_currentRowIndex = 0;
						m_currentColumnIndex++;
					}
				}
				else
				{
					break;
				}
			}

			if (m_currentColumnIndex >= m_maxCol || m_currentRowIndex >= m_maxRow)
			{
				pIsMapFull = true;
				m_currentColumnIndex = 0;
				m_currentRowIndex = 0;
			}
		}

		/// <summary>
		/// This method returns the used/occupied gidsize from current Items arrangement.
		/// </summary>
		/// <param name="pMaxColIndex">Column index.</param>
		/// <param name="pMaxRowIndex">Max row index.</param>
		internal void FindUsedGridSize(out int pMaxColIndex, out int pMaxRowIndex)
		{
			int colindex = 0;
			int rowindex = 0;
			bool isOccupied;

			for (int j = 0; j < m_maxCol; ++j)
			{
				for (int i = 0; i < m_maxRow; ++i)
				{
					isOccupied = m_pOccupancyBlock.IsOccupied(i, j);
					if (isOccupied)
					{
						colindex = j;
						break;
					}
				}
			}

			for (int j = 0; j < m_maxRow; ++j)
			{
				for (int i = 0; i < m_maxCol; ++i)
				{
					isOccupied = m_pOccupancyBlock.IsOccupied(j, i);
					if (isOccupied)
					{
						rowindex = j;
						break;
					}
				}
			}

			if (m_isHorizontalOrientation)
			{
				pMaxColIndex = colindex + 1;
				pMaxRowIndex = rowindex + 1;
			}
			else
			{
				pMaxRowIndex = rowindex + 1;
				pMaxColIndex = colindex + 1;
			}

			// Saving the used Row and Col Index
			m_maxUsedRow = rowindex + 1;
			m_maxUsedCol = colindex + 1;
		}

		// Starting from sourceIndex, returns the index in the given direction.
		// OccupancyMap internally remembers the currentRow and ColumnIndex
		// if the sourceIndex at currentRow/Column Position is same as the sourceIndex (param),
		// it uses currentRow/ColumnIndex, 
		// If not, it uses row/columnIndex from sourceIndex (topLeft tile)
		// This method returns the pAdjacentElementIndex based on KeyNavigationAction
		//
		// pActionValidForSourceIndex can be false in following conditions:
		// If sourceIndex is 0 and action is Up or Left
		// If sourceIndex is maxRow -1, maxCol -1 and action is Right or Down
		// if action is not Up, Down, Right or Left
		internal void GetTargetIndexFromNavigationAction(
			int sourceIndex,
			KeyNavigationAction action,
			bool allowWrap,
			out int pComputedTargetIndex,
			out bool pActionValidForSourceIndex)
		{
			MapLocation point = default;

			pActionValidForSourceIndex = false;
			pComputedTargetIndex = 0;

			if (sourceIndex > m_elementCount)
			{
				return;
			}

			var currentElementIndex = m_pOccupancyBlock.GetItem(m_currentRowIndex, m_currentColumnIndex);

			// If sourceIndex at current Position is not same as required, need to update currentRow/ColumnIndex
			if (currentElementIndex != sourceIndex + 1)
			{
				point = m_pElementLocation[sourceIndex];
				m_currentRowIndex = point.RowIndex;
				m_currentColumnIndex = point.ColIndex;
			}

			if (action == KeyNavigationAction.First ||
				action == KeyNavigationAction.Last)
			{
				if (action == KeyNavigationAction.First)
				{
					// if there are items top Left position will always filled up
					m_currentRowIndex = 0;
					m_currentColumnIndex = 0;
					pActionValidForSourceIndex = true;
				}
				else
				{
					// get the last element and returns it's locations
					point = m_pElementLocation[m_elementCount - 1];
					m_currentRowIndex = point.RowIndex;
					m_currentColumnIndex = point.ColIndex;
					pActionValidForSourceIndex = true;
				}
			}
			else
			{
				// If horizontal Orientation, swap the Keys
				if (m_isHorizontalOrientation)
				{
					switch (action)
					{
						case KeyNavigationAction.Left:
							{
								MoveUpOrLeft(ref m_currentRowIndex, out pActionValidForSourceIndex);
								break;
							}
						case KeyNavigationAction.Right:
							{
								MoveDownOrRight(ref m_currentRowIndex, m_maxUsedRow, out pActionValidForSourceIndex);
								break;
							}
						case KeyNavigationAction.Up:
							{
								MoveUpOrLeft(ref m_currentColumnIndex, out pActionValidForSourceIndex);
								break;
							}
						case KeyNavigationAction.Down:
							{
								MoveDownOrRight(ref m_currentColumnIndex, m_maxUsedCol, out pActionValidForSourceIndex);
								break;
							}
					}
				}
				else
				{
					switch (action)
					{
						case KeyNavigationAction.Left:
							{
								MoveUpOrLeft(ref m_currentColumnIndex, out pActionValidForSourceIndex);
								break;
							}
						case KeyNavigationAction.Right:
							{
								MoveDownOrRight(ref m_currentColumnIndex, m_maxUsedCol, out pActionValidForSourceIndex);
								break;
							}
						case KeyNavigationAction.Up:
							{
								MoveUpOrLeft(ref m_currentRowIndex, out pActionValidForSourceIndex);
								break;
							}
						case KeyNavigationAction.Down:
							{
								MoveDownOrRight(ref m_currentRowIndex, m_maxUsedRow, out pActionValidForSourceIndex);
								break;
							}
					}
				}
			}

			if (pActionValidForSourceIndex)
			{
				pComputedTargetIndex = m_pOccupancyBlock.GetItem(m_currentRowIndex, m_currentColumnIndex);
			}
		}

		/// <summary>
		/// This is common Method used by MoveUp and MoveLeft.
		/// </summary>
		/// <param name="moveDirectionIndex">Move direction index.</param>
		/// <param name="pIsHandled">Is handled?</param>
		private void MoveUpOrLeft(ref int moveDirectionIndex, out bool pIsHandled)
		{
			int elementIndex = 0;

			pIsHandled = false;
			var currentElementIndex = m_pOccupancyBlock.GetItem(m_currentRowIndex, m_currentColumnIndex);
			elementIndex = currentElementIndex;

			// if the currentElement is same as elementIndex or there is no element on current position
			while (elementIndex == currentElementIndex || currentElementIndex == 0)
			{
				if (moveDirectionIndex == 0)
				{
					// reached the first element in the line
					return;
				}
				else
				{
					--moveDirectionIndex;
				}

				currentElementIndex = m_pOccupancyBlock.GetItem(m_currentRowIndex, m_currentColumnIndex);
			}

			pIsHandled = true;
		}

		/// <summary>
		/// Common method for MoveDown and Right.
		/// </summary>
		/// <param name="moveDirectionIndex">Move direction index.</param>
		/// <param name="maxCountInMoveDirection">Max number of moves.</param>
		/// <param name="pIsHandled">Is handled?</param>
		private void MoveDownOrRight(ref int moveDirectionIndex, int maxCountInMoveDirection, out bool pIsHandled)
		{

			int elementIndex = 0;

			MUX_ASSERT(moveDirectionIndex < maxCountInMoveDirection);
			pIsHandled = false;
			var currentElementIndex = m_pOccupancyBlock.GetItem(m_currentRowIndex, m_currentColumnIndex);
			elementIndex = currentElementIndex;

			// if the currentElement is same as elementIndex or there is no element on current position
			while (elementIndex == currentElementIndex || currentElementIndex == 0)
			{
				if (moveDirectionIndex >= maxCountInMoveDirection - 1)
				{
					// reached the last element in the line
					return;
				}
				else
				{
					++moveDirectionIndex;
				}

				currentElementIndex = m_pOccupancyBlock.GetItem(m_currentRowIndex, m_currentColumnIndex);
			}

			pIsHandled = true;
		}
	}
}
