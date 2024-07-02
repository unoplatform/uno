using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// A border layouter, to apply Padding to the border.
/// </summary>
partial class Border
#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
	: ICustomClippingElement
#endif
{
	protected override Size MeasureOverride(Size availableSize)
	{
		Size childAvailableSize = default;

		Size combined = HelperGetCombinedThickness(this);

		// Get the child to measure it - if any.
		//If we have a child
		if (Child is { } child)
		{
			// Remove combined size from child's reference size.
			childAvailableSize.Width = Math.Max(0.0f, availableSize.Width - combined.Width);
			childAvailableSize.Height = Math.Max(0.0f, availableSize.Height - combined.Height);

			var desiredSize = MeasureElement(child, childAvailableSize);

			//IFC(pChild->EnsureLayoutStorage());

			// Desired size would be my child's desired size plus the border
			desiredSize.Width = desiredSize.Width + combined.Width;
			desiredSize.Height = desiredSize.Height + combined.Height;
			return desiredSize;
		}
		else
		{
			return combined;
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		// Get the child to arrange it - if any.
		//If we have a child
		if (Child is { } child)
		{
			Rect childRect = HelperGetInnerRect(this, finalSize);

			// Give the child the inner rectangle as the available size
			// and ask it to arrange itself within this rectangle.
			// Uno TODO: child.Arrange(childRect) doesn't work properly here while it should.
			//           It fails some tests, e.g, TestVariousArrangedPosition
			base.ArrangeElement(child, childRect);
		}

		return finalSize;
	}
}
