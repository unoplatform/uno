// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Windows.UI.Xaml.Controls
{

	partial class Grid
	{
		[Flags]
		enum CellUnitTypes : byte
		{
			None = 0x00,
			Auto = 0x01,
			Star = 0x02,
			Pixel = 0x04,
		};

		//DEFINE_ENUM_FLAG_OPERATORS(CellUnitTypes);

		struct CellCache
		{
			internal UIElement m_child;

			// Index of the next cell in the group.
			internal int m_next;

			// Union of the different height unit types across the row
			// definitions within the row span of this cell.
			internal CellUnitTypes m_rowHeightTypes;

			// Union of the different width unit types across the column
			// definitions within the column span of this cell.
			internal CellUnitTypes m_columnWidthTypes;

			internal static bool IsStar(CellUnitTypes unitTypes)
			{
				return (unitTypes & CellUnitTypes.Star) == CellUnitTypes.Star;
			}

			internal static bool IsAuto(CellUnitTypes unitTypes)
			{
				return (unitTypes & CellUnitTypes.Auto) == CellUnitTypes.Auto;
			}
		};

		// Grid classifies cells into four groups based on their column/row
		// type. The following diagram depicts all the possible combinations
		// and their corresponding cell group:
		// 
		//                  Px      Auto     Star
		//              +--------+--------+--------+
		//              |        |        |        |
		//           Px |    1   |    1   |    3   |
		//              |        |        |        |
		//              +--------+--------+--------+
		//              |        |        |        |
		//         Auto |    1   |    1   |    3   |
		//              |        |        |        |
		//              +--------+--------+--------+
		//              |        |        |        |
		//         Star |    4   |    2   |    4   |
		//              |        |        |        |
		//              +--------+--------+--------+
		//
		struct CellGroups
		{
			internal int group1;
			internal int group2;
			internal int group3;
			internal int group4;
		}
	}
}
