// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference StackingLayoutStrategy_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal partial class StackingLayoutStrategy : ILayoutStrategy
{
	public StackingLayoutStrategy()
	{
		Initialize();
	}

	private void Initialize()
	{
		_layoutStrategyImpl.Initialize();
	}

	public void SetLayoutDataInfoProvider(ILayoutDataInfoProvider pProvider) /*override*/
	{
		_spDataInfoProvider = pProvider;
		_layoutStrategyImpl.GetLayoutDataInfoProviderNoRef = pProvider;
	}

	#region Layout related methods

	public void BeginMeasure() /*override*/
	{
		_layoutStrategyImpl.BeginMeasure();
	}

	public void EndMeasure() /*override*/
	{
		_layoutStrategyImpl.EndMeasure();
	}

	// returns the size we should use to measure a container or header with
	// itemIndex - indicates an index of valid item or -1 for general, non-special items
	public Size GetElementMeasureSize(ElementType elementType, int elementIndex, Rect windowConstraint) /*override*/
	{
		Size pReturnValue = default;
		pReturnValue = _layoutStrategyImpl.GetElementMeasureSize(elementType, elementIndex, windowConstraint);

		return pReturnValue;
	}

	public Rect GetElementBounds(
		ElementType elementType,
		int elementIndex,
		Size containerDesiredSize,
		LayoutReference referenceInformation,
		Rect windowConstraint) /*override*/
	{
		Rect pReturnValue = default;
		pReturnValue = _layoutStrategyImpl.GetElementBounds(
			elementType,
			elementIndex,
			containerDesiredSize,
			referenceInformation,
			windowConstraint);

		return pReturnValue;
	}

	public Rect GetElementArrangeBounds(
		ElementType elementType,
		int elementIndex,
		Rect containerBounds,
		Rect windowConstraint,
		Size finalSize) /*override*/
	{
		Rect pReturnValue = default;
		pReturnValue = _layoutStrategyImpl.GetElementArrangeBounds(
			elementType,
			elementIndex,
			containerBounds,
			windowConstraint,
			finalSize);

		return pReturnValue;
	}

	public bool ShouldContinueFillingUpSpace(
		ElementType elementType,
		int elementIndex,
		LayoutReference referenceInformation,
		Rect windowToFill) /*override*/
	{
		bool pReturnValue = default;
		pReturnValue = _layoutStrategyImpl.ShouldContinueFillingUpSpace(
			elementType,
			elementIndex,
			referenceInformation,
			windowToFill);

		return pReturnValue;
	}

	public Point GetPositionOfFirstElement()
	{
		Point returnValue = default;
		returnValue = _layoutStrategyImpl.GetPositionOfFirstElement();

		return returnValue;
	}

	#endregion

	#region Estimation and virtualization related methods.

	public Orientation VirtualizationDirection
	{
		get => _layoutStrategyImpl.VirtualizationDirection;
		set => _layoutStrategyImpl.VirtualizationDirection = value;
	}

	public void EstimateElementIndex(
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
	}

	// Estimate the location of an anchor group, using items-per-group to estimate an average group extent.
	public Rect EstimateElementBounds(
		ElementType elementType,
		int elementIndex,
		EstimationReference headerReference,
		EstimationReference containerReference,
		Rect window) /*override*/
	{
		Rect pReturnValue = default;
		pReturnValue = _layoutStrategyImpl.EstimateElementBounds(
			elementType,
			elementIndex,
			headerReference,
			containerReference,
			window);

		return pReturnValue;
	}

	public Size EstimatePanelExtent(
		EstimationReference lastHeaderReference,
		EstimationReference lastContainerReference,
		Rect windowConstraint) /*override*/
	{
		Size pExtent = default;
		pExtent = _layoutStrategyImpl.EstimatePanelExtent(
			lastHeaderReference,
			lastContainerReference,
			windowConstraint);

		return pExtent;
	}

	#endregion

	#region IItemLookupPanel related

	// Estimates the index or the insertion index closest to the given point.
	public void EstimateIndexFromPoint(
		bool requestingInsertionIndex,
		Point point,
		EstimationReference reference,
		Rect windowConstraint,
		out IndexSearchHint pSearchHint,
		out ElementType pElementType,
		out int pElementIndex) /*override*/
	{
		pSearchHint = IndexSearchHint.NoHint;
		pElementType = ElementType.ItemContainer;
		pElementIndex = 0;
		_layoutStrategyImpl.EstimateIndexFromPoint(
			requestingInsertionIndex,
			point,
			reference,
			windowConstraint,
			out pSearchHint,
			out pElementType,
			out pElementIndex);
	}

	// Based on current element's index/type and action, return the next element index/type.
	public void GetTargetIndexFromNavigationAction(
		ElementType elementType,
		int elementIndex,
		KeyNavigationAction action,
		Rect windowConstraint,
		int itemIndexHintForHeaderNavigation,
		out ElementType targetElementType,
		out int targetElementIndex) /*override*/
	{
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
	public void IsIndexLayoutBoundary(
		ElementType elementType,
		int elementIndex,
		Rect windowConstraint,
		out bool pIsLeftBoundary,
		out bool pIsTopBoundary,
		out bool pIsRightBoundary,
		out bool pIsBottomBoundary) /*override*/
	{
		bool isLeftBoundary, isTopBoundary, isRightBoundary, isBottomBoundary;
		_layoutStrategyImpl.IsIndexLayoutBoundary(
			elementType,
			elementIndex,
			windowConstraint,
			out isLeftBoundary,
			out isTopBoundary,
			out isRightBoundary,
			out isBottomBoundary);

		pIsLeftBoundary = isLeftBoundary;
		pIsTopBoundary = isTopBoundary;
		pIsRightBoundary = isRightBoundary;
		pIsBottomBoundary = isBottomBoundary;
	}

	#endregion

	#region Snap points related

	public void GetRegularSnapPoints(
		out float pNearOffset,
		out float pFarOffset,
		out float pSpacing,
		out bool pHasRegularSnapPoints) /*override*/
	{
		pHasRegularSnapPoints = false;
		pHasRegularSnapPoints = _layoutStrategyImpl.GetRegularSnapPoints(
			out pNearOffset,
			out pFarOffset,
			out pSpacing);
	}

	public bool HasIrregularSnapPoints(
		ElementType elementType) /*override*/
	{
		bool returnValue = false;
		returnValue = _layoutStrategyImpl.HasIrregularSnapPoints(elementType);

		return returnValue;
	}

	public bool HasSnapPointOnElement(
		ElementType elementType,
		int elementIndex)
	{
		bool returnValue = false;
		returnValue = _layoutStrategyImpl.HasSnapPointOnElement(elementType, elementIndex);

		return returnValue;
	}

	#endregion

	// returns true if we are wrapping, needed for transitions
	public bool GetIsWrappingStrategy() /*override*/
	{
		bool returnValue = false;
		returnValue = _layoutStrategyImpl.IsWrappingStrategy;

		return returnValue;
	}

	public Rect GetElementTransitionsBounds(
		ElementType elementType,
		int elementIndex,
		Rect windowConstraint) /*override*/
	{
		Rect pReturnValue = default;
		pReturnValue = _layoutStrategyImpl.GetElementTransitionsBounds(
			elementType,
			elementIndex,
			windowConstraint);

		return pReturnValue;
	}

	#region Special elements methods

	public bool NeedsSpecialItem()
	{
		return _layoutStrategyImpl.NeedsSpecialItem();
	}

	public bool NeedsSpecialGroup()
	{
		return _layoutStrategyImpl.NeedsSpecialGroup();
	}

	public int GetSpecialItemIndex()
	{
		return _layoutStrategyImpl.GetSpecialItemIndex();
	}

	public int GetSpecialGroupIndex()
	{
		return _layoutStrategyImpl.GetSpecialGroupIndex();
	}

	public void RegisterSpecialContainerSize(int itemIndex, Size containerDesiredSize)
	{
		_layoutStrategyImpl.RegisterSpecialContainerSize(itemIndex, containerDesiredSize);
	}

	public void RegisterSpecialHeaderSize(int groupIndex, Size headerDesiredSize)
	{
		_layoutStrategyImpl.RegisterSpecialHeaderSize(groupIndex, headerDesiredSize);
	}

	#endregion

	// Notify the layout strategy of the new header placement.
	public void SetGroupHeaderStrategy(GroupHeaderStrategy strategy)
	{
		_layoutStrategyImpl.GroupHeaderStrategy = strategy;
	}
}
