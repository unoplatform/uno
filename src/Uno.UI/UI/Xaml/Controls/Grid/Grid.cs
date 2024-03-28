// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Uno.Extensions;
using XSIZEF = Windows.Foundation.Size;
using Xuint = System.Int32;
using XFLOAT = System.Double;
using XRECTF = Windows.Foundation.Rect;
using PropertyChangedParams = Windows.UI.Xaml.DependencyPropertyChangedEventArgs;
using CDOCollection = Windows.UI.Xaml.Controls.DefinitionCollectionBase;

namespace Windows.UI.Xaml.Controls
{
	partial class Grid
	{
		private const XFLOAT REAL_EPSILON = 1.192092896e-07F /* FLT_EPSILON */;

		//CGrid.~CGrid()
		//{
		//	ReleaseInterface(m_pRows);
		//	ReleaseInterface(m_pColumns);
		//	ReleaseInterface(m_pColumnDefinitions);
		//	ReleaseInterface(m_pRowDefinitions);
		//	delete[] m_ppTempDefinitions;
		//}

		// Get the row index of a child.
		int GetRowIndex(
			UIElement child)
		{
			return Math.Min(
				//(FrameworkElement)(child).m_pLayoutProperties.m_nGridRow,
				Grid.GetRow(child),
				m_pRows.Count - 1);
		}

		int GetRowSpanAdjusted(UIElement child)
		{
			return Math.Min(GetRowSpan(child), m_pRows.Count - GetRowIndex(child));
		}

		// Get the column index of a child.
		int GetColumnIndex(
			UIElement child)
		{
			return Math.Min(
				//(FrameworkElement)(child).m_pLayoutProperties.m_nGridColumn,
				Grid.GetColumn(child),
				m_pColumns.Count - 1);
		}

		int GetColumnSpanAdjusted(UIElement child)
		{
			return Math.Min(GetColumnSpan(child), m_pColumns.Count - GetColumnIndex(child));
		}

		//// Get the row span value of a child.
		//uint GetRowSpan(
		//	UIElement child)
		//{
		//	return Math.Min(
		//		(FrameworkElement)(child).m_pLayoutProperties.m_nGridRowSpan,
		//		m_pRows.Count - GetRowIndex(child));
		//}

		//// Get the column span value of a child.
		//uint GetColumnSpan(
		//	UIElement child)
		//{
		//	return Math.Min(
		//		(FrameworkElement)(child).m_pLayoutProperties.m_nGridColumnSpan,
		//		m_pColumns.Count - GetColumnIndex(child));
		//}

		//------------------------------------------------------------------------
		//
		//  Method: GetRow
		//
		//  Synopsis: Get the row definition for a Grid child
		//
		//------------------------------------------------------------------------
		DefinitionBase GetRowNoRef(UIElement pChild)
		{
			//return (DefinitionBase)(m_pRows.GetItem(GetRowIndex(pChild)));
			m_pRows.TryGetElementAt(GetRowIndex(pChild), out RowDefinition rd);
			return rd;
		}


		//------------------------------------------------------------------------
		//
		//  Method: GetColumn
		//
		//  Synopsis: Get the column definition for a Grid child
		//
		//------------------------------------------------------------------------
		DefinitionBase GetColumnNoRef(UIElement pChild)
		{
			//return (DefinitionBase)(m_pColumns.GetItem(GetColumnIndex(pChild)));
			m_pColumns.TryGetElementAt(GetColumnIndex(pChild), out ColumnDefinition cd);
			return cd;
		}

		// Locks user-defined RowDefinitions and ColumnDefinitions so that user code
		// cannot change them while we are working with them.
		void LockDefinitions()
		{
			if (m_pRowDefinitions is DefinitionCollectionBase rowDefs)
			{
				rowDefs.Lock();
			}

			if (m_pColumnDefinitions is DefinitionCollectionBase colDefs)
			{
				colDefs.Lock();
			}
		}

		// Unlocks user-defined RowDefinitions and ColumnDefinitions.
		void UnlockDefinitions()
		{
			if (m_pRowDefinitions is DefinitionCollectionBase rowDefs)
			{
				rowDefs.Unlock();
			}

			if (m_pColumnDefinitions is DefinitionCollectionBase colDefs)
			{
				colDefs.Unlock();
			}
		}

		//------------------------------------------------------------------------
		//
		//  Method:   ValidateDefinitionStructure
		//
		//  Synopsis: Initializes m_pRows and  m_pColumns either to user supplied ColumnDefinitions collection
		//                 or to a default single element collection. This is the only method where user supplied
		//                 row or column definitions is directly used. All other must use m_pRows/m_pColumns
		//------------------------------------------------------------------------
		void InitializeDefinitionStructure()
		{
			RowDefinition emptyRow = null;
			ColumnDefinition emptyColumn = null;

			ASSERT(!IsWithoutRowAndColumnDefinitions());
			//ReleaseInterface(m_pRows);
			//ReleaseInterface(m_pColumns);

			if (m_pRowDefinitions == null || m_pRowDefinitions.Count == 0)
			{
				//empty collection defaults to single row
				//CValue value;
				//value.WrapObjectNoRef(null);
				//CREATEPARAMETERS param(this.
				//	GetContext(), value);
				//RowDefinitionCollection.Create((CDependencyObject)(m_pRows), &param);
				//RowDefinition.Create((CDependencyObject)(emptyRow), &param);
				//m_pRows.Append(emptyRow);
				m_pRows = new RowDefinitionCollection();
				emptyRow = new RowDefinition();
				m_pRows.Add(emptyRow);
			}
			else
			{
				m_pRows = m_pRowDefinitions;
				//m_pRowDefinitions.AddRef();
			}

			if (m_pColumnDefinitions == null || m_pColumnDefinitions.Count == 0)
			{
				//empty collection defaults to single row
				m_pColumns = new ColumnDefinitionCollection();
				emptyColumn = new ColumnDefinition();
				m_pColumns.Add(emptyColumn);
			}
			else
			{
				m_pColumns = m_pColumnDefinitions;
			}

		}

		// Sets the initial, effective values of a CDefinitionCollectionBase.
		void ValidateDefinitions(
			DefinitionCollectionBase definitions,
			bool treatStarAsAuto)
		{
			//for (auto & cdo : definitions)
			var itemsEnumerator = definitions.GetItems().GetEnumerator();

			while (itemsEnumerator.MoveNext())
			{
				var def = itemsEnumerator.Current;

				bool useLayoutRounding = GetUseLayoutRounding();
				var userSize = double.PositiveInfinity;
				var userMinSize = useLayoutRounding
					? LayoutRound(def.GetUserMinSize())
					: def.GetUserMinSize();
				var userMaxSize = useLayoutRounding
					? LayoutRound(def.GetUserMaxSize())
					: def.GetUserMaxSize();

				switch (def.GetUserSizeType())
				{
					case GridUnitType.Pixel:
						userSize = useLayoutRounding
							? LayoutRound(def.GetUserSizeValue())
							: def.GetUserSizeValue();
						userMinSize = Math.Max(userMinSize, Math.Min(userSize, userMaxSize));
						def.SetEffectiveUnitType(GridUnitType.Pixel);
						break;
					case GridUnitType.Auto:
						def.SetEffectiveUnitType(GridUnitType.Auto);
						break;
					case GridUnitType.Star:
						if (treatStarAsAuto)
						{
							def.SetEffectiveUnitType(GridUnitType.Auto);
						}
						else
						{
							def.SetEffectiveUnitType(GridUnitType.Star);
						}

						break;
					default:
						ASSERT(false);
						break;
				}

				def.SetEffectiveMinSize((XFLOAT)userMinSize);
				def.SetMeasureArrangeSize((XFLOAT)Math.Max(userMinSize, Math.Min(userSize, userMaxSize)));
			}
		}

