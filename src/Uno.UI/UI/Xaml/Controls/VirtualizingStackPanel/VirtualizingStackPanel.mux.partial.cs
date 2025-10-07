// MUX Reference VirtualizingStackPanel_Partial.cpp

namespace Windows.UI.Xaml.Controls;

public partial class VirtualizingStackPanel
{
	// Gets a series of BOOLEAN values indicating whether a given index is
	// positioned on the leftmost, topmost, rightmost, or bottommost
	// edges of the layout.  This can be useful for both determining whether
	// to tilt items at the edges of rows or columns as well as providing
	// data for portal animations.
	internal static void ComputeLayoutBoundary(
		int index,
		int itemCount,
		bool isHorizontal,
		out bool isLeftBoundary,
		out bool isTopBoundary,
		out bool isRightBoundary,
		out bool isBottomBoundary)
	{
		if (isHorizontal)
		{
			isLeftBoundary = (index == 0);
			isBottomBoundary = true;
			isTopBoundary = true;
			isRightBoundary = (index == itemCount - 1);
		}
		else
		{
			isLeftBoundary = true;
			isBottomBoundary = (index == itemCount - 1);
			isTopBoundary = (index == 0);
			isRightBoundary = true;
		}
	}
}
