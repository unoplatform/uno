// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class CalendarLayoutStrategy : Control
	{
		private CalendarLayoutStrategyImpl _layoutStrategyImpl;

		private ILayoutDataInfoProvider _spDataInfoProvider;

		private void SetLayoutDataInfoProviderImpl(ILayoutDataInfoProvider pProvider) /*override*/
		{
			_spDataInfoProvider = pProvider;
			_layoutStrategyImpl.SetLayoutDataInfoProviderNoRef(pProvider);
		}

		#region Layout related methods

		private void BeginMeasureImpl() /*override*/
		{
			_layoutStrategyImpl.BeginMeasure();
		}

		private void EndMeasureImpl() /*override*/
		{
			_layoutStrategyImpl.EndMeasure();
		}

		// returns the size we should use to measure a container or header with
		// itemIndex - indicates an index of valid item or -1 for general, non-special items
		private void GetElementMeasureSizeImpl(ElementType elementType, int elementIndex, Rect windowConstraint, out Size pReturnValue) /*override*/
		{
			pReturnValue = null;
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
			pReturnValue =  null;
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
			pReturnValue = null;
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
			pReturnValue = null;
			pReturnValue = !!_layoutStrategyImpl.ShouldContinueFillingUpSpace(
				elementType,
				elementIndex,
				referenceInformation,
				windowToFill);

			return;
		}

		private void GetPositionOfFirstElementImpl(out Point returnValue)
		{
			returnValue = null;
			returnValue = _layoutStrategyImpl.GetPositionOfFirstElement();

			return;
		}

		#endregion

		#region Estimation and virtualization related methods.

		private void GetVirtualizationDirectionImpl(
			out Orientation pReturnValue)
		{
			pReturnValue = _layoutStrategyImpl.GetVirtualizationDirection();
		}

		private void EstimateElementIndexImpl(
			ElementType elementType,
			EstimationReference headerReference,
			EstimationReference containerReference,
			Rect window,
			out Rect pTargetRect,
			out int pReturnValue) /*override*/
		{
			pReturnValue = null;
			_layoutStrategyImpl.EstimateElementIndex(
				elementType,
				headerReference,
				containerReference,
				window,
				pTargetRect,
				pReturnValue));

			return;
		}

		// Estimate the location of an anchor group, using items-per-group to estimate an average group extent.
		private void EstimateElementBoundsImpl(
			ElementType elementType,
			int elementIndex,
			EstimationReference headerReference,
			EstimationReference containerReference,
			Rect window,
			out Rect pReturnValue) /*override*/
		{
			pReturnValue = null;
			_layoutStrategyImpl.EstimateElementBounds(
				elementType,
				elementIndex,
				headerReference,
				containerReference,
				window,
				pReturnValue);
		}

		private void EstimatePanelExtentImpl(
			EstimationReference lastHeaderReference,
			EstimationReference lastContainerReference,
			Rect windowConstraint,
			out Size pExtent) /*override*/
		{
			pExtent = null;
			_layoutStrategyImpl.EstimatePanelExtent(
				lastHeaderReference,
				lastContainerReference,
				windowConstraint,
				pExtent);
		}

		#endregion

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
				targetElementType,
				targetElementIndex);
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
			pHasRegularSnapPoints = !!_layoutStrategyImpl.GetRegularSnapPoints(
				pNearOffset,
				pFarOffset,
				pSpacing);
		}

		private void HasIrregularSnapPointsImpl(
			ElementType elementType,
			out bool returnValue) /*override*/
		{
			returnValue = false;
			returnValue = !!_layoutStrategyImpl.HasIrregularSnapPoints(elementType);
		}

		private void HasSnapPointOnElementImpl(
			ElementType elementType,
			int elementIndex,
			out bool returnValue)
		{
			returnValue = false;
			bool result = false;
			_layoutStrategyImpl.HasSnapPointOnElement(elementType, elementIndex, result));
			returnValue = result;
		}
		#endregion

		private void GetIsWrappingStrategyImpl(out bool returnValue) /*override*/
		{
			returnValue = false;
			returnValue = !!_layoutStrategyImpl.GetIsWrappingStrategy();
		}

		private void GetElementTransitionsBoundsImpl(
			ElementType elementType,
			int elementIndex,
			Rect windowConstraint,
			out Rect pReturnValue) /*override*/
		{
			throw new NotImplementedException();
		}


		#region Special elements methods

		private bool NeedsSpecialItem()
		{
			return _layoutStrategyImpl.NeedsSpecialItem();
		}

		private int GetSpecialItemIndex()
		{
			return _layoutStrategyImpl.GetSpecialItemIndex();
		}

		#endregion


		private Size GetDesiredViewportSize()
		{
			return _layoutStrategyImpl.GetDesiredViewportSize();
		}

		private void SetSnapPointFilterFunction(Func<int, bool> func)
		{
			_layoutStrategyImpl.SetSnapPointFilterFunction(func);
		}

		Components.Moco.CalendarLayoutStrategyImpl.IndexCorrectionTable GetIndexCorrectionTable()
		{
			return _layoutStrategyImpl.GetIndexCorrectionTable();
		}

	}

	internal enum ElementType
	{
		// Structural element that holds Inline elements.
		Paragraph = 0,

		// Formatting element that may hold other Inline elements.
		Inline = 1,

		// An explicit line break within a Paragraph.
		LineBreak = 2,

		// An embedded object (UIElement).
		Object = 3,
	}
}
