using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class PivotPanel : Panel
	{
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
		{
			var scroll = this.FindFirstParent<ScrollViewer>();

			if (scroll == null)
			{
				this.Log().Warn("Failed to find expected parent ScrollViewer of this PivotPanel");
			}
			else
			{
				// Here we are bypassing the infinite width provided by the ScrollViewer of the Pivot's template
				// and instead we are constraining the items to have the same width of the parent pivot so can be panned properly.

				availableSize = new Size(
					Math.Min(availableSize.Width, scroll.ViewportMeasureSize.Width),
					availableSize.Height);
			}

			// Note: Here we should X-stack the items to allow the ScrollViewer to do its job
			//		 however currently the Pivot is only changing the Visibility of the items,
			//		 so we only have to Z-stack items and return the 'availableSize' (which actually disable the 'scroll' ScrollViewer)

			var maxHeight = 0d;
			foreach (UIElement child in Children)
			{
				MeasureElement(child, availableSize);
				if (child.DesiredSize.Height > maxHeight)
				{
					maxHeight = child.DesiredSize.Height;
				}
			}

			return availableSize.Height > maxHeight
				? new Size(availableSize.Width, maxHeight)
				: availableSize;
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var scroll = this.FindFirstParent<ScrollViewer>();

			if (scroll == null)
			{
				this.Log().Warn("Failed to find expected parent ScrollViewer of this PivotPanel");
			}
			else
			{
				// Here we are bypassing the infinite width provided by the ScrollViewer of the Pivot's template
				// and instead we are constraining the items to have the same width of the parent pivot so can be panned properly.

				finalSize = new Size(Math.Min(finalSize.Width, scroll.ViewportArrangeSize.Width), finalSize.Height);
			}

			// Note: Here we should X-stack the items to allow the ScrollViewer to do its job
			//		 however currently the Pivot is only changing the Visibility of the items,
			//		 so we only have to Z-stack items and return the 'finalSize' (which actually disable the 'scroll' ScrollViewer)

			foreach (var child in Children)
			{
				ArrangeElement(child, new Rect(new Point(), finalSize));
			}

			return finalSize;
		}
	}
}
