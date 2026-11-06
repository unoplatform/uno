using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarLayoutStrategy
	{
		/// <inheritdoc />
		public Orientation VirtualizationDirection
		{
			get
			{
				GetVirtualizationDirection(out var value);
				return value;
			}
			set => SetVirtualizationDirection(value);
		}

		internal void BeginMeasure()
		{
			_layoutStrategyImpl.BeginMeasure();
		}

		internal void EndMeasure()
		{
			_layoutStrategyImpl.EndMeasure();
		}

		internal Size GetElementMeasureSize(ElementType elementType, int elementIndex, Rect windowConstraint)
		{
			GetElementMeasureSizeImpl(elementType, elementIndex, windowConstraint, out var size);
			return size;
		}

		internal Rect GetElementBounds(
			ElementType elementType,
			int elementIndex,
			Size containerDesiredSize,
			LayoutReference referenceInformation,
			Rect windowConstraint)
		{
			GetElementBoundsImpl(
				elementType,
				elementIndex,
				containerDesiredSize,
				referenceInformation,
				windowConstraint,
				out var rect);

			return rect;
		}

		internal Rect GetElementArrangeBounds(
			ElementType elementType,
			int elementIndex,
			Rect containerBounds,
			Rect windowConstraint,
			Size finalSize)
		{
			GetElementArrangeBoundsImpl(
				elementType,
				elementIndex,
				containerBounds,
				windowConstraint,
				finalSize,
				out var pReturnValue);

			return pReturnValue;
		}

		internal bool ShouldContinueFillingUpSpace(
			ElementType elementType,
			int elementIndex,
			LayoutReference referenceInformation,
			Rect windowToFill)
		{
			ShouldContinueFillingUpSpaceImpl(
				elementType,
				elementIndex,
				referenceInformation,
				windowToFill,
				out var shouldContinue);
			return shouldContinue;
		}

		internal Point GetPositionOfFirstElement()
		{
			GetPositionOfFirstElementImpl(out var point);
			return point;
		}

		#region ILayoutStrategy
		void ILayoutStrategy.SetLayoutDataInfoProvider(ILayoutDataInfoProvider provider) => LayoutDataInfoProvider = provider;

		Size ILayoutStrategy.GetElementMeasureSize(ElementType elementType, int elementIndex, Rect windowConstraint)
			=> GetElementMeasureSize(elementType, elementIndex, windowConstraint);

		Rect ILayoutStrategy.GetElementBounds(ElementType elementType, int elementIndex, Size containerDesiredSize, LayoutReference referenceInformation, Rect windowConstraint)
			=> GetElementBounds(elementType, elementIndex, containerDesiredSize, referenceInformation, windowConstraint);

		Rect ILayoutStrategy.GetElementArrangeBounds(ElementType elementType, int elementIndex, Rect containerBounds, Rect windowConstraint, Size finalSize)
			=> GetElementArrangeBounds(elementType, elementIndex, containerBounds, windowConstraint, finalSize);

		bool ILayoutStrategy.ShouldContinueFillingUpSpace(ElementType elementType, int elementIndex, LayoutReference referenceInformation, Rect windowToFill)
			=> ShouldContinueFillingUpSpace(elementType, elementIndex, referenceInformation, windowToFill);

		Point ILayoutStrategy.GetPositionOfFirstElement() => GetPositionOfFirstElement();

		void ILayoutStrategy.EstimateElementIndex(ElementType elementType, EstimationReference headerReference, EstimationReference containerReference, Rect window, out Rect targetRect, out int result)
			=> EstimateElementIndex(elementType, headerReference, containerReference, window, out targetRect, out result);

		Rect ILayoutStrategy.EstimateElementBounds(ElementType elementType, int elementIndex, EstimationReference headerReference, EstimationReference containerReference, Rect window)
		{
			EstimateElementBounds(elementType, elementIndex, headerReference, containerReference, window, out var result);
			return result;
		}

		Size ILayoutStrategy.EstimatePanelExtent(EstimationReference lastHeaderReference, EstimationReference lastContainerReference, Rect windowConstraint)
		{
			EstimatePanelExtent(lastHeaderReference, lastContainerReference, windowConstraint, out var result);
			return result;
		}

		// CalendarPanel keeps its own inline panel, which never calls the members below.
		void ILayoutStrategy.EstimateIndexFromPoint(bool requestingInsertionIndex, Point point, EstimationReference reference, Rect windowConstraint, out IndexSearchHint searchHint, out ElementType elementType, out int elementIndex)
			=> throw new NotSupportedException();

		void ILayoutStrategy.GetTargetIndexFromNavigationAction(ElementType elementType, int elementIndex, KeyNavigationAction action, Rect windowConstraint, int itemIndexHintForHeaderNavigation, out ElementType targetElementType, out int targetElementIndex)
			=> _layoutStrategyImpl.GetTargetIndexFromNavigationAction(elementType, elementIndex, action, windowConstraint, out targetElementType, out targetElementIndex);

		void ILayoutStrategy.IsIndexLayoutBoundary(ElementType elementType, int elementIndex, Rect windowConstraint, out bool isLeftBoundary, out bool isTopBoundary, out bool isRightBoundary, out bool isBottomBoundary)
			=> throw new NotSupportedException();

		void ILayoutStrategy.GetRegularSnapPoints(out float nearOffset, out float farOffset, out float spacing, out bool result)
			=> result = _layoutStrategyImpl.GetRegularSnapPoints(out nearOffset, out farOffset, out spacing);

		bool ILayoutStrategy.HasIrregularSnapPoints(ElementType elementType) => _layoutStrategyImpl.HasIrregularSnapPoints(elementType);

		bool ILayoutStrategy.HasSnapPointOnElement(ElementType elementType, int elementIndex)
		{
			_layoutStrategyImpl.HasSnapPointOnElement(elementType, elementIndex, out var result);
			return result;
		}

		bool ILayoutStrategy.GetIsWrappingStrategy() => _layoutStrategyImpl.IsWrappingStrategy;

		Rect ILayoutStrategy.GetElementTransitionsBounds(ElementType elementType, int elementIndex, Rect windowConstraint)
			=> throw new NotSupportedException();
		#endregion
	}
}
