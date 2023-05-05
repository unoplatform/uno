#if !IS_UNIT_TESTS
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using static System.Math;

namespace Windows.UI.Xaml.Controls
{
#if __ANDROID__ || __IOS__
	internal partial class ManagedItemsStackPanelLayout : ManagedVirtualizingPanelLayout
#else
	partial class ItemsStackPanelLayout
#endif
	{
#if __ANDROID__ || __IOS__
		public override Orientation ScrollOrientation => Orientation;
#endif

		private protected override Line CreateLine(GeneratorDirection fillDirection, double extentOffset, double availableBreadth, Uno.UI.IndexPath nextVisibleItem)
		{
			if (ShouldInsertReorderingView(extentOffset) && GetAndUpdateReorderingIndex() is { } reorderingIndex)
			{
				nextVisibleItem = reorderingIndex;
			}

			var item = GetFlatItemIndex(nextVisibleItem);
			var view = Generator.DequeueViewForItem(item);

			AddView(view, fillDirection, extentOffset, 0);

			return new Line(item, (view, nextVisibleItem));
		}

		protected override int GetItemsPerLine() => 1;

		protected override Rect GetElementArrangeBounds(int elementIndex, Rect containerBounds, Size windowConstraint, Size finalSize)
		{
			// we will give the container what it requested if bigger than the constraint, and let the clipping occur by the scrollviewer
			// we will give the container the constraint of the window (the viewport really) so that it can be laid out inside of the
			// viewport. Basically this means that a bigger container does not influence the alignment of smaller elements.
			// we do not use the finalsize, because that will represent the largest element in the viewport.
			var breadth = Max(GetBreadth(containerBounds), GetBreadth(windowConstraint));

			// unfortunate, but incorrectly configured panels (for instance, panel is set to orient horizontally, where scrollviewer is set to enable scrolling vertically
			// will potentially have infinity here. Also, the listview itself might have been inside of scrollviewer that allowed infinite in this direction
			breadth = Min(breadth, GetBreadth(finalSize));
			SetBreadth(ref containerBounds, breadth);

			// Uno TODO
			// result.* SizeFromRectInNonVirtualizingDirection() -= GetGroupPaddingAtStart().* SizeInNonVirtualizingDirection() + GetGroupPaddingAtEnd().* SizeInNonVirtualizingDirection();

			return containerBounds;
		}
	}
}

#endif
