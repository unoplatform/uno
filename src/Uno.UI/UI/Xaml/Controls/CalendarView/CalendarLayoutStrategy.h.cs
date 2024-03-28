// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	internal partial class CalendarLayoutStrategy : ILayoutStrategy
	{
		//public:

		//private void SetLayoutDataInfoProviderImpl( ILayoutDataInfoProvider pProvider);

		//#pragma region Layout related methods

		//private void BeginMeasureImpl();
		//private void EndMeasureImpl();

		//// returns the size we should use to measure a container or header with
		//// itemIndex - indicates an index of valid item or -1 for general, non-special items
		//private void GetElementMeasureSizeImpl(
		//     ElementType elementType,
		//     int elementIndex,
		//    Rect windowConstraint,
		//    outSize pReturnValue);

		//private void GetElementBoundsImpl(
		//     ElementType elementType,
		//     int elementIndex,
		//    Size containerDesiredSize,
		//     LayoutReference referenceInformation,
		//    Rect windowConstraint,
		//    outRect pReturnValue);

		//private void GetElementArrangeBoundsImpl(
		//     ElementType elementType,
		//     int elementIndex,
		//    Rect containerBounds,
		//    Rect windowConstraint,
		//    Size finalSize,
		//    outRect pReturnValue);

		//private void ShouldContinueFillingUpSpaceImpl(
		//     ElementType elementType,
		//     int elementIndex,
		//     LayoutReference referenceInformation,
		//    Rect windowToFill,
		//    out BOOLEAN pReturnValue);

		//private void GetPositionOfFirstElementImpl(outPoint returnValue);

		//#pragma endregion

		//#pragma region Estimation and virtualization related methods.

		//private void GetVirtualizationDirectionImpl(
		//    out Orientation pReturnValue);
		internal void GetVirtualizationDirection(out Orientation orientation) => orientation = _layoutStrategyImpl.VirtualizationDirection;

		//private void EstimateElementIndexImpl(
		//     ElementType elementType,
		//     EstimationReference headerReference,
		//     EstimationReference containerReference,
		//    Rect window,
		//    outRect pTargetRect,
		//    out INT pReturnValue);

		//private void EstimateElementBoundsImpl(
		//     ElementType elementType,
		//     int elementIndex,
		//     EstimationReference headerReference,
		//     EstimationReference containerReference,
		//    Rect window,
		//    outRect pReturnValue);

		//private void EstimatePanelExtentImpl(
		//     EstimationReference lastHeaderReference,
		//     EstimationReference lastContainerReference,
		//    Rect windowConstraint,
		//    outSize pExtent);

		//#pragma endregion

		//#pragma region IItemLookupPanel related

		//// Estimates the index or the insertion index closest to the given point.
		//private void EstimateIndexFromPointImpl(
		//     bool requestingInsertionIndex,
		//    Point point,
		//     EstimationReference reference,
		//    Rect windowConstraint,
		//    out IndexSearchHint pSearchHint,
		//    out ElementType pElementType,
		//    out INT pElementIndex);

		//// Based on current element's index/type and action, return the next element index/type.
		//private void GetTargetIndexFromNavigationActionImpl(
		//     ElementType elementType,
		//     int elementIndex,
		//     KeyNavigationAction action,
		//    Rect windowConstraint,
		//     int itemIndexHintForHeaderNavigation,
		//    out ElementType targetElementType,
		//    out INT targetElementIndex);

		//// Determines whether or not the given item index
		//// is a layout boundary.
		//private void IsIndexLayoutBoundaryImpl(
		//     ElementType elementType,
		//     int elementIndex,
		//    Rect windowConstraint,
		//    out BOOLEAN isLeftBoundary,
		//    out BOOLEAN isTopBoundary,
		//    out BOOLEAN isRightBoundary,
		//    out BOOLEAN isBottomBoundary);

		//#pragma endregion

		//#pragma region Snap points related

		//private void GetRegularSnapPointsImpl(
		//    out FLOAT pNearOffset,
		//    out FLOAT pFarOffset,
		//    out FLOAT pSpacing,
		//    out BOOLEAN pHasRegularSnapPoints);

		//private void HasIrregularSnapPointsImpl(
		//     ElementType elementType,
		//    out BOOLEAN returnValue);

		//private void HasSnapPointOnElementImpl(
		//     ElementType elementType,
		//     int elementIndex,
		//    out BOOLEAN returnValue);

		//#pragma endregion

		//// returns true if we are wrapping, needed for transitions
		//private void GetIsWrappingStrategyImpl(out BOOLEAN returnValue);

		//private void GetElementTransitionsBoundsImpl(
		//     ElementType elementType,
		//     int elementIndex,
		//    Rect windowConstraint, 
		//    outRect pReturnValue);


		// internal configuration
		public void SetVirtualizationDirection(Orientation direction) { _layoutStrategyImpl.VirtualizationDirection = direction; }

		public void SetGroupPadding(Thickness padding) { _layoutStrategyImpl.GroupPadding = padding; }

		//bool NeedsSpecialItem() ;

		//int GetSpecialItemIndex() ;

		public void SetViewportSize(Size size, out bool pNeedsRemeasure) { _layoutStrategyImpl.SetViewportSize(size, out pNeedsRemeasure); }

		public void SetItemMinimumSize(Size size, out bool pNeedsRemeasure) { _layoutStrategyImpl.SetItemMinimumSize(size, out pNeedsRemeasure); }

		public void SetRows(int rows) { _layoutStrategyImpl.SetRows(rows); }

		public void SetCols(int cols) { _layoutStrategyImpl.SetCols(cols); }

		//Components.Moco.CalendarLayoutStrategyImpl.IndexCorrectionTable& GetIndexCorrectionTable();

		//Size GetDesiredViewportSize() ;

		//private void SetSnapPointFilterFunction(
		//     std.function<HRESULT( int itemIndex, out bool pHasSnapPoint)> func);

		//private:
		private CalendarLayoutStrategyImpl _layoutStrategyImpl = new CalendarLayoutStrategyImpl();

		private ILayoutDataInfoProvider _spDataInfoProvider;
	}
}
