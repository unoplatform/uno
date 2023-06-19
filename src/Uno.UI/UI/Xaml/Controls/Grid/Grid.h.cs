// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Uno.Collections;
using Uno.Helpers;
using CRowDefinitionCollection = Microsoft.UI.Xaml.Controls.RowDefinitionCollection;
using CColumnDefinitionCollection = Microsoft.UI.Xaml.Controls.ColumnDefinitionCollection;

using Xuint = System.Int32;
using XFLOAT = System.Double;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Grid : Panel
	{
		//const int GRID_STARVALUE_MAX = (XFLOAT_MAX / XUint_MAX - 1);
		const int GRID_STARVALUE_MAX = int.MaxValue;


		[Flags]
		private enum GridFlags : byte
		{
			None = 0x00,
			HasStarRows = 0x01,
			HasStarColumns = 0x02,
			HasAutoRowsAndStarColumn = 0x04,
			DefinitionsChanged = 0x08,
		}


		// private
		// Stores RowSpan or ColumnSpan information.
		[StructLayout(LayoutKind.Sequential)]
		struct SpanStoreEntry
		{
			internal SpanStoreEntry(Xuint spanStart, Xuint spanCount, XFLOAT desiredSize, bool isColumnDefinition)
			{
				m_spanStart = spanStart;
				m_spanCount = spanCount;
				m_desiredSize = desiredSize;
				m_isColumnDefinition = isColumnDefinition;
			}

			// Starting index of the cell.
			internal Xuint m_spanStart;

			// Span value of the cell.
			internal Xuint m_spanCount;

			// DesiredSize of the element in the cell.
			internal XFLOAT m_desiredSize;

			internal bool m_isColumnDefinition;

		}

		static int c_spanStoreStackVectorSize = 16;

		static int c_cellCacheStackVectorSize = 16;

		//typedef Jupiter.stack_vector<CellCache, c_cellCacheStackVectorSize> CellCacheStackVector;
		class CellCacheStackVector : StackVector<CellCache>
		{
			internal CellCacheStackVector(int childrenCount) : base(c_cellCacheStackVectorSize, childrenCount)
			{
			}
		}

		//typedef Jupiter.stack_vector<SpanStoreEntry, c_spanStoreStackVectorSize> SpanStoreStackVector;
		class SpanStoreStackVector : StackVector<SpanStoreEntry>
		{
			internal SpanStoreStackVector() : base(c_spanStoreStackVectorSize)
			{
			}
		}

		//void InitializeDefinitionStructure();

		//void ValidateDefinitions(
		//	CDefinitionCollectionBase definitions,
		//	bool treatStarAsAuto);

		//CellGroups ValidateCells(
		//	CUIElementCollectionWrapper

		//		&children,
		//ref CellCacheStackVector & cellCacheVector);

		//Xuint GetRowIndex(CUIElement child);

		//Xuint GetColumnIndex(CUIElement child);

		//Xuint GetRowSpan(CUIElement child);

		//Xuint GetColumnSpan(CUIElement child);

		//CDefinitionBase GetRowNoRef(CUIElement pChild);

		//CDefinitionBase GetColumnNoRef(CUIElement pChild);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void SetGridFlags(GridFlags mask)
		{
			m_gridFlags |= mask;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool TrySetGridFlags(GridFlags mask)
		{
			if (HasGridFlags(mask))
			{
				return false;
			}

			m_gridFlags |= mask;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ClearGridFlags(GridFlags mask)
		{
			m_gridFlags &= ~mask;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool HasGridFlags(GridFlags mask)
		{
			return (m_gridFlags & mask) == mask;
		}

		//void LockDefinitions();

		//void UnlockDefinitions();

		//void MeasureCellsGroup(
		//	Xuint cellsHead,
		//	Xuint cellCount,
		//	float rowSpacing,
		//	float columnSpacing,
		//	bool ignoreColumnDesiredSize,
		//	bool forceRowToInfinity,
		//	ref CellCacheStackVector

		//		&cellCacheVector);

		//void MeasureCell(
		//	CUIElement child,
		//	CellUnitTypes rowHeightTypes,
		//	CellUnitTypes columnWidthTypes,
		//	bool forceRowToInfinity,
		//	float rowSpacing,
		//	float columnSpacing);

		//CellUnitTypes GetLengthTypeForRange(
		//	CDOCollection definitions,
		//	Xuint start,
		//	Xuint count);

		//float GetAvailableSizeForRange(
		//	CDOCollection definitions,
		//	Xuint start,
		//	Xuint count,
		//	float spacing);

		//float GetFinalSizeForRange(
		//	CDOCollection definitions,
		//	Xuint start,
		//	Xuint count,
		//	float spacing);

		//float GetDesiredInnerSize(CDOCollection definitions);

		//void SortDefinitionsForOverflowSizeDistribution(
		//	reads_

		//(cDefinitions) CDefinitionBase ppDefinitions,
		//Xuint cDefinitions);

		//void SortDefinitionsForStarSizeDistribution(
		//	reads_

		//(cDefinitions) CDefinitionBase ppDefinitions,
		//Xuint cDefinitions);

		//void SortDefinitionsForSpanPreferredDistribution(
		//	reads_

		//(cDefinitions) CDefinitionBase ppDefinitions,
		//Xuint cDefinitions);

		//void SortDefinitionsForSpanMaxSizeDistribution(
		//	reads_

		//(cDefinitions) CDefinitionBase ppDefinitions,
		//Xuint cDefinitions);

		//void SetFinalSize(
		//	CDefinitionCollectionBase definitions,
		//	XFLOAT finalSize);

		//void ResolveStar(
		//	CDefinitionCollectionBase definitions,
		//	XFLOAT availableSize);

		//void EnsureTempDefinitionsStorage(Xuint minCount);

		//void DistributeStarSpace(
		//	reads_

		//(cStarDefinitions) CDefinitionBase ppStarDefinitions,

		//Xuint cStarDefinitions,
		//	XFLOAT availableSize,
		//ref XFLOAT pTotalResolvedSize);

		//void RegisterSpan(
		//	ref SpanStoreStackVector

		//		&spanStore,
		//Xuint spanStart,
		//Xuint spanCount,
		//float desiredtSize,
		//bool isColumnDefinition);

		//void EnsureMinSizeInDefinitionRange(
		//	CDOCollection definitions,
		//	Xuint spanStart,
		//	Xuint spanCount,
		//	float spacing,
		//	float childDesiredSize);

		//bool IsWithoutRowAndColumnDefinitions();

		//xref_ptr<CBrush> GetBorderBrush() override sealed;

		//XTHICKNESS GetBorderThickness() override sealed;

		//XTHICKNESS GetPadding() override sealed;

		//XCORNERRADIUS GetCornerRadius() override sealed;

		//DirectUI.BackgroundSizing GetBackgroundSizing() override sealed;

		//float GetRowSpacing();

		//float GetColumnSpacing();

		//void CloneElementPropertiesForCachedTemplateExpansion(
		//	CDependencyObject pCDependencyObject) override;

		//// protected
		//CGrid(CCoreServices pCore)
		//	: CPanel(pCore)
		//{
		//}

		//void MeasureOverride(XSIZEF availableSize, XSIZEF& desiredSize) override sealed;

		//void ArrangeOverride(XSIZEF finalSize, XSIZEF& newFinalSize) override sealed;

		//virtual void OnPropertyChanged(PropertyChangedParams& args) override final;

		//// public
		//~CGrid();

		//DECLARE_CREATE(CGrid);

		//__override virtual KnownTypeIndex GetTypeIndex()
		//{
		//	return DependencyObjectTraits<CGrid>.Index;
		//}

		internal void InvalidateDefinitions()
		{
			if (TrySetGridFlags(GridFlags.DefinitionsChanged))
			{
				InvalidateMeasure();
			}
		}

		// public

		CRowDefinitionCollection m_pRowDefinitions; // User specified rows.
		CColumnDefinitionCollection m_pColumnDefinitions; // User specified columns.

		// private
		CRowDefinitionCollection m_pRows; // Effective row collection.
		CColumnDefinitionCollection m_pColumns; // Effective column collection.

		// This is a temporary storage that is released after arrange.
		// Note the ScopeExit in ArrangeOveride
		DefinitionBase[] m_ppTempDefinitions; // Temporary definitions storage.
		Xuint m_cTempDefinitions; // Size in elements of temporary definitions storage


		GridFlags
			m_gridFlags =
				GridFlags.None; // Internal grid flags used for layout processing. Should have enough bits to fit all flags set by SetGridFlag.

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private double XcpAbsF(double rValue) => Math.Abs(rValue);
	}
}
