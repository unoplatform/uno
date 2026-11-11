#if UNO_REFERENCE_API
using Uno.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using System.Collections.Specialized;
using Uno.Extensions.Specialized;
using System.Diagnostics;
using Uno.UI;
using Uno.Disposables;
using Microsoft.UI.Xaml.Data;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	partial class Selector
	{
		private protected override bool ShouldItemsControlManageChildren => !(ItemsPanelRoot is IVirtualizingPanel);

		partial void RefreshPartial()
		{
			if (VirtualizingPanel != null)
			{
				VirtualizingPanel.GetLayouter().Refresh();

				InvalidateMeasure();
			}
		}

		// Scrolls a realized container into view, or has the virtualizing panel bring the item in.
		private protected void ScrollIntoViewManaged(object item, ScrollIntoViewAlignment alignment)
		{
			if (ItemsPanelRoot is null)
			{
				// Not ready.
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"ScrollIntoView('{item}') ignored: the items panel isn't created yet.");
				}

				return;
			}

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
			else if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"ScrollIntoView('{item}') ignored: the item has no container.");
			}
		}

		private void ScrollIntoViewFastPath(UIElement element, ScrollIntoViewAlignment alignment)
		{
			if (ScrollViewer is { } sv && sv.Presenter is { } presenter)
			{
				var offsetXY = element.TransformToVisual(presenter).TransformPoint(
#if __SKIA__ // Skia correctly doesn't include the offsets in TransformToVisual
					new Point(presenter.HorizontalOffset, presenter.VerticalOffset)
#else
					Point.Zero
#endif
					);

				var orientation = ItemsPanelRoot?.PhysicalOrientation ?? Orientation.Vertical;

				var (elementOffset, elementLength, presenterOffset, presenterViewportLength) =
					orientation is Orientation.Vertical
						? (offsetXY.Y, element.ActualSize.Y, presenter.VerticalOffset, presenter.ViewportHeight)
						: (offsetXY.X, element.ActualSize.X, presenter.HorizontalOffset, presenter.ViewportWidth);

				if (presenterOffset <= elementOffset && elementOffset + elementLength <= presenterOffset + presenterViewportLength)
				{
					// if the element is within the visible viewport, do nothing.
					return;
				}

				// If we use the above offset directly, the item we want to jump to will be the start of the viewport, i.e. leading.
				// For the default alignment, we move the element to either of the viewport ends (i.e. to the top or the bottom of the
				// viewport. To move to the bottom, we scroll one "viewport page" less. This brings the element's start right after the
				// viewport's length ends we then scroll again by elementLength so that the end of the element is the end of the viewport.
				var newOffset = alignment is ScrollIntoViewAlignment.Default && presenterOffset < elementOffset
					? elementOffset - presenterViewportLength + elementLength
					: elementOffset;

				if (orientation is Orientation.Vertical)
				{
					sv.ScrollToVerticalOffset(newOffset);
				}
				else
				{
					sv.ScrollToHorizontalOffset(newOffset);
				}
			}
		}
	}
}
#endif
