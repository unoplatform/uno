// MUX Reference StackingLayoutStrategy.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	internal interface ILayoutStrategy
	{
		Orientation VirtualizationDirection { get; }

		void SetLayoutDataInfoProvider(ILayoutDataInfoProvider provider);

		void BeginMeasure();

		void EndMeasure();

		// returns the size we should use to measure a container or header with
		// itemIndex - indicates an index of valid item or -1 for general, non-special items
		Size GetElementMeasureSize(ElementType elementType, int elementIndex, Rect windowConstraint);

		Rect GetElementBounds(
			ElementType elementType,
			int elementIndex,
			Size containerDesiredSize,
			LayoutReference referenceInformation,
			Rect windowConstraint);

		Rect GetElementArrangeBounds(
			ElementType elementType,
			int elementIndex,
			Rect containerBounds,
			Rect windowConstraint,
			Size finalSize);

		bool ShouldContinueFillingUpSpace(
			ElementType elementType,
			int elementIndex,
			LayoutReference referenceInformation,
			Rect windowToFill);

		Point GetPositionOfFirstElement();

		void EstimateElementIndex(
			ElementType elementType,
			EstimationReference headerReference,
			EstimationReference containerReference,
			Rect window,
			out Rect targetRect,
			out int result);

		Rect EstimateElementBounds(
			ElementType elementType,
			int elementIndex,
			EstimationReference headerReference,
			EstimationReference containerReference,
			Rect window);

		Size EstimatePanelExtent(
			EstimationReference lastHeaderReference,
			EstimationReference lastContainerReference,
			Rect windowConstraint);

		void EstimateIndexFromPoint(
			bool requestingInsertionIndex,
			Point point,
			EstimationReference reference,
			Rect windowConstraint,
			out IndexSearchHint searchHint,
			out ElementType elementType,
			out int elementIndex);

		void GetTargetIndexFromNavigationAction(
			ElementType elementType,
			int elementIndex,
			KeyNavigationAction action,
			Rect windowConstraint,
			int itemIndexHintForHeaderNavigation,
			out ElementType targetElementType,
			out int targetElementIndex);

		void IsIndexLayoutBoundary(
			ElementType elementType,
			int elementIndex,
			Rect windowConstraint,
			out bool isLeftBoundary,
			out bool isTopBoundary,
			out bool isRightBoundary,
			out bool isBottomBoundary);

		void GetRegularSnapPoints(out float nearOffset, out float farOffset, out float spacing, out bool result);

		bool HasIrregularSnapPoints(ElementType elementType);

		bool HasSnapPointOnElement(ElementType elementType, int elementIndex);

		bool GetIsWrappingStrategy();

		Rect GetElementTransitionsBounds(ElementType elementType, int elementIndex, Rect windowConstraint);
	}
}
