#nullable enable

using Windows.Foundation;

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//  Abstract:
//      Represents a VariableSizeWrapGrid
//      VariableSizeWrapGrid is used to create variable size tiles inside the grid.
//
//      It has OccupancyBlock and OccupancyMap nested classes
//      OccupancyMap: This class is used to arrange all items of variableSizeGrid
//      Since the size of OccupancyMap can be Infinite, OccupancyBlock is used to
//      allocate block of Memory
//
//      OccupancyMap always arrange items vertically(row), whether user sets
//      Orientation=Vertical or Horizontal. OccupancyMap's public APIs take care of
//      providing correct Orientation. Similarly OccupancyBlock has fixed rows while
//      it is growing on Column direction
//
//      Both classes are nested inside the VariableSizedWrapGrid class

namespace Windows.UI.Xaml.Controls;

// Represents a VariableSizeWrapGrid.
partial class VariableSizedWrapGrid
{
	// OccupancyBlock class
	// This class contains the block of 2 dimensional Array which will be used to map used/empty
	// tiles in VariableSizedWrapGrid Panel
	// BLOCK_SIZE is the size of the block it allocates.
	// This has link to another block of Data, so if total size of grid is larger then current Block,
	// it uses next Block
	private sealed partial class OccupancyBlock
	{
		// Maximum number of Rows in Variable Size Grid
		private int m_maxRow;
		// Maximum number of Columns in Variable Size Grid
		// If isHorizontalOrientation is false, this will be Grid Width
		// OR if width is infinite, this will be maximum tiles needs all items layout Horizontally
		// If isHorizontalOrientation is true, this will be Grid Height
		// OR if height is infinite, this will be maximum tiles needs all items layout vertically
		private int m_maxCol;

		// 2 dimensional Array which will be used to map used/empty tiles in VariableSizedWrapGrid Panel
		// it stores UIElementIndex + 1 to specify which element has occupied particular area
		// value 0 is reserved for default, which means it is empty.
		private int[][]? m_pOccupancyArray;

		// Pointer to next OccupancyBlock
		private OccupancyBlock? m_pNextBlock;

		// static BLOCK_SIZE, default is 50 for now
		private const int BLOCK_SIZE = 50;
	};

	// This class contains the occupancyMap for VariableSizeWrapGrid Panel
	// OccupancyMap always arrange items vertically(row), whether user sets
	// Orientation=Vertical or Horizontal. OccupancyMap's public APIs take care of
	// providing correct Orientation.
	private sealed partial class OccupancyMap
	{
		// Internal struct used to keep track of row and colIndex
		// This will be used to track the location of each element in OccupancyMap
		private struct MapLocation
		{
			internal int ColIndex;
			internal int RowIndex;
		};

		// Maximum number of Rows in Variable Size Grid
		private int m_maxRow;
		// Maximum number of Columns in Variable Size Grid
		// If isHorizontalOrientation is false, this will be Grid Width
		// OR if width is infinite, this will be maximum tiles needs all items layout Horizontally
		// If isHorizontalOrientation is true, this will be Grid Height
		// OR if height is infinite, this will be maximum tiles needs all items layout vertically
		private int m_maxCol;
		// Max number of rows used by arranged items
		private int m_maxUsedRow;
		// Max number of Columns used by arranged items
		private int m_maxUsedCol;

		private bool m_isHorizontalOrientation;
		private int m_currentRowIndex;
		private int m_currentColumnIndex;
		private Size m_itemSize;
		private OccupancyBlock m_pOccupancyBlock;

		// contains the list of locations (rowIndex,colIndex) for each Element
		// This way it is easy to find out the row/colIndex for given Element
		private MapLocation[] m_pElementLocation;

		// number of elements in grid
		private int m_elementCount;
	}

	private Size m_itemSize;

	// Occupancy Map arrange all items in 2 dimensional map
	// it is used to find the ElementRect for particular item
	private OccupancyMap m_pMap;

	// Saves the RowSpan and ColumnSpans attached properties value for each item
	// this is used in OccupancyMap
	private int[]? m_pRowSpans;
	private int[]? m_pColumnSpans;
	private int m_itemsPerLine;
	private int m_lineCount;

	// Starting from elementIndex, returns the index in the given direction.
	// OccupancyMap internally remembers the currentRow and ColumnIndex
	// if the elementIndex at currentRow/Column Position is same as the elementIndex (param),
	// it uses currentRow/ColumnIndex,
	// If not, it uses row/columnIndex from elementIndex (topLeft tile)
	// This method returns the pAdjacentElementIndex based on FocusNavigationDirection
	//
	// pActionValidForSourceIndex can be false in following conditions:
	// If elementIndex is 0 and Key is Up or Left
	// If elementIndex is maxRow -1, maxCol -1 and Key is Right or Down
	// if Key is not Up, Down, Right or Left	
}
