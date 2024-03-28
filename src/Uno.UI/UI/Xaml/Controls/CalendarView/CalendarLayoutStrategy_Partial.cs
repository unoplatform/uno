// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarLayoutStrategy
	{
		// TODO UNO
		internal ILayoutDataInfoProvider LayoutDataInfoProvider
		{
			get => _spDataInfoProvider;
			set
			{
				_spDataInfoProvider = value;
				_layoutStrategyImpl.GetLayoutDataInfoProviderNoRef = value;
			}
		}

		#region Layout related methods

		void ILayoutStrategy.BeginMeasure()
		{
			_layoutStrategyImpl.BeginMeasure();
		}

		void ILayoutStrategy.EndMeasure()
		{
			_layoutStrategyImpl.EndMeasure();
		}

		// returns the size we should use to measure a container or header with
		// itemIndex - indicates an index of valid item or -1 for general, non-special items
		private void GetElementMeasureSizeImpl(ElementType elementType, int elementIndex, Rect windowConstraint, out Size pReturnValue) /*override*/
		{
			pReturnValue = default;
			pReturnValue = _layoutStrategyImpl.GetElementMeasureSize(elementType, elementIndex, windowConstraint);
		}

		private void GetElementBoundsImpl(
			ElementType elementType,
			int elementIndex,
			Size containerDesiredSize,
			LayoutReference referenceInformation,
			Rect windowConstraint,
			out Rect pReturnValue) /*override*/
		{
			pReturnValue = default;
			pReturnValue = _layoutStrategyImpl.GetElementBounds(
				elementType,
				elementIndex,
				containerDesiredSize,
				referenceInformation,
				windowConstraint);
		}

		private void GetElementArrangeBoundsImpl(
			ElementType elementType,
			int elementIndex,
			Rect containerBounds,
			Rect windowConstraint,
			Size finalSize,
			out Rect pReturnValue) /*override*/
		{
			pReturnValue = default;
			pReturnValue = _layoutStrategyImpl.GetElementArrangeBounds(
				elementType,
				elementIndex,
				containerBounds,
				windowConstraint,
				finalSize);

			return;
		}

		private void ShouldContinueFillingUpSpaceImpl(
			ElementType elementType,
			int elementIndex,
			LayoutReference referenceInformation,
			Rect windowToFill,
			out bool pReturnValue) /*override*/
		{
			pReturnValue = default;
			pReturnValue = _layoutStrategyImpl.ShouldContinueFillingUpSpace(
				elementType,
				elementIndex,
				referenceInformation,
				windowToFill);

			return;
		}

		private void GetPositionOfFirstElementImpl(out Point returnValue)
		{
			returnValue = _layoutStrategyImpl.GetPositionOfFirstElement();

			return;
		}

		#endregion

		#region Estimation and virtualization related methods.
#if false
		private void GetVirtualizationDirectionImpl(
			out Orientation pReturnValue)
		{
			pReturnValue = _layoutStrategyImpl.VirtualizationDirection;
		}
#endif

		internal void EstimateElementIndex(
			ElementType elementType,
			EstimationReference headerReference,
			EstimationReference containerReference,
			Rect window,
			out Rect pTargetRect,
			out int pReturnValue) /*override*/
		{
			pReturnValue = default;
			_layoutStrategyImpl.EstimateElementIndex(
				elementType,
				headerReference,
				containerReference,
				window,
				out pTargetRect,
				out pReturnValue);

			return;
		}

		// Estimate the location of an anchor group, using items-per-group to estimate an average group extent.
		internal void EstimateElementBounds(
			ElementType elementType,
			int elementIndex,
			EstimationReference headerReference,
			EstimationReference containerReference,
			Rect window,
			out Rect pReturnValue) /*override*/
		{
			pReturnValue = default;
			_layoutStrategyImpl.EstimateElementBounds(
				elementType,
				elementIndex,
				headerReference,
				containerReference,
				window,
				out pReturnValue);
		}

		internal void EstimatePanelExtent(
			EstimationReference lastHeaderReference,
			EstimationReference lastContainerReference,
			Rect windowConstraint,
			out Size pExtent) /*override*/
		{
			pExtent = default;
			_layoutStrategyImpl.EstimatePanelExtent(
				lastHeaderReference,
				lastContainerReference,
				windowConstraint,
				out pExtent);
		}

		#endregion

#if false
		#region IItemLookupPanel related
		// Estimates the index or the insertion index closest to the given point.
		private void EstimateIndexFromPointImpl(
			bool requestingInsertionIndex,
			Point point,
			EstimationReference reference,
			Rect windowConstraint,
			out IndexSearchHint pSearchHint,
			out ElementType pElementType,
			out int pElementIndex) /*override*/
		{
			throw new NotImplementedException();
		}

		// Based on current element's index/type and action, return the next element index/type.
		private void GetTargetIndexFromNavigationActionImpl(
			ElementType elementType,
			int elementIndex,
			KeyNavigationAction action,
			Rect windowConstraint,
			int itemIndexHintForHeaderNavigation,
			out ElementType targetElementType,
			out int targetElementIndex) /*override*/
		{
			targetElementType = ElementType.ItemContainer;
			targetElementIndex = 0;
			_layoutStrategyImpl.GetTargetIndexFromNavigationAction(
				elementType,
				elementIndex,
				action,
				windowConstraint,
				out targetElementType,
				out targetElementIndex);
		}

		// Determines whether or not the given item index
		// is a layout boundary.
		private void IsIndexLayoutBoundaryImpl(
			ElementType elementType,
			int elementIndex,
			Rect windowConstraint,
			out bool pIsLeftBoundary,
			out bool pIsTopBoundary,
			out bool pIsRightBoundary,
			out bool pIsBottomBoundary) /*override*/
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Snap points related

		private void GetRegularSnapPointsImpl(
			out float pNearOffset,
			out float pFarOffset,
			out float pSpacing,
			out bool pHasRegularSnapPoints) /*override*/
		{
			pHasRegularSnapPoints = false;
			pHasRegularSnapPoints = !_layoutStrategyImpl.GetRegularSnapPoints(
				out pNearOffset,
				out pFarOffset,
				out pSpacing);
		}

		private void HasIrregularSnapPointsImpl(
			ElementType elementType,
			out bool returnValue) /*override*/
		{
			returnValue = false;
			returnValue = _layoutStrategyImpl.HasIrregularSnapPoints(elementType);
		}

		private void HasSnapPointOnElementImpl(
			ElementType elementType,
			int elementIndex,
			out bool returnValue)
		{
			returnValue = false;
			bool result = false;
			_layoutStrategyImpl.HasSnapPointOnElement(elementType, elementIndex, out result);
			returnValue = result;
		}
		#endregion

		private void GetIsWrappingStrategyImpl(out bool returnValue) /*override*/
		{
			returnValue = false;
			returnValue = !_layoutStrategyImpl.IsWrappingStrategy;
		}

		private void GetElementTransitionsBoundsImpl(
			ElementType elementType,
			int elementIndex,
			Rect windowConstraint,
			out Rect pReturnValue) /*override*/
		{
			throw new NotImplementedException();
		}
#endif

		#region Special elements methods
		internal bool NeedsSpecialItem()
		{
			return _layoutStrategyImpl.NeedsSpecialItem();
		}

		internal int GetSpecialItemIndex()
		{
			return _layoutStrategyImpl.GetSpecialItemIndex();
		}

		#endregion

		internal Size GetDesiredViewportSize()
		{
			return _layoutStrategyImpl.GetDesiredViewportSize();
		}

		internal void SetSnapPointFilterFunction(Func<int, bool> func)
		{
			_layoutStrategyImpl.SetSnapPointFilterFunction(func);
		}

		internal CalendarLayoutStrategyImpl.IndexCorrectionTable GetIndexCorrectionTable()
		{
			return _layoutStrategyImpl.GetIndexCorrectionTable();
		}



	}
}
