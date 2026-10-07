// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Common\GridCoordinateHelper.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

internal partial struct GridCoordinateHelper
{
	public GridCoordinateHelper(int rowCount, int columnCount)
	{
		m_rowCount = rowCount < 0 ? 0 : rowCount;
		m_columnCount = columnCount < 0 ? 0 : columnCount;
	}

	public partial void Resize(int rowCount, int columnCount)
	{
		m_rowCount = rowCount < 0 ? 0 : rowCount;
		m_columnCount = columnCount < 0 ? 0 : columnCount;
	}

	public partial bool IsValidCellAddress(int row, int column) =>
		row >= 0 && row < m_rowCount && column >= 0 && column < m_columnCount;

	public partial int GetFlatIndex(int row, int column)
	{
		if (!IsValidCellAddress(row, column))
		{
			return -1;
		}
		// Use int64_t to avoid overflow on very large grids.
		long flat = (long)row * (long)m_columnCount
			+ (long)column;
		return flat > int.MaxValue ? -1 : (int)flat;
	}

	public partial bool TryGetCellAddress(int flatIndex, out int row, out int column)
	{
		// Use int64_t so large grids cannot wrap the bounds check.
		long total = (long)m_rowCount * (long)m_columnCount;
		if (m_columnCount <= 0 || m_rowCount <= 0 ||
			flatIndex < 0 || (long)flatIndex >= total)
		{
			row = -1;
			column = -1;
			return false;
		}

		row = flatIndex / m_columnCount;
		column = flatIndex % m_columnCount;
		return true;
	}

	public partial bool TryGetNextFocusableCell(
		int currentRow,
		int currentColumn,
		FocusNavigationDirection direction,
		bool wrap,
		out int nextRow,
		out int nextColumn)
	{
		nextRow = -1;
		nextColumn = -1;

		if (m_rowCount <= 0 || m_columnCount <= 0)
		{
			return false;
		}

		// Clamp the starting cell into the grid so callers can pass any "current focus" hint.
		int startRow = Math.Clamp(currentRow, 0, m_rowCount - 1);
		int startColumn = Math.Clamp(currentColumn, 0, m_columnCount - 1);

		int row = startRow;
		int column = startColumn;

		switch (direction)
		{
			case FocusNavigationDirection.Left:
				{
					if (column > 0)
					{
						column -= 1;
					}
					else if (wrap)
					{
						// Wrap to the previous row, or the last cell from row 0.
						if (row > 0)
						{
							row -= 1;
						}
						else
						{
							row = m_rowCount - 1;
						}
						column = m_columnCount - 1;
					}
					else
					{
						return false;
					}
					break;
				}
			case FocusNavigationDirection.Right:
				{
					if (column < m_columnCount - 1)
					{
						column += 1;
					}
					else if (wrap)
					{
						// Wrap to the next row, or (0,0) from the last row.
						if (row < m_rowCount - 1)
						{
							row += 1;
						}
						else
						{
							row = 0;
						}
						column = 0;
					}
					else
					{
						return false;
					}
					break;
				}
			case FocusNavigationDirection.Up:
				{
					if (row > 0)
					{
						row -= 1;
					}
					else if (wrap)
					{
						row = m_rowCount - 1;
					}
					else
					{
						return false;
					}
					break;
				}
			case FocusNavigationDirection.Down:
				{
					if (row < m_rowCount - 1)
					{
						row += 1;
					}
					else if (wrap)
					{
						row = 0;
					}
					else
					{
						return false;
					}
					break;
				}
			case FocusNavigationDirection.Next:
				{
					// Reading-order tab with int64_t math to avoid large-grid overflow.
					long flat = (long)row * m_columnCount + column + 1;
					long total = (long)m_rowCount * m_columnCount;
					if (flat >= total)
					{
						if (!wrap)
						{
							return false;
						}
						row = 0;
						column = 0;
					}
					else
					{
						row = (int)(flat / m_columnCount);
						column = (int)(flat % m_columnCount);
					}
					break;
				}
			case FocusNavigationDirection.Previous:
				{
					// Reverse reading-order tab. int64 intermediate as above.
					long flat = (long)row * m_columnCount + column - 1;
					if (flat < 0)
					{
						if (!wrap)
						{
							return false;
						}
						row = m_rowCount - 1;
						column = m_columnCount - 1;
					}
					else
					{
						row = (int)(flat / m_columnCount);
						column = (int)(flat % m_columnCount);
					}
					break;
				}
			default:
				// None / other directions are not defined for grid navigation.
				return false;
		}

		nextRow = row;
		nextColumn = column;
		return true;
	}
}
