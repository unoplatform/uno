using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
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

	}
}
