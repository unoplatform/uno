#if __IOS__ || __ANDROID__
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class FlipView : Selector
	{
		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnItemsSourceChanged(e);

			if (HasItems)
			{
				this.SelectedIndex = 0;
			}
		}

		protected override void OnItemsChanged(object e)
		{
			base.OnItemsChanged(e);

			if (HasItems && SelectedIndex < 0)
			{
				SelectedIndex = 0;
			}
		}

		internal override void OnSelectedIndexChanged(int oldValue, int newValue)
		{
			base.OnSelectedIndexChanged(oldValue, newValue);

			// Never animate for changes greater than next/previous item
			var smallChange = Math.Abs(newValue - oldValue) <= 1;
			OnSelectedIndexChangedPartial(oldValue, newValue, smallChange && UseTouchAnimationsForAllNavigation);
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new FlipViewItem() { IsGeneratedContainer = true };
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is FlipViewItem;
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			DependencyObject pElement = (DependencyObject)(element);
			FlipViewItem pFlipViewItem = null;
			double value = 0.0;
			Thickness flipViewItemMargin = Thickness.Empty;

			// Cast container to known type
			pFlipViewItem = (FlipViewItem)(pElement);

			flipViewItemMargin = pFlipViewItem.Margin;

			value = GetDesiredItemWidth();

			value -= (flipViewItemMargin.Left + flipViewItemMargin.Right);
			pFlipViewItem.Width = value;

			value = GetDesiredItemHeight();

			value -= (flipViewItemMargin.Top + flipViewItemMargin.Bottom);
			pFlipViewItem.Height = value;
		}

		double GetDesiredItemWidth()
		{
			double width = 0.0;

			var spPanel = CollectionView;

			if (spPanel != null)
			{
				width = LayoutInformation.GetAvailableSize(spPanel).Width;
			}

			double pWidth;
			if (double.IsInfinity(width) || width <= 0)
			{
				// Desired container width matches the width of the ScrollingHost part (or FlipView)
				pWidth = m_tpScrollViewer != null ? m_tpScrollViewer.ActualWidth : ActualWidth;
			}
			else
			{
				pWidth = width;
			}

			// If flipview has never been measured yet - scroll viewer will not have its size set.
			// Use flipview size set by developer in that case.
			if (pWidth <= 0)
			{
				pWidth = Width;
			}

			return pWidth;
		}

		double GetDesiredItemHeight()
		{
			double height = 0.0;

			var spPanel = CollectionView;

			if (spPanel != null)
			{
				height = LayoutInformation.GetAvailableSize(spPanel).Height;
			}

			double pHeight;
			if (double.IsInfinity(height) || height <= 0)
			{
				// Desired container height matches the height of the ScrollingHost part (or FlipView)
				pHeight = m_tpScrollViewer != null ? m_tpScrollViewer.ActualHeight : ActualHeight;
			}
			else
			{
				pHeight = height;
			}

			// If flipview has never been measured yet - scroll viewer will not have its size set.
			// Use flipview size set by developer in that case.
			if (pHeight <= 0)
			{
				pHeight = Height;
			}

			return pHeight;
		}
	}
}
#endif
