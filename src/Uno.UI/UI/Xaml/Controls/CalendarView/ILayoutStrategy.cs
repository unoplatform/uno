using System;
using System.Linq;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	internal interface ILayoutStrategy
	{
		/*
		 *
		 * Uno: To get code to compile
		 *
		 */
		Orientation VirtualizationDirection { get; }

		/*
		 *
		 * From CalendarLayoutStrategyImpl.h.cs, the "Layout related methods" region
		 *
		 */

		void BeginMeasure();

		void EndMeasure();

		//// returns the size we should use to measure a container or header with
		//// itemIndex - indicates an index of valid item or -1 for general, non-special items
		//Size GetElementMeasureSize(
		//	ElementType elementType,
		//	int elementIndex,
		//	Rect windowConstraint);

		//Rect GetElementBounds(
		//	ElementType elementType,
		//	int elementIndex,
		//	Size containerDesiredSize,
		//	LayoutReference referenceInformation,
		//	Rect windowConstraint);

		//Rect GetElementArrangeBounds(
		//	ElementType elementType,
		//	int elementIndex,
		//	Rect containerBounds,
		//	Rect windowConstraint,
		//	Size finalSize);

		//bool ShouldContinueFillingUpSpace(
		//	ElementType elementType,
		//	int elementIndex,
		//	LayoutReference referenceInformation,
		//	Rect windowToFill);

		//// CalendarPanel doesn't have a group
		//public Point GetPositionOfFirstElement();
	}
}