		// Organizes the grid cells into four groups, defining the order in which
		// these should be measured.
		CellGroups ValidateCells(
			UIElementCollection children,
			ref CellCacheStackVector cellCacheVector)
		{
			m_gridFlags = GridFlags.None;

			CellGroups cellGroups;
			cellGroups.group1 = int.MaxValue;
			cellGroups.group2 = int.MaxValue;
			cellGroups.group3 = int.MaxValue;
			cellGroups.group4 = int.MaxValue;

			var childrenCount = children.Count;

			// Initialize the cells in the cell cache.
			//cellCacheVector.m_vector.clear();
			//cellCacheVector.m_vector.resize(childrenCount);
			cellCacheVector.Resize(childrenCount);

			var childIndex = childrenCount;
			while (childIndex-- > 0)
			{
				UIElement currentChild = children[childIndex];
				ref CellCache cell = ref cellCacheVector[childIndex];

				cell.m_child = currentChild;
				cell.m_rowHeightTypes = GetLengthTypeForRange(
					m_pRows,
					GetRowIndex(currentChild),
					GetRowSpanAdjusted(currentChild));
				cell.m_columnWidthTypes = GetLengthTypeForRange(
					m_pColumns,
					GetColumnIndex(currentChild),
					GetColumnSpanAdjusted(currentChild));

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

				if (!CellCache.IsStar(cell.m_rowHeightTypes))
				{
					if (!CellCache.IsStar(cell.m_columnWidthTypes))
					{
						cell.m_next = cellGroups.group1;
						cellGroups.group1 = childIndex;
					}
					else
					{
						cell.m_next = cellGroups.group3;
						cellGroups.group3 = childIndex;

						if (CellCache.IsAuto(cell.m_rowHeightTypes))
						{
							// Remember that this Grid has at least one Auto row;
							// useful for detecting cyclic dependency while measuring.
							SetGridFlags(GridFlags.HasAutoRowsAndStarColumn);
						}
					}
				}
				else
				{
					SetGridFlags(GridFlags.HasStarRows);

					if (CellCache.IsAuto(cell.m_columnWidthTypes) && !CellCache.IsStar(cell.m_columnWidthTypes))
					{
						cell.m_next = cellGroups.group2;
						cellGroups.group2 = childIndex;
					}
					else
					{
						cell.m_next = cellGroups.group4;
						cellGroups.group4 = childIndex;
					}
				}

				if (CellCache.IsStar(cell.m_columnWidthTypes))
				{
					SetGridFlags(GridFlags.HasStarColumns);
				}
			}

			return cellGroups;
		}

		//------------------------------------------------------------------------
		//
		//  Method:   MeasureCellsGroup
		//
		//  Synopsis: Measure one group of cells
		//
		//------------------------------------------------------------------------
		void MeasureCellsGroup(
			int cellsHead, //cell group number
			int cellCount, //elements in the cell
			XFLOAT rowSpacing,
			XFLOAT columnSpacing,
			bool ignoreColumnDesiredSize,
			bool forceRowToInfinity,
			ref CellCacheStackVector cellCacheVector)
		{

			SpanStoreStackVector spanStore = new SpanStoreStackVector();

			if (cellsHead >= cellCount)
			{
				return;
			}

			do
			{
				CellCache cell = cellCacheVector[cellsHead];
				UIElement pChild = cell.m_child;

				MeasureCell(pChild, cell.m_rowHeightTypes, cell.m_columnWidthTypes, forceRowToInfinity, rowSpacing,
					columnSpacing);
				//If a span exists, add to span store for delayed processing. processing is done when
				//all the desired sizes for a given definition index and span value are known.

				if (!ignoreColumnDesiredSize)
				{
					Xuint columnSpan = GetColumnSpanAdjusted(pChild);
					//pChild.EnsureLayoutStorage();
					if (columnSpan == 1)
					{
						DefinitionBase pChildColumn = GetColumnNoRef(pChild);
						pChildColumn.UpdateEffectiveMinSize((XFLOAT)pChild.DesiredSize.Width);
					}
					else
					{
						RegisterSpan(
							spanStore,
							GetColumnIndex(pChild),
							columnSpan,
							pChild.DesiredSize.Width,
							true /* isColumnDefinition */);
					}
				}

				if (!forceRowToInfinity)
				{
					Xuint rowSpan = GetRowSpanAdjusted(pChild);
					//pChild.EnsureLayoutStorage();
					if (rowSpan == 1)
					{
						DefinitionBase pChildRow = GetRowNoRef(pChild);
						pChildRow.UpdateEffectiveMinSize((XFLOAT)pChild.DesiredSize.Height);
					}
					else
					{
						RegisterSpan(
							spanStore,
							GetRowIndex(pChild),
							rowSpan,
							pChild.DesiredSize.Height,
							false /* isColumnDefinition */);
					}
				}

				cellsHead = cellCacheVector[cellsHead].m_next;

			} while (cellsHead < cellCount);

			//Go through the spanned rows/columns allocating sizes.
			foreach (ref var entry in spanStore.Memory.Span)
			{
				if (entry.m_isColumnDefinition)
				{
					EnsureMinSizeInDefinitionRange(
						m_pColumns,
						entry.m_spanStart,
						entry.m_spanCount,
						columnSpacing,
						entry.m_desiredSize);
				}
				else
				{
					EnsureMinSizeInDefinitionRange(
						m_pRows,
						entry.m_spanStart,
						entry.m_spanCount,
						rowSpacing,
						entry.m_desiredSize);
				}
			}

			// Cleanup
			// return hr;

			// Return allocated memory to the pool
			spanStore.Dispose();
		}

		// Measure a child of the Grid by taking in consideration the properties of
		// the cell it belongs to.
		void MeasureCell(
			UIElement child,
			CellUnitTypes rowHeightTypes,
			CellUnitTypes columnWidthTypes,
			bool forceRowToInfinity,
			XFLOAT rowSpacing,
			XFLOAT columnSpacing)
		{
			XSIZEF availableSize = default;

			if (CellCache.IsAuto(columnWidthTypes) && !CellCache.IsStar(columnWidthTypes))
			{
				// If this cell belongs to at least one Auto column and not a single
				// Star column, then it should be measured freely to fit its content.
				// In other words, we must give it an infinite available width.
				availableSize.Width = XFLOAT.PositiveInfinity;
			}
			else
			{
				availableSize.Width = GetAvailableSizeForRange(
					m_pColumns,
					GetColumnIndex(child),
					GetColumnSpanAdjusted(child),
					columnSpacing);
			}

			if (forceRowToInfinity
				|| (CellCache.IsAuto(rowHeightTypes) && !CellCache.IsStar(rowHeightTypes)))
			{
				// If this cell belongs to at least one Auto row and not a single Star
				// row, then it should be measured freely to git its content. In other
				// words, we must give it an infinite available height.
				availableSize.Height = XFLOAT.PositiveInfinity;
			}
			else
			{
				availableSize.Height = GetAvailableSizeForRange(
					m_pRows,
					GetRowIndex(child),
					GetRowSpanAdjusted(child),
					rowSpacing);
			}

			//child.Measure(availableSize);
			this.MeasureElement(child, availableSize);

			return;
		}

