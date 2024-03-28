// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{

	internal partial class CalendarLayoutStrategyImpl : LayoutStrategyBase
	{
		//public:
		public partial struct IndexCorrectionTable
		{
			//void SetCorrectionEntryForSkippedDay(int index, int correction);

			//void SetCorrectionEntryForElementStartAt(int correction);

			//int VisualIndexToActualIndex(int visualIndex);

			//internal int ActualIndexToVisualIndex(int actualIndex);

			//private:

			// currently we have up to two index correction entries:
			//    1. MonthPanel can set ElementStartAt to push all items to the right
			//    2. MonthPanel can have up to one skipped day, where we should push all items after this date to the right
			// both correction should be positive number.
			private (int first, int second)[] m_indexCorrectionTable => uno_m_indexCorrectionTable ??= new (int, int)[2];
			private (int first, int second)[] uno_m_indexCorrectionTable;
		};

		//CalendarLayoutStrategyImpl();

		// implementation of interface

		#region Layout related methods

		public void BeginMeasure() { }
		public void EndMeasure() { }

		//// returns the size we should use to measure a container or header with
		//// itemIndex - indicates an index of valid item or -1 for general, non-special items
		//wf.Size GetElementMeasureSize(
		//     xaml_controls.ElementType elementType,
		//     int elementIndex,
		//     wf.Rect windowConstraint);

		//wf.Rect GetElementBounds(
		//     xaml_controls.ElementType elementType,
		//     int elementIndex,
		//    Size containerDesiredSize,
		//     xaml_controls.LayoutReference referenceInformation,
		//     wf.Rect windowConstraint);

		//wf.Rect GetElementArrangeBounds(
		//     xaml_controls.ElementType elementType,
		//     int elementIndex,
		//     wf.Rect containerBounds,
		//     wf.Rect windowConstraint,
		//    Size finalSize);

		//bool ShouldContinueFillingUpSpace(
		//     xaml_controls.ElementType elementType,
		//     int elementIndex,
		//     xaml_controls.LayoutReference referenceInformation,
		//     wf.Rect windowToFill);

		// CalendarPanel doesn't have a group
		public Point GetPositionOfFirstElement() { return new Point(0, 0); }

		#endregion

		#region Estimation and virtualization related methods.

		//private void EstimateElementIndex(
		//     xaml_controls.ElementType elementType,
		//     xaml_controls.EstimationReference headerReference,
		//     xaml_controls.EstimationReference containerReference,
		//     wf.Rect window,
		//    out wf.Rect pTargetRect,
		//    out INT pReturnValue);

		//private void EstimateElementBounds(
		//     xaml_controls.ElementType elementType,
		//     int elementIndex,
		//     xaml_controls.EstimationReference headerReference,
		//     xaml_controls.EstimationReference containerReference,
		//     wf.Rect window,
		//    out wf.Rect pReturnValue);

		//private void EstimatePanelExtent(
		//     xaml_controls.EstimationReference lastHeaderReference,
		//     xaml_controls.EstimationReference lastContainerReference,
		//     wf.Rect windowConstraint,
		//    outSize pExtent);

		#endregion

		#region IItemLookupPanel related

		//// Based on current element's index/type and action, return the next element index/type.
		//private void GetTargetIndexFromNavigationAction(
		//     xaml_controls.ElementType elementType,
		//     int elementIndex,
		//     xaml_controls.KeyNavigationAction action,
		//     wf.Rect windowConstraint,
		//    out xaml_controls.ElementType pTargetElementType,
		//    out INT pTargetIndex);

		#endregion

		#region Snap points related

		//bool GetRegularSnapPoints(
		//    out float pNearOffset,
		//    out float pFarOffset,
		//    out float pSpacing);

		//bool HasIrregularSnapPoints(
		//     xaml_controls.ElementType elementType);

		//private void HasSnapPointOnElement(
		//     xaml_controls.ElementType elementType,
		//     int elementIndex,
		//    out bool& hasSnapPointOnElement);

		#endregion

		// CalendarPanel don't have a special item.
		public bool NeedsSpecialItem() { return false; }

		public int GetSpecialItemIndex() { return c_specialItemIndex; }

		//// set the viewport size and check if we need to remeasure (when item size changed).
		//void SetViewportSize(Size size, out bool pNeedsRemeasure);

		//void SetItemMinimumSize(Size size, out bool pNeedsRemeasure);

		public void SetRows(int rows) { m_rows = rows; }

		public void SetCols(int cols) { m_cols = cols; }

		//// the desired viewport size, this is determined by the minimum item size.
		//Size GetDesiredViewportSize() ;

		public void SetSnapPointFilterFunction(Func<int, bool> func) { m_snapPointFilterFunction = func; }

		public IndexCorrectionTable GetIndexCorrectionTable() { return m_indexCorrectionTable; }

		//private:
		//private void DetermineLineInformation(int visualIndex, out int pStackingLines, out int pVirtualizingLine, out int pStackingLine);
		//private int DetermineMaxStackingLine() ;
		//private float GetVirtualizedExtentOfItems(int itemCount, int maxStackingLine);
		//private float GetItemStackingPosition(int stackingLine);

		//wf.Rect GetItemBounds( int index);

		//private:
		// CalendarPanel is a fixed size panel, meaning that all the containers
		// get the same size, whether they want to or not
		// the cellSize will be default to {1,1}.
		// CalendarPanel will adjust this size based on the given viewport size
		// and trigger a measure/arrange pass again after the arrange pass, 
		// until the size is not changed.
		private Size m_cellSize;

		// minimum size.
		private Size m_cellMinSize;

		private int m_rows;

		private int m_cols;

		// When this is null, we use the default regular snap point behaivor.
		// When this is not null, we use the function to filter out unwanted snap point (and also we'll use irregular snap point behavior).
		private Func<int, bool> m_snapPointFilterFunction;

		private IndexCorrectionTable m_indexCorrectionTable;
	}
}
