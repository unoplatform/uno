#if UNO_REFERENCE_API || __MACOS__
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBase
	{
		private int PageSize
		{
			get
			{
				if (VirtualizingPanel is null)
				{
					return 0;
				}

				var layouter = VirtualizingPanel.GetLayouter();
				var firstVisibleIndex = layouter.FirstVisibleIndex;
				var lastVisibleIndex = layouter.LastVisibleIndex;
				if (lastVisibleIndex == -1)
				{
					return 0;
				}

				return lastVisibleIndex - firstVisibleIndex + 1;
			}
		}

		private void AddItems(int firstItem, int count, int section)
		{
			if (VirtualizingPanel != null)
			{
				VirtualizingPanel.GetLayouter().AddItems(firstItem, count, section);
			}
			else
			{
				Refresh();
			}
		}

		private void RemoveItems(int firstItem, int count, int section)
		{
			if (VirtualizingPanel != null)
			{
				VirtualizingPanel.GetLayouter().RemoveItems(firstItem, count, section);
			}
			else
			{
				Refresh();
			}
		}

		private void AddGroup(int groupIndexInView)
		{
			Refresh();
		}

		private void RemoveGroup(int groupIndexInView)
		{
			Refresh();
		}

		private void ReplaceGroup(int groupIndexInView)
		{
			Refresh();
		}

		private ContentControl ContainerFromGroupIndex(int groupIndex) => throw new NotImplementedException();

		private void TryLoadMoreItems()
		{
			if (VirtualizingPanel.GetLayouter() is { } layouter)
			{
				TryLoadMoreItems(layouter.LastVisibleIndex);
			}
		}

#if !__MACOS__
		public void ScrollIntoView(object item) => ScrollIntoView(item, ScrollIntoViewAlignment.Default);

		public void ScrollIntoView(object item, ScrollIntoViewAlignment alignment)
		{
			if (ContainerFromItem(item) is UIElement element)
			{
				// The container we want to jump to is already materialized, so just jump to it.
				// This means we're in a non-virtualizing panel or in a virtualizing panel where the container we want is materialized for some reason (e.g. partially in view)
				ScrollIntoViewFastPath(element, alignment);
			}
			else if (VirtualizingPanel?.GetLayouter() is { } layouter)
			{
				layouter.ScrollIntoView(item, alignment);
			}
		}

		private void ScrollIntoViewFastPath(UIElement element, ScrollIntoViewAlignment alignment)
		{
			if (ScrollViewer is { } sv && sv.Presenter is { } presenter)
			{
				var offsetXY = element.TransformToVisual(presenter).TransformPoint(Point.Zero);

				var (newOffset, elementLength, presenterOffset, presenterViewportLength) =
					ItemsPanelRoot.PhysicalOrientation is Orientation.Vertical
						? (offsetXY.Y, element.ActualSize.Y, presenter.VerticalOffset, presenter.ViewportHeight)
						: (offsetXY.X, element.ActualSize.X, presenter.HorizontalOffset, presenter.ViewportWidth);

				if (presenterOffset < newOffset && newOffset + elementLength < presenterOffset + presenterViewportLength)
				{
					// if the element is within the visible viewport, do nothing.
					return;
				}

				// If we use the above offset directly, the item we want to jump to will be the start of the viewport, i.e. leading
				if (alignment is ScrollIntoViewAlignment.Default)
				{
					if (presenterOffset < newOffset)
					{
						// scroll one "viewport page" less: this brings the element's start right after the viewport's length ends
						// we then scroll again by elementLength so that the end of the element is the end of the viewport
						newOffset += (-presenterViewportLength) + elementLength;
					}
				}

				if (ItemsPanelRoot.PhysicalOrientation is Orientation.Vertical)
				{
					sv.ScrollToVerticalOffset(newOffset);
				}
				else
				{
					sv.ScrollToHorizontalOffset(newOffset);
				}
			}
		}
#endif
	}
}
#endif
