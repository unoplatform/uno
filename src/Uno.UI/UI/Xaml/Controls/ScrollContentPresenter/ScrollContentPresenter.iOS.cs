using Windows.Foundation;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollContentPresenter
	{
		public Rect MakeVisible(UIElement visual, Rect rectangle)
		{
			// NOTE: We should re-use the computation of the offsets and then use the Set

			if (Native is UIKit.UIScrollView nativeScroller)
			{
				ScrollViewExtensions.BringIntoView(nativeScroller, visual, BringIntoViewMode.ClosestEdge);
			}
			else if (Native is ListViewBaseScrollContentPresenter lv)
			{
				ScrollViewExtensions.BringIntoView(lv.NativePanel, visual, BringIntoViewMode.ClosestEdge);
			}

			return rectangle;
		}
	}
}