		// Adds a span entry to the list.
		void RegisterSpan(
			SpanStoreStackVector spanStore,
			int spanStart,
			int spanCount,
			XFLOAT desiredSize,
			bool isColumnDefinition)
		{
			var spanStoreVector = spanStore;
			// If an entry already exists with the same row/column index and span,
			// then update the desired size stored in the entry.
			//var it = std.find_if(
			//		spanStoreVector.begin(),
			//		spanStoreVector.end(),
			//	[isColumnDefinition, spanStart, spanCount](SpanStoreEntry & entry)
			//{
			//	return entry.m_isColumnDefinition == isColumnDefinition && entry.m_spanStart == spanStart && entry.m_spanCount == spanCount;
			//});

			SpanStoreEntry it = default;
			bool IsEntry(ref SpanStoreEntry entry)
			{
				return entry.m_isColumnDefinition == isColumnDefinition && entry.m_spanStart == spanStart && entry.m_spanCount == spanCount;
			}
			spanStore.FirstOrDefault(IsEntry, ref it);


			unsafe
			{
				SpanStoreEntry last = default;
				if (spanStoreVector.LastOrDefault(ref last) && Unsafe.AsPointer(ref last) == Unsafe.AsPointer(ref it))
				//if (it != spanStoreVector.LastOrDefault())
				{
					if ((it).m_desiredSize < desiredSize)
					{
						(it).m_desiredSize = desiredSize;
					}
				}
				else
				{
					//spanStoreVector.emplace_back(spanStart, spanCount, desiredSize, isColumnDefinition);
					ref var newEntry = ref spanStoreVector.PushBack();
					newEntry = new SpanStoreEntry(spanStart, spanCount, desiredSize, isColumnDefinition);
				}
			}
		}


		//------------------------------------------------------------------------
		//
		//  Method:   EnsureMinSizeInDefinitionRange
		//
		//  Synopsis:  Distributes min size back to definition array's range.
		//
		//------------------------------------------------------------------------
		void EnsureMinSizeInDefinitionRange(
			CDOCollection definitions,
			int spanStart,
			int spanCount,
			XFLOAT spacing,
			XFLOAT childDesiredSize)
		{
			ASSERT((spanCount > 1) && (spanStart + spanCount) <= definitions.Count);

			// The spacing between definitions that this element spans through must not
			// be distributed.
			XFLOAT requestedSize = Math.Max((childDesiredSize - spacing * (spanCount - 1)), 0.0f);

			//  No need to process if asked to distribute "zero".
			if (requestedSize <= REAL_EPSILON)
			{
				return;
			}

			int spanEnd = spanStart + spanCount;
			int autoDefinitionsCount = 0;
			XFLOAT rangeMinSize = 0.0f;
			XFLOAT rangeMaxSize = 0.0f;
			XFLOAT rangePreferredSize = 0.0f;
			XFLOAT maxMaxSize = 0.0f;

			EnsureTempDefinitionsStorage(spanCount);

			// First, we need to obtain the necessary information:
			// a) Sum up the sizes in the range.
			// b) Cache the maximum size into SizeCache.
			// c) Obtain max of MaxSizes.
			// d) Count the number of var definitions in the range.
			// e) Prepare indices.
			for (int i = spanStart; i < spanEnd; i++)
			{
				var def = definitions.GetItem(i);
				XFLOAT effectiveMinSize = def.GetEffectiveMinSize();
				XFLOAT preferredSize = def.GetPreferredSize();
				XFLOAT maxSize = Math.Max(def.GetUserMaxSize(), effectiveMinSize);
				rangeMinSize += effectiveMinSize;
				rangePreferredSize += preferredSize;
				rangeMaxSize += maxSize;

				// Sanity check: effectiveMinSize must always be the smallest value, maxSize
				// must be the largest one, and the preferredSize should fall in between.
				ASSERT(effectiveMinSize <= preferredSize
					   && preferredSize <= maxSize
					   && rangeMinSize <= rangePreferredSize
					   && rangePreferredSize <= rangeMaxSize);

				def.SetSizeCache(maxSize);
				maxMaxSize = Math.Max(maxMaxSize, maxSize);

				if (def.GetUserSizeType() == GridUnitType.Auto)
				{
					autoDefinitionsCount++;
				}

				m_ppTempDefinitions[i - spanStart] = def;
			}

			if (requestedSize <= rangeMinSize)
			{
				// No need to process if the range is already big enough.
				return;
			}
			else if (requestedSize <= rangePreferredSize)
			{
				// If the requested size fits within the preferred size of the range,
				// we distribute the space following this logic:
				// - Do not distribute into Auto definitions; they should continue to
				//   stay "tight".
				// - For all non-Auto definitions, distribute to equi-size min sizes
				//   without exceeding the preferred size of the definition.
				//
				// In order to achieve this, the definitions are sorted in a way so
				// that all Auto definitions go first, then the other definitions
				// follow in ascending order of PreferredSize.
				XFLOAT sizeToDistribute = requestedSize;
				SortDefinitionsForSpanPreferredDistribution(m_ppTempDefinitions, spanCount);

				// Process Auto definitions.
				for (int i = 0; i < autoDefinitionsCount; i++)
				{
					var def = m_ppTempDefinitions[i];
					ASSERT(def.GetUserSizeType() == GridUnitType.Auto);

					sizeToDistribute -= def.GetEffectiveMinSize();
				}

				// Process the remaining, non-Auto definitions, distributing
				// the requested size among them.
				for (int i = autoDefinitionsCount; i < spanCount; i++)
				{
					var def = m_ppTempDefinitions[i];
					ASSERT(def.GetUserSizeType() != GridUnitType.Auto);

					XFLOAT newMinSize = Math.Min((sizeToDistribute / (spanCount - i)), def.GetPreferredSize());
					def.UpdateEffectiveMinSize(newMinSize);
					sizeToDistribute -= newMinSize;

					// Stop if there's no more space to distribute.
					if (sizeToDistribute < REAL_EPSILON)
					{
						break;
					}
				}
			}
			else if (requestedSize <= rangeMaxSize)
			{
				// If the requested size is larger than the preferred size of the range
				// but still fits within the max size of the range, we distribute the
				// space following this logic:
				// - Do not distribute into Auto definitions if possible; they should
				//   continue to stay "tight".
				// - For all non-Auto definitions, distribute to equi-size min sizes
				//   without exceeding the max size.
				//
				// In order to achieve this, the definitions are sorted in a way so
				// that all non-Auto definitions go first, followed by the Auto
				// definitions, and all of them in ascending order of MaxSize, which
				// is currently stored in the size cache of each definition.
				XFLOAT sizeToDistribute = requestedSize - rangePreferredSize;
				SortDefinitionsForSpanMaxSizeDistribution(m_ppTempDefinitions, spanCount);

				int nonAutoDefinitionsCount = spanCount - autoDefinitionsCount;
				for (int i = 0; i < spanCount; i++)
				{
					var def = m_ppTempDefinitions[i];
					XFLOAT newMinSize = def.GetPreferredSize();

					if (i < nonAutoDefinitionsCount)
					{
						// Processing non-Auto definitions.
						ASSERT(def.GetUserSizeType() != GridUnitType.Auto);
						newMinSize += sizeToDistribute / (nonAutoDefinitionsCount - i);
					}
					else
					{
						// Processing the remaining, Auto definitions.
						ASSERT(def.GetUserSizeType() == GridUnitType.Auto);
						newMinSize += sizeToDistribute / (spanCount - i);
					}

					// Cache PreferredSize and update MinSize.
					XFLOAT preferredSize = def.GetPreferredSize();
					newMinSize = Math.Min(newMinSize, def.GetSizeCache());
					def.UpdateEffectiveMinSize(newMinSize);

					sizeToDistribute -= def.GetEffectiveMinSize() - preferredSize;

					// Stop if there's no more space to distribute.
					if (sizeToDistribute < REAL_EPSILON)
					{
						break;
					}
				}
			}
			else
			{
				// If the requested size is larger than the max size of the range, we
				// distribute the space following this logic:
				// - For all definitions, distribute to equi-size min sizes.
				XFLOAT equallyDistributedSize = requestedSize / spanCount;

				if ((equallyDistributedSize < maxMaxSize) && ((maxMaxSize - equallyDistributedSize) > REAL_EPSILON))
				{
					// If equi-size is less than the maximum of max sizes, then
					// we distribute space so that smaller definitions grow
					// faster than larger ones.
					XFLOAT totalRemainingSize = maxMaxSize * spanCount - rangeMaxSize;
					XFLOAT sizeToDistribute = requestedSize - rangeMaxSize;

					ASSERT(totalRemainingSize.IsFinite()
						   && totalRemainingSize > 0
						   && sizeToDistribute.IsFinite()
						   && sizeToDistribute > 0);

					for (int i = 0; i < spanCount; i++)
					{
						var def = m_ppTempDefinitions[i];
						XFLOAT deltaSize = (maxMaxSize - def.GetSizeCache()) * sizeToDistribute / totalRemainingSize;
						def.UpdateEffectiveMinSize(def.GetSizeCache() + deltaSize);
					}
				}
				else
				{
					// If equi-size is greater or equal to the maximum of max sizes,
					// then all definitions receive equi-size as their min sizes.
					for (int i = 0; i < spanCount; i++)
					{
						m_ppTempDefinitions[i].UpdateEffectiveMinSize(equallyDistributedSize);
					}
				}
			}
		}

