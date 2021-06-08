// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class BreadcrumbLayout : NonVirtualizingLayout
	{
		public BreadcrumbLayout(BreadcrumbBar breadcrumb)
		{
			m_breadcrumb = breadcrumb;
		}

		uint GetItemCount(NonVirtualizingLayoutContext context)
		{
			return (uint32_t)(context.Children().Size());
		}

		UIElement GetElementAt(NonVirtualizingLayoutContext context, uint index)
		{
			return context.Children.GetAt(index);
		}

		// Measuring is performed in a single step, every element is measured, including the ellipsis
		// item, but the total amount of space needed is only composed of the non-ellipsis breadcrumbs
		protected internal override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
		{
			m_availableSize = availableSize;

			Size accumulatedCrumbsSize(0, 0);

			for (uint i = 0; i < GetItemCount(context); ++i)
			{
				var breadcrumbItem = GetElementAt(context, i).as< BreadcrumbBarItem > ();
				breadcrumbItem.Measure(availableSize);

				if (i != 0)
				{
					accumulatedCrumbsSize.Width += breadcrumbItem.DesiredSize().Width;
					accumulatedCrumbsSize.Height = std.max(accumulatedCrumbsSize.Height, breadcrumbItem.DesiredSize().Height);
				}
			}

			// Save a reference to the ellipsis button to avoid querying for it multiple times
			if (GetItemCount(context) > 0)
			{
				if (var ellipsisButton = GetElementAt(context, 0) as BreadcrumbBarItem())
        {
					m_ellipsisButton = ellipsisButton;
				}
			}

			if (accumulatedCrumbsSize.Width > availableSize.Width)
			{
				m_ellipsisIsRendered = true;
			}
			else
			{
				m_ellipsisIsRendered = false;
			}

			return accumulatedCrumbsSize;
		}

		void ArrangeItem(UIElement& breadcrumbItem, float& accumulatedWidths, float maxElementHeight)
		{
			Size elementSize = breadcrumbItem.DesiredSize();
			Rect arrangeRect(accumulatedWidths, 0, elementSize.Width, maxElementHeight);
			breadcrumbItem.Arrange(arrangeRect);

			accumulatedWidths += elementSize.Width;
		}

		void ArrangeItem(NonVirtualizingLayoutContext& context, int index, float& accumulatedWidths, float maxElementHeight)
		{
			var element = GetElementAt(context, index);
			ArrangeItem(element, accumulatedWidths, maxElementHeight);
		}

		void HideItem(UIElement& breadcrumbItem)
		{
			Rect arrangeRect(0, 0, 0, 0);
			breadcrumbItem.Arrange(arrangeRect);
		}

		void HideItem(NonVirtualizingLayoutContext& context, int index)
		{
			var element = GetElementAt(context, index);
			HideItem(element);
		}

		int GetFirstBreadcrumbBarItemToArrange(NonVirtualizingLayoutContext & context)
		{
			int itemCount = GetItemCount(context);
			float accumLength = GetElementAt(context, itemCount - 1).DesiredSize().Width +
				m_ellipsisButton.DesiredSize().Width;

			for (int i = itemCount - 2; i >= 0; --i)
			{
				float newAccumLength = accumLength + GetElementAt(context, i).DesiredSize().Width;
				if (newAccumLength > m_availableSize.Width)
				{
					return i + 1;
				}
				accumLength = newAccumLength;
			}

			return 0;
		}

		float GetBreadcrumbBarItemsHeight(NonVirtualizingLayoutContext & context, int firstItemToRender)
		{
			float maxElementHeight{ };

			if (m_ellipsisIsRendered)
			{
				maxElementHeight = m_ellipsisButton.DesiredSize().Height;
			}

			for (uint i = firstItemToRender; i < GetItemCount(context); ++i)
			{
				maxElementHeight = std.max(maxElementHeight, GetElementAt(context, i).DesiredSize().Height);
			}

			return maxElementHeight;
		}

		// Arranging is performed in a single step, as many elements are tried to be drawn going from the last element
		// towards the first one, if there's not enough space, then the ellipsis button is drawn
		protected internal override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
		{
			int itemCount = GetItemCount(context);
			int firstElementToRender{ };
			m_firstRenderedItemIndexAfterEllipsis = itemCount - 1;
			m_visibleItemsCount = 0;

			// If the ellipsis must be drawn, then we find the index (x) of the first element to be rendered, any element with
			// a lower index than x will be hidden (except for the ellipsis button) and every element after x (including x) will
			// be drawn. At the very least, the ellipis and the last item will be rendered
			if (m_ellipsisIsRendered)
			{
				firstElementToRender = GetFirstBreadcrumbBarItemToArrange(context);
				m_firstRenderedItemIndexAfterEllipsis = firstElementToRender;
			}

			float accumulatedWidths{ };
			float maxElementHeight = GetBreadcrumbBarItemsHeight(context, firstElementToRender);

			// If there is at least one element, we may render the ellipsis item
			if (itemCount > 0)
			{
				var ellipsisButton = m_ellipsisButton;

				if (m_ellipsisIsRendered)
				{
					ArrangeItem(ellipsisButton, accumulatedWidths, maxElementHeight);
				}
				else
				{
					HideItem(ellipsisButton);
				}
			}

			// For each item, if the item has an equal or larger index to the first element to render, then
			// render it, otherwise, hide it and add it to the list of hidden items
			for (int i = 1; i < itemCount; ++i)
			{
				if (i < firstElementToRender)
				{
					HideItem(context, i);
				}
				else
				{
					ArrangeItem(context, i, accumulatedWidths, maxElementHeight);
					++m_visibleItemsCount;
				}
			}

			if (m_breadcrumb is BreadcrumbBar breadcrumb)
	{
				breadcrumb.ReIndexVisibleElementsForAccessibility();
			}

			return finalSize;
		}

		internal bool EllipsisIsRendered() => m_ellipsisIsRendered;

		internal uint FirstRenderedItemIndexAfterEllipsis() => m_firstRenderedItemIndexAfterEllipsis;

		internal uint GetVisibleItemsCount() => m_visibleItemsCount;
	}
}
