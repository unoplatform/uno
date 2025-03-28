using DirectUI;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls;

public partial class WrapGrid
{
	internal static void ComputeAlignmentOffsets(
		// The alignment we're computing offsets for (this is either a
		// HorizontalAlignment or VerticalAlignment value we've forced into a int)
		int alignment,
		// The available width (or height, depending on orientation)
		double availableSize,
		// The width (or height, depending on orientation) required to layout all of
		// the child items
		double requiredSize,
		// The total number of lines required to layout all of the child items
		int totalLines,
		// The calculated initial top/left offset for alignment
		out double pStartingOffset,
		// The calculated offset required between items for alignment
		out double pJustificationOffset)
	{
		// Ensure the HorizontalAlignment and VerticalAlignment enum values
		// correctly match up because we're making the assumption they do (when we
		// pass them in as the value of our alignment property).  This is a hack,
		// but the enum values never change and the static asserts will create a
		// build error if they do.  If this ever fails, just use some clipboard
		// inheritance to specialize both horizontal and vertical versions of this
		// method.
		MUX_ASSERT((int)HorizontalAlignment.Left == (int)VerticalAlignment.Top, "HorizontalAlignment.Left integral value does not equal VerticalAlignment.Top integral value!");
		MUX_ASSERT((int)HorizontalAlignment.Center == (int)VerticalAlignment.Center, "HorizontalAlignment.Center integral value does not equal VerticalAlignment.Center integral value!");
		MUX_ASSERT((int)HorizontalAlignment.Right == (int)VerticalAlignment.Bottom, "HorizontalAlignment.Right integral value does not equal VerticalAlignment.Bottom integral value!");
		MUX_ASSERT((int)HorizontalAlignment.Stretch == (int)VerticalAlignment.Stretch, "HorizontalAlignment.Stretch integral value does not equal VerticalAlignment.Stretch integral value!");

		pStartingOffset = 0.0;
		pJustificationOffset = 0.0;

		// There's nothing to align if we have infinite space.  Note that we default
		// to Top or Left alignment for infinite or otherwise unspecified values.
		if (DoubleUtil.IsInfinity(availableSize))
		{
			return;
		}
		else if (alignment == (int)VerticalAlignment.Center)
		{
			pStartingOffset = DoubleUtil.Max((availableSize - requiredSize) / 2.0, 0.0);
		}
		else if (alignment == (int)VerticalAlignment.Bottom) // or HorizontalAlignment_Right
		{
			pStartingOffset = DoubleUtil.Max(availableSize - requiredSize, 0.0);
		}
		else if (alignment == (int)VerticalAlignment.Stretch)
		{
			pJustificationOffset = DoubleUtil.Max((availableSize - requiredSize) / (double)(totalLines + 1), 0.0);
		}
	}
}