		// Gets the union of the length types for a given range of definitions.
		CellUnitTypes GetLengthTypeForRange(
			CDOCollection definitions,
			int start,
			int count)
		{
			ASSERT((count > 0) && ((start + count) <= definitions.Count));

			CellUnitTypes unitTypes = CellUnitTypes.None;
			int index = start + count - 1;

			do
			{
				var def = (DefinitionBase)(definitions.GetItem(index));
				switch (def.GetEffectiveUnitType())
				{
					case GridUnitType.Auto:
						unitTypes |= CellUnitTypes.Auto;
						break;
					case GridUnitType.Pixel:
						unitTypes |= CellUnitTypes.Pixel;
						break;
					case GridUnitType.Star:
						unitTypes |= CellUnitTypes.Star;
						break;
				}
			} while (index > 0 && --index >= start);

			return unitTypes;
		}

		// Accumulates available size information for a given range of definitions.
		XFLOAT GetAvailableSizeForRange(
			CDOCollection definitions,
			int start,
			int count,
			XFLOAT spacing)
		{
			ASSERT((count > 0) && ((start + count) <= definitions.Count));

			XFLOAT availableSize = 0.0f;
			int index = start + count - 1;

			do
			{
				var def = (DefinitionBase)(definitions.GetItem(index));
				availableSize += (def.GetEffectiveUnitType() == GridUnitType.Auto)
					? def.GetEffectiveMinSize()
					: def.GetMeasureArrangeSize();
			} while (index > 0 && --index >= start);

			availableSize += spacing * (count - 1);

			return availableSize;
		}

		// Accumulates final size information for a given range of definitions.
		XFLOAT GetFinalSizeForRange(
			CDOCollection definitions,
			int start,
			int count,
			XFLOAT spacing)
		{
			ASSERT((count > 0) && ((start + count) <= definitions.Count));

			XFLOAT finalSize = 0.0f;
			int index = start + count - 1;

			do
			{
				var def = (DefinitionBase)(definitions.GetItem(index));
				finalSize += def.GetMeasureArrangeSize();
			} while (index > 0 && --index >= start);

			finalSize += spacing * (count - 1);

			return finalSize;
		}

		// Calculates the desired size of the Grid minus its BorderThickness and
		// Padding assuming all the cells have already been measured.
		XFLOAT GetDesiredInnerSize(CDOCollection definitions)
		{
			XFLOAT desiredSize = 0.0f;

			for (Xuint i = 0; i < definitions.Count; ++i)
			{
				var def = (DefinitionBase)(definitions.GetItem(i));
				desiredSize += def.GetEffectiveMinSize();
			}

			return desiredSize;
		}

		//------------------------------------------------------------------------
		//
		//  Method:   ResolveStar
		//
		//  Synopsis:  Resolves Star's for given array of definitions during measure pass
		//
		//------------------------------------------------------------------------
		void ResolveStar(
			DefinitionCollectionBase definitions, //the definitions collection
			XFLOAT availableSize //the total available size across this dimension
		)
		{
			Xuint cStarDefinitions = 0;
			XFLOAT takenSize = 0.0f;
			XFLOAT effectiveAvailableSize = availableSize;

			EnsureTempDefinitionsStorage(definitions.Count);

			for (Xuint i = 0; i < definitions.Count; i++)
			{
				//if star definition, setup values for distribution calculation

				DefinitionBase pDef = (DefinitionBase)(definitions.GetItem(i));

				if (pDef.GetEffectiveUnitType() == GridUnitType.Star)
				{
					m_ppTempDefinitions[cStarDefinitions++] = pDef;

					// Note that this user value is in star units and not pixel units,
					// and thus, there is no need to layout-round.
					XFLOAT starValue = pDef.GetUserSizeValue();

					if (starValue < REAL_EPSILON)
					{
						pDef.SetMeasureArrangeSize(0.0f);
						pDef.SetSizeCache(0.0f);
					}
					else
					{
						//clipping by a max to avoid overflow when all the star values are added up.
						starValue = Math.Min(starValue, GRID_STARVALUE_MAX);

						pDef.SetMeasureArrangeSize(starValue);

						// Note that this user value is used for a computation that is cached
						// and then used in the call to CGrid.DistributeStarSpace below for
						// further calculations where the final result is layout-rounded as
						// appropriate. In other words, it doesn't seem like we need to apply
						// layout-rounding just yet.
						XFLOAT maxSize = Math.Min(GRID_STARVALUE_MAX,
							Math.Max(pDef.GetEffectiveMinSize(), pDef.GetUserMaxSize()));
						pDef.SetSizeCache(maxSize / starValue);
					}
				}
				else
				{
					//if not star definition, reduce the size available to star definitions
					if (pDef.GetEffectiveUnitType() == GridUnitType.Pixel)
					{
						takenSize += pDef.GetMeasureArrangeSize();
					}
					else if (pDef.GetEffectiveUnitType() == GridUnitType.Auto)
					{
						takenSize += pDef.GetEffectiveMinSize();
					}
				}
			}

			if (GetUseLayoutRounding())
			{
				takenSize = LayoutRound(takenSize);

				// It is important to apply layout rounding to the available size too,
				// since we need to make sure that we are working with one or the other:
				// rounded values or non-rounded values only. Otherwise, the distribution
				// of star space will compute slightly different results for the star
				// definitions in the case were takenSize == 0 vs. takenSize != 0.
				effectiveAvailableSize = LayoutRound((double)effectiveAvailableSize);
			}

			DistributeStarSpace(m_ppTempDefinitions, cStarDefinitions, effectiveAvailableSize - takenSize, ref takenSize);
		}

		//------------------------------------------------------------------------
		//
		//  Method:   DistributeStarSpace
		//
		//  Synopsis:  Distributes available space between star definitions.
		//
		//------------------------------------------------------------------------
		void DistributeStarSpace(
			DefinitionBase[] ppStarDefinitions,
			Xuint cStarDefinitions,
			XFLOAT availableSize,
			ref XFLOAT pTotalResolvedSize)
		{
			XFLOAT resolvedSize;
			XFLOAT starValue;
			XFLOAT totalStarResolvedSize = 0.0f;

			if (cStarDefinitions < 0)
			{
				return;
			}

			//sorting definitions for order of space allocation. definition with the lowest
			//maxSize to starValue ratio gets the size first.
			SortDefinitionsForStarSizeDistribution(ppStarDefinitions, cStarDefinitions);

			XFLOAT allStarWeights = 0.0f;
			Xuint i = cStarDefinitions;

			while (i > 0)
			{
				i--;
				allStarWeights += ppStarDefinitions[i].GetMeasureArrangeSize();
				//store partial sum of weights
				ppStarDefinitions[i].SetSizeCache(allStarWeights);
			}

			i = 0;
			while (i < cStarDefinitions)
			{
				resolvedSize = 0.0f;
				starValue = ppStarDefinitions[i].GetMeasureArrangeSize();

				if (starValue == 0.0f)
				{
					resolvedSize = ppStarDefinitions[i].GetEffectiveMinSize();
				}
				else
				{
					resolvedSize = Math.Max(availableSize - totalStarResolvedSize, 0.0f) *
								   (starValue / ppStarDefinitions[i].GetSizeCache());
					resolvedSize = Math.Max(ppStarDefinitions[i].GetEffectiveMinSize(),
						Math.Min(resolvedSize, ppStarDefinitions[i].GetUserMaxSize()));
				}

				if (GetUseLayoutRounding())
				{
					resolvedSize = LayoutRound(resolvedSize);
				}

				ppStarDefinitions[i].SetMeasureArrangeSize(resolvedSize);
				totalStarResolvedSize += resolvedSize;

				i++;
			}

			pTotalResolvedSize += totalStarResolvedSize;
		}


		//------------------------------------------------------------------------
		//
		//  Method:   EnsureTempDefinitionsStorage
		//
		//  Synopsis:  allocates memory for temporary definitions storage.
		//
		//------------------------------------------------------------------------
		void EnsureTempDefinitionsStorage(int minCount)
		{
			if (m_ppTempDefinitions == null || m_cTempDefinitions < minCount)
			{
				//delete[] m_ppTempDefinitions;
				m_ppTempDefinitions = new DefinitionBase[minCount];
				m_cTempDefinitions = minCount;
			}

			//memset(m_ppTempDefinitions, 0, minCount * sizeof(DefinitionBase));
		}


		//------------------------------------------------------------------------
		//
		//  Method:   CGrid.MeasureOverride
		//
		//  Synopsis:
		//      Overriding CFrameworkElement virtual to add grid sepecific logic to measure pass
		//
		//------------------------------------------------------------------------
		protected override XSIZEF MeasureOverride(XSIZEF availableSize)
		{
			// Locking the row and columns definitions to prevent changes by user code
			// during the measure pass.
			LockDefinitions();
			//var scopeGuard = wil.scope_exit([&]
			//{
			//	UnlockDefinitions();
			//});

			try
			{
				return InnerMeasureOverride(availableSize);
			}
			finally
			{
				UnlockDefinitions();
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private XSIZEF InnerMeasureOverride(XSIZEF availableSize)
		{
			XSIZEF desiredSize;

			XSIZEF combinedThickness = Border.HelperGetCombinedThickness(this);
			XSIZEF innerAvailableSize =
				new XSIZEF(
					availableSize.Width - combinedThickness.Width,
					availableSize.Height - combinedThickness.Height);

			desiredSize = default;

			if (IsWithoutRowAndColumnDefinitions())
			{
				// If this Grid has no user-defined rows or columns, it is possible
				// to shortcut this MeasureOverride.
				var children = (GetChildren());
				if (children is { })
				{
					// This block is a manual enumeration to avoid the foreach pattern
					// See https://github.com/dotnet/runtime/issues/56309 for details
					var childrenEnumerator = children.GetEnumerator();
					while (childrenEnumerator.MoveNext())
					{
						var currentChild = childrenEnumerator.Current;
						ASSERT(currentChild is { });

						//currentChild.Measure(innerAvailableSize);
						this.MeasureElement(currentChild, innerAvailableSize);
						//currentChild.EnsureLayoutStorage();

						//XSIZEF childDesiredSize = currentChild.GetLayoutStorage().m_desiredSize;
						XSIZEF childDesiredSize = currentChild.DesiredSize;
						desiredSize.Width = Math.Max(desiredSize.Width, childDesiredSize.Width);
						desiredSize.Height = Math.Max(desiredSize.Height, childDesiredSize.Height);
					}
				}
			}
			else
			{
				if (HasGridFlags(GridFlags.DefinitionsChanged))
				{
					ClearGridFlags(GridFlags.DefinitionsChanged);
					InitializeDefinitionStructure();
				}

				ValidateDefinitions(m_pRows, innerAvailableSize.Height == XFLOAT.PositiveInfinity /* treatStarAsAuto */);
				ValidateDefinitions(m_pColumns, innerAvailableSize.Width == XFLOAT.PositiveInfinity /* treatStarAsAuto */);

				XFLOAT rowSpacing = RowSpacing;
				XFLOAT columnSpacing = ColumnSpacing;
				XFLOAT combinedRowSpacing = rowSpacing * (m_pRows.Count - 1);
				XFLOAT combinedColumnSpacing = columnSpacing * (m_pColumns.Count - 1);
				innerAvailableSize.Width -= combinedColumnSpacing;
				innerAvailableSize.Height -= combinedRowSpacing;

				var children = GetUnsortedChildren();
				int childrenCount = children.Count;

				CellCacheStackVector cellCacheVector = new CellCacheStackVector(childrenCount);
				CellGroups cellGroups = ValidateCells(children, ref cellCacheVector);

				// The group number of a cell indicates the order in which it will be
				// measured; a certain order is necessary to dynamically resolve star
				// definitions since there are cases when the topology of a Grid causes
				// cyclical dependencies. For example:
				//
				//
				//                         column width="Auto"      column width="*"
				//                      +----------------------+----------------------+
				//                      |                      |                      |
				//                      |                      |                      |
				//                      |                      |                      |
				//                      |                      |                      |
				//  row height="Auto"   |                      |      cell 1 2        |
				//                      |                      |                      |
				//                      |                      |                      |
				//                      |                      |                      |
				//                      |                      |                      |
				//                      +----------------------+----------------------+
				//                      |                      |                      |
				//                      |                      |                      |
				//                      |                      |                      |
				//                      |                      |                      |
				//  row height="*"      |       cell 2 1       |                      |
				//                      |                      |                      |
				//                      |                      |                      |
				//                      |                      |                      |
				//                      |                      |                      |
				//                      +----------------------+----------------------+
				//
				// In order to accurately calculate the raining width for "cell 1 2"
				// (which corresponds to the remaining space of the grid's available width
				// and calculated value of the var column), "cell 2 1" needs to be calculated
				// first, as it contributes to the calculated value of the var column.
				// At the same time in order to accurately calculate the raining
				// height for "cell 2 1", "cell 1 2" needs to be calcualted first,
				// as it contributes to the calculated height of the var row, which is used
				// in the computation of the resolved height of the star row.
				//
				// To break this cyclical dependency we are making the (arbitrary)
				// decision to treat cells like "cell 2 1" as if they were in auto
				// rows. We will recalculate them later once the heights of star rows
				// are resolved. In other words, the code below implements the
				// following logic:
				//
				//                       +---------+
				//                       |  enter  |
				//                       +---------+
				//                            |
				//                            V
				//                    +----------------+
				//                    | Measure Group1 |
				//                    +----------------+
				//                            |
				//                            V
				//                          / - \
				//                        /       \
				//                  Y   /    Can    \    N
				//            +--------|   Resolve   |-----------+
				//            |         \  StarsV?  /            |
				//            |           \       /              |
				//            |             \ - /                |
				//            V                                  V
				//    +----------------+                       / - \
				//    | Resolve StarsV |                     /       \
				//    +----------------+               Y   /    Can    \    N
				//            |                      +----|   Resolve   |------+
				//            V                      |     \  StarsU?  /       |
				//    +----------------+             |       \       /         |
				//    | Measure Group2 |             |         \ - /           |
				//    +----------------+             |                         V
				//            |                      |                 +-----------------+
				//            V                      |                 | Measure Group2' |
				//    +----------------+             |                 +-----------------+
				//    | Resolve StarsU |             |                         |
				//    +----------------+             V                         V
				//            |              +----------------+        +----------------+
				//            V              | Resolve StarsU |        | Resolve StarsU |
				//    +----------------+     +----------------+        +----------------+
				//    | Measure Group3 |             |                         |
				//    +----------------+             V                         V
				//            |              +----------------+        +----------------+
				//            |              | Measure Group3 |        | Measure Group3 |
				//            |              +----------------+        +----------------+
				//            |                      |                         |
				//            |                      V                         V
				//            |              +----------------+        +----------------+
				//            |              | Resolve StarsV |        | Resolve StarsV |
				//            |              +----------------+        +----------------+
				//            |                      |                         |
				//            |                      |                         V
				//            |                      |                +------------------+
				//            |                      |                | Measure Group2'' |
				//            |                      |                +------------------+
				//            |                      |                         |
				//            +----------------------+-------------------------+
				//                                   |
				//                                   V
				//                           +----------------+
				//                           | Measure Group4 |
				//                           +----------------+
				//                                   |
				//                                   V
				//                               +--------+
				//                               |  exit  |
				//                               +--------+
				//
				// Where:
				// *   StarsV = Stars in Rows
				// *   StarsU = Stars in Columns.
				// *   All [Measure GroupN] - regular children measure process -
				//     each cell is measured given contraint size as an input
				//     and each cell's desired size is accumulated on the
				//     corresponding column / row.
				// *   [Measure Group2'] - is when each cell is measured with
				//     infinit height as a raint and a cell's desired
				//     height is ignored.
				// *   [Measure Groups''] - is when each cell is measured (second
				//     time during single Grid.MeasureOverride) regularly but its
				//     returned width is ignored.
				//
				// This algorithm is believed to be as close to ideal as possible.
				// It has the following drawbacks:
				// *   Cells belonging to Group2 could be measured twice.
				// *   Iff during the second measure, a cell belonging to Group2
				//     returns a desired width greater than desired width returned
				//     the first time, the cell is going to be clipped, even though
				//     it appears in an var column.

				// Measure Group1. After Group1 is measured, only Group3 can have
				// cells belonging to var rows.
				MeasureCellsGroup((int)cellGroups.group1, childrenCount, rowSpacing, columnSpacing, false, false, ref cellCacheVector);

				// After Group1 is measured, only Group3 may have cells belonging to
				// Auto rows.
				if (!HasGridFlags(GridFlags.HasAutoRowsAndStarColumn))
				{
					// We have no cyclic dependency; resolve star row/var column first.
					if (HasGridFlags(GridFlags.HasStarRows))
					{
						ResolveStar(m_pRows, innerAvailableSize.Height);
					}

					// Measure Group2.
					MeasureCellsGroup((int)cellGroups.group2, childrenCount, rowSpacing, columnSpacing, false, false, ref cellCacheVector);

					if (HasGridFlags(GridFlags.HasStarColumns))
					{
						ResolveStar(m_pColumns, innerAvailableSize.Width);
					}

					// Measure Group3.
					MeasureCellsGroup((int)cellGroups.group3, childrenCount, rowSpacing, columnSpacing, false, false, ref cellCacheVector);
				}
				else
				{
					// If at least one cell exists in Group2, it must be measured
					// before star columns can be resolved.
					if (cellGroups.group2 > childrenCount)
					{
						if (HasGridFlags(GridFlags.HasStarColumns))
						{
							ResolveStar(m_pColumns, innerAvailableSize.Width);
						}

						// Measure Group3.
						MeasureCellsGroup((int)cellGroups.group3, childrenCount, rowSpacing, columnSpacing, false, false, ref cellCacheVector);

						if (HasGridFlags(GridFlags.HasStarRows))
						{
							ResolveStar(m_pRows, innerAvailableSize.Height);
						}
					}
					else
					{
						// We have a cyclic dependency; measure Group2 for their
						// widths, while setting the row heights to infinity.
						MeasureCellsGroup((int)cellGroups.group2, childrenCount, rowSpacing, columnSpacing, false, true, ref cellCacheVector);

						if (HasGridFlags(GridFlags.HasStarColumns))
						{
							ResolveStar(m_pColumns, innerAvailableSize.Width);
						}

						// Measure Group3.
						MeasureCellsGroup(cellGroups.group3, childrenCount, rowSpacing, columnSpacing, false, false, ref cellCacheVector);

						if (HasGridFlags(GridFlags.HasStarRows))
						{
							ResolveStar(m_pRows, innerAvailableSize.Height);
						}

						// Now, Measure Group2 again for their heights and ignore their widths.
						MeasureCellsGroup((int)cellGroups.group2, childrenCount, rowSpacing, columnSpacing, true, false, ref cellCacheVector);
					}
				}

				// Finally, measure Group4.
				MeasureCellsGroup((int)cellGroups.group4, childrenCount, rowSpacing, columnSpacing, false, false, ref cellCacheVector);

				desiredSize.Width = GetDesiredInnerSize(m_pColumns) + combinedColumnSpacing;
				desiredSize.Height = GetDesiredInnerSize(m_pRows) + combinedRowSpacing;

				// Return memory to the array pool
				cellCacheVector.Dispose();
			}

			desiredSize.Width += combinedThickness.Width;
			desiredSize.Height += combinedThickness.Height;

			return desiredSize;
		}

		//------------------------------------------------------------------------
		//
		//  Method:   CGrid.ArrangeOverride
		//
		//  Synopsis:
		//      Overriding CFrameworkElement virtual to add grid specific logic to arrange pass
		//
		//------------------------------------------------------------------------
		protected override XSIZEF ArrangeOverride(XSIZEF finalSize)
		{
			// Locking the row and columns definitions to prevent changes by user code
			// during the arrange pass.
			LockDefinitions();
			try
			{
				return InnerArrangeOverride(finalSize);
			}
			finally
			{
				m_ppTempDefinitions = null;
				m_cTempDefinitions = 0;
				UnlockDefinitions();
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private XSIZEF InnerArrangeOverride(XSIZEF finalSize)
		{
			XRECTF innerRect = Border.HelperGetInnerRect(this, finalSize);

			if (IsWithoutRowAndColumnDefinitions())
			{
				// If this Grid has no user-defined rows or columns, it is possible
				// to shortcut this ArrangeOverride.
				var children = GetChildren();
				if (children is { })
				{
					// This block is a manual enumeration to avoid the foreach pattern
					// See https://github.com/dotnet/runtime/issues/56309 for details
					var childrenEnumerator = children.GetEnumerator();
					while (childrenEnumerator.MoveNext())
					{
						var currentChild = childrenEnumerator.Current;
						ASSERT(currentChild is { });

						//currentChild.EnsureLayoutStorage();
						//XSIZEF childDesiredSize = currentChild.GetLayoutStorage().m_desiredSize;
						XSIZEF childDesiredSize = currentChild.DesiredSize;
						innerRect.Width = Math.Max(innerRect.Width, childDesiredSize.Width);
						innerRect.Height = Math.Max(innerRect.Height, childDesiredSize.Height);
						//currentChild.Arrange(innerRect);
						this.ArrangeElement(currentChild, innerRect);
					}
				}
			}
			else
			{
				// UNO NRE FIX
				if (HasGridFlags(GridFlags.DefinitionsChanged))
				{
					// A call to .Measure() is required before arranging children
					// When the DefinitionsChanged is set, the measure is already invalidated
					// This can arise in certain niche layout scenarios, like adding a child during MeasureOverride() set to Collapsed, then setting it to Visible during ArrangeOverride (true story)
					return default;  // Returning (0, 0)
				}
				//IFCEXPECT_RETURN(m_pRows && m_pColumns);

				XFLOAT rowSpacing = (XFLOAT)RowSpacing;
				XFLOAT columnSpacing = (XFLOAT)ColumnSpacing;
				XFLOAT combinedRowSpacing = rowSpacing * (m_pRows.Count - 1);
				XFLOAT combinedColumnSpacing = columnSpacing * (m_pColumns.Count - 1);

				// Given an effective final size, compute the offsets and sizes of each
				// row and column, including the resdistribution of Star sizes based on
				// the new width and height.
				SetFinalSize(m_pRows, (XFLOAT)innerRect.Height - combinedRowSpacing);
				SetFinalSize(m_pColumns, (XFLOAT)innerRect.Width - combinedColumnSpacing);

				var children = GetUnsortedChildren();
				int count = children.Count;

				for (Xuint childIndex = 0; childIndex < count; childIndex++)
				{
					UIElement currentChild = children[childIndex];
					ASSERT(currentChild is { });

					DefinitionBase row = GetRowNoRef(currentChild);
					DefinitionBase column = GetColumnNoRef(currentChild);
					Xuint columnIndex = GetColumnIndex(currentChild);
					Xuint rowIndex = GetRowIndex(currentChild);

					XRECTF arrangeRect = new XRECTF();
					arrangeRect.X = column.GetFinalOffset() + innerRect.X + (columnSpacing * columnIndex);
					arrangeRect.Y = row.GetFinalOffset() + innerRect.Y + (rowSpacing * rowIndex);
					arrangeRect.Width = GetFinalSizeForRange(m_pColumns, columnIndex, GetColumnSpanAdjusted(currentChild), columnSpacing);
					arrangeRect.Height = GetFinalSizeForRange(m_pRows, rowIndex, GetRowSpanAdjusted(currentChild), rowSpacing);

					//currentChild.Arrange(arrangeRect);
					this.ArrangeElement(currentChild, arrangeRect);
				}
			}

			XSIZEF newFinalSize = finalSize;

			return newFinalSize;
		}


		//------------------------------------------------------------------------
		//
		//  Method:   CGrid.SetFinalSize
		//
		//  Synopsis:
		//      Computes the offsets and sizes for each row and column
		//
		//------------------------------------------------------------------------
		void SetFinalSize(
			DefinitionCollectionBase definitions,
			XFLOAT finalSize
		)
		{
			XFLOAT allPreferredArrangeSize = 0;
			DefinitionBase currDefinition = null;
			DefinitionBase nextDefinition = null;
			Xuint cStarDefinitions = 0;
			Xuint cNonStarDefinitions = definitions.Count;

			EnsureTempDefinitionsStorage(definitions.Count);

			for (Xuint i = 0; i < definitions.Count; i++)
			{
				DefinitionBase pDef = (DefinitionBase)(definitions.GetItem(i));

				if (pDef.GetUserSizeType() == GridUnitType.Star)
				{
					//if star definition, setup values for distribution calculation

					m_ppTempDefinitions[cStarDefinitions++] = pDef;

					// Note that this user value is in star units and not pixel units,
					// and thus, there is no need to layout-round.
					XFLOAT starValue = pDef.GetUserSizeValue();

					if (starValue < REAL_EPSILON)
					{
						//cache normalized star value temporary into MeasureSize
						pDef.SetMeasureArrangeSize(0.0f);
						pDef.SetSizeCache(0.0f);
					}
					else
					{
						//clipping by a max to avoid overflow when all the star values are added up.
						starValue = Math.Min(starValue, GRID_STARVALUE_MAX);

						//cache normalized star value temporary into MeasureSize
						pDef.SetMeasureArrangeSize(starValue);

						// Note that this user value is used for a computation that is cached
						// and then used in the call to CGrid.DistributeStarSpace below for
						// further calculations where the final result is layout-rounded as
						// appropriate. In other words, it doesn't seem like we need to apply
						// layout-rounding just yet.
						XFLOAT maxSize = Math.Min(GRID_STARVALUE_MAX,
							Math.Max(pDef.GetEffectiveMinSize(), pDef.GetUserMaxSize()));
						pDef.SetSizeCache(maxSize / starValue);
					}
				}
				else
				{
					//if not star definition, reduce the size available to star definitions
					bool useLayoutRounding = GetUseLayoutRounding();
					XFLOAT userSize = 0.0f;
					XFLOAT userMaxSize = useLayoutRounding
						? LayoutRound(pDef.GetUserMaxSize())
						: pDef.GetUserMaxSize();

					m_ppTempDefinitions[--cNonStarDefinitions] = pDef;

					switch (pDef.GetUserSizeType())
					{
						case GridUnitType.Pixel:
							userSize = useLayoutRounding
								? LayoutRound(pDef.GetUserSizeValue())
								: pDef.GetUserSizeValue();
							break;
						case GridUnitType.Auto:
							userSize = pDef.GetEffectiveMinSize();
							break;
					}

					pDef.SetMeasureArrangeSize(Math.Max(pDef.GetEffectiveMinSize(), Math.Min(userSize, userMaxSize)));
					allPreferredArrangeSize += pDef.GetMeasureArrangeSize();
				}
			}

			//distribute available space among star definitions.
			DistributeStarSpace(m_ppTempDefinitions, cStarDefinitions, finalSize - allPreferredArrangeSize, ref allPreferredArrangeSize);

			//if the combined size of all definitions exceeds the finalSize, take the difference away.
			if ((allPreferredArrangeSize > finalSize) && XcpAbsF(allPreferredArrangeSize - finalSize) > REAL_EPSILON)
			{
				//sort definitions to define an order for space distribution.
				SortDefinitionsForOverflowSizeDistribution(m_ppTempDefinitions, definitions.Count);
				XFLOAT sizeToDistribute = finalSize - allPreferredArrangeSize;

				for (Xuint i = 0; i < definitions.Count; i++)
				{
					XFLOAT finalSize2 = m_ppTempDefinitions[i].GetMeasureArrangeSize() +
									   (sizeToDistribute / (definitions.Count - i));

					finalSize2 = Math.Max(finalSize2, m_ppTempDefinitions[i].GetEffectiveMinSize());
					finalSize2 = Math.Min(finalSize2, m_ppTempDefinitions[i].GetMeasureArrangeSize());
					sizeToDistribute -= (finalSize2 - m_ppTempDefinitions[i].GetMeasureArrangeSize());
					m_ppTempDefinitions[i].SetMeasureArrangeSize(finalSize2);
				}
			}

			//Process definitions in original order to calculate offsets
			currDefinition = (DefinitionBase)(definitions.GetItem(0));
			currDefinition.SetFinalOffset(0.0f);

			for (Xuint i = 0; i < definitions.Count - 1; i++)
			{
				nextDefinition = (DefinitionBase)(definitions.GetItem(i + 1));
				nextDefinition.SetFinalOffset(currDefinition.GetFinalOffset() + currDefinition.GetMeasureArrangeSize());
				currDefinition = nextDefinition;
				nextDefinition = null;
			}
		}


		//------------------------------------------------------------------------
		//
		//  Method:   SortDefinitionsForSpanPreferredDistribution
		//
		//  Synopsis: Sort definitions for span processing, for the case when the element
		//                  desired Size is greater than rangeMinSize but less than rangePreferredSize.
		//
		//------------------------------------------------------------------------
		void SortDefinitionsForSpanPreferredDistribution(
			IList<DefinitionBase> ppDefinitions,
			Xuint cDefinitions)
		{
			DefinitionBase pTemp;

			for (Xuint i = 1, j; i < cDefinitions; i++)
			{
				pTemp = ppDefinitions[i];
				for (j = i; j > 0; j--)
				{
					if (pTemp.GetUserSizeType() == GridUnitType.Auto)
					{
						if (ppDefinitions[j - 1].GetUserSizeType() == GridUnitType.Auto)
						{
							if (pTemp.GetEffectiveMinSize() >= ppDefinitions[j - 1].GetEffectiveMinSize())
							{
								break;
							}
						}
					}
					else
					{
						if (ppDefinitions[j - 1].GetUserSizeType() != GridUnitType.Auto)
						{
							if (pTemp.GetPreferredSize() >= ppDefinitions[j - 1].GetPreferredSize())
							{
								break;
							}
						}
						else
						{
							break;
						}
					}

					ppDefinitions[j] = ppDefinitions[j - 1];
				}

				ppDefinitions[j] = pTemp;
			}
		}


		//------------------------------------------------------------------------
		//
		//  Method:   SortDefinitionsForSpanMaxSizeDistribution
		//
		//  Synopsis: Sort definitions for span processing, for the case when the element
		//                  desired Size is greater than rangePreferredSize but less than rangeMaxSize.
		//
		//------------------------------------------------------------------------
		void SortDefinitionsForSpanMaxSizeDistribution(
			IList<DefinitionBase> ppDefinitions,
			Xuint cDefinitions)
		{
			DefinitionBase pTemp;

			for (Xuint i = 1, j; i < cDefinitions; i++)
			{
				pTemp = ppDefinitions[i];
				for (j = i; j > 0; j--)
				{
					if (pTemp.GetUserSizeType() == GridUnitType.Auto)
					{
						if (ppDefinitions[j - 1].GetUserSizeType() == GridUnitType.Auto)
						{
							if (pTemp.GetSizeCache() >= ppDefinitions[j - 1].GetSizeCache())
							{
								break;
							}
						}
						else
						{
							break;
						}
					}
					else
					{
						if (ppDefinitions[j - 1].GetUserSizeType() != GridUnitType.Auto)
						{
							if (pTemp.GetSizeCache() >= ppDefinitions[j - 1].GetSizeCache())
							{
								break;
							}
						}
					}

					ppDefinitions[j] = ppDefinitions[j - 1];
				}

				ppDefinitions[j] = pTemp;
			}
		}


		//------------------------------------------------------------------------
		//
		//  Method: SortDefinitionsForOverflowSizeDistribution
		//
		//  Synopsis: Sort definitions for final size processing in ArrangeOverride, for the case
		//                 when the combined size of all definitions across a dimension exceeds the
		//                 finalSize in that dimension.
		//
		//------------------------------------------------------------------------
		void SortDefinitionsForOverflowSizeDistribution(
			IList<DefinitionBase> ppDefinitions,
			Xuint cDefinitions)
		{
			DefinitionBase pTemp;

			// use insertion sort...it is stable...
			for (Xuint i = 1, j; i < cDefinitions; i++)
			{
				pTemp = ppDefinitions[i];
				for (j = i; j > 0; j--)
				{
					if ((pTemp.GetMeasureArrangeSize() - pTemp.GetEffectiveMinSize())
						>= (ppDefinitions[j - 1].GetMeasureArrangeSize() - ppDefinitions[j - 1].GetEffectiveMinSize()))
					{
						break;
					}

					ppDefinitions[j] = ppDefinitions[j - 1];
				}

				ppDefinitions[j] = pTemp;
			}
		}


		//------------------------------------------------------------------------
		//
		//  Method: SortDefinitionsForStarSizeDistribution
		//
		//  Synopsis: Sort definitions for distributing star space.
		//
		//------------------------------------------------------------------------
		void SortDefinitionsForStarSizeDistribution(
				IList<DefinitionBase> ppDefinitions,
		Xuint cDefinitions
		)
		{
			DefinitionBase pTemp;

			// use insertion sort...it is stable...
			for (Xuint i = 1, j; i < cDefinitions; i++)
			{
				pTemp = ppDefinitions[i];
				for (j = i; j > 0; j--)
				{
					// Use >= instead of > to keep sort stable. If > is used,
					// sort will not be stable & size will distributed in a different
					// order than WPF.
					if (pTemp.GetSizeCache() >= ppDefinitions[j - 1].GetSizeCache())
					{
						break;
					}

					ppDefinitions[j] = ppDefinitions[j - 1];
				}

				ppDefinitions[j] = pTemp;
			}
		}

		//void OnPropertyChanged(PropertyChangedParams args)
		//{
		//	//Panel.OnPropertyChanged(args);

		//	if (args.Property == Grid.RowDefinitionsProperty
		//	    || args.Property == Grid.ColumnDefinitionsProperty)
		//	{
		//		InvalidateDefinitions();
		//	}
		//}

		//xref_ptr<CBrush> GetBorderBrush()
		//{
		//	if (!IsPropertyDefaultByIndex(Grid.BorderBrush))
		//	{
		//		CValue result;
		//			/VERIFYHR / (GetValueByIndex(Grid.BorderBrush, &result));
		//		return static_sp_cast<CBrush>(result.DetachObject());
		//	}
		//	else
		//	{
		//		return CPanel.GetBorderBrush();
		//	}
		//}

		//XTHICKNESS GetBorderThickness()
		//{
		//	if (!IsPropertyDefaultByIndex(Grid.BorderThickness))
		//	{
		//		CValue result;
		//			/VERIFYHR / (GetValueByIndex(Grid.BorderThickness, &result));
		//		return *(result.AsThickness());
		//	}
		//	else
		//	{
		//		return CPanel.GetBorderThickness();
		//	}
		//}

		//XTHICKNESS GetPadding()
		//{
		//	CValue result;
		//		/VERIFYHR / (GetValueByIndex(Grid.Padding, &result));
		//	return *(result.AsThickness());
		//}

		//XCORNERRADIUS GetCornerRadius()
		//{
		//	if (!IsPropertyDefaultByIndex(Grid.CornerRadius))
		//	{
		//		CValue result;
		//			/VERIFYHR / (GetValueByIndex(Grid.CornerRadius, &result));
		//		return *(result.AsCornerRadius());
		//	}
		//	else
		//	{
		//		return CPanel.GetCornerRadius();
		//	}
		//}

		//DirectUI.BackgroundSizing GetBackgroundSizing()
		//{
		//	CValue result;
		//		/VERIFYHR / (GetValueByIndex(Grid.BackgroundSizing, &result));
		//	return (DirectUI.BackgroundSizing)(result.AsEnum());
		//}

		//XFLOAT GetRowSpacing()
		//{
		//	CValue result;
		//	IFCFAILFAST(GetValueByIndex(Grid.RowSpacing, &result));
		//	return result.As<valueFloat>();
		//}

		//XFLOAT GetColumnSpacing()
		//{
		//	CValue result;
		//	IFCFAILFAST(GetValueByIndex(Grid.ColumnSpacing, &result));
		//	return result.As<valueFloat>();
		//}

		bool IsWithoutRowAndColumnDefinitions()
		{
			return (m_pRowDefinitions == null || m_pRowDefinitions.Count == 0) &&
				   (m_pColumnDefinitions == null || m_pColumnDefinitions.Count == 0);
		}
	}
}
