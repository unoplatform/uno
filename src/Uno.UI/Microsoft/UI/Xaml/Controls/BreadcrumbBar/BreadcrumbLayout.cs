// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbLayout.cpp, tag winui3/release/1.5.3, commit 2a60e27

#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Controls;

internal partial class BreadcrumbLayout : NonVirtualizingLayout
{
	public BreadcrumbLayout(BreadcrumbBar breadcrumb)
	{
		m_breadcrumb = WeakReferencePool.RentWeakReference(this, breadcrumb);
	}

	private int GetItemCount(NonVirtualizingLayoutContext context)
	{
		return context.Children.Count;
	}

	private UIElement GetElementAt(NonVirtualizingLayoutContext context, int index)
	{
		return context.Children[index];
	}

	// Measuring is performed in a single step, every element is measured, including the ellipsis
	// item, but the total amount of space needed is only composed of the non-ellipsis breadcrumbs
	protected internal override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
	{
		m_availableSize = availableSize;

		Size accumulatedCrumbsSize = new Size(0, 0);

		for (int i = 0; i < GetItemCount(context); ++i)
		{
			var breadcrumbItem = (BreadcrumbBarItem)GetElementAt(context, i);
			breadcrumbItem.Measure(availableSize);

			if (i != 0)
			{
				accumulatedCrumbsSize.Width += breadcrumbItem.DesiredSize.Width;
				accumulatedCrumbsSize.Height = Math.Max(accumulatedCrumbsSize.Height, breadcrumbItem.DesiredSize.Height);
			}
		}

		// Save a reference to the ellipsis button to avoid querying for it multiple times
		if (GetItemCount(context) > 0)
		{
			if (GetElementAt(context, 0) is BreadcrumbBarItem ellipsisButton)
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

	private void ArrangeItem(UIElement breadcrumbItem, ref float accumulatedWidths, float maxElementHeight)
	{
		Size elementSize = breadcrumbItem.DesiredSize;
		Rect arrangeRect = new Rect(accumulatedWidths, 0, elementSize.Width, maxElementHeight);
		breadcrumbItem.Arrange(arrangeRect);

		accumulatedWidths += (float)elementSize.Width;
	}

	private void ArrangeItem(NonVirtualizingLayoutContext context, int index, ref float accumulatedWidths, float maxElementHeight)
	{
		var element = GetElementAt(context, index);
		ArrangeItem(element, ref accumulatedWidths, maxElementHeight);
	}

	private void HideItem(UIElement breadcrumbItem)
	{
		Rect arrangeRect = new Rect(0, 0, 0, 0);
		breadcrumbItem.Arrange(arrangeRect);
	}

	private void HideItem(NonVirtualizingLayoutContext context, int index)
	{
		var element = GetElementAt(context, index);
		HideItem(element);
	}

	private int GetFirstBreadcrumbBarItemToArrange(NonVirtualizingLayoutContext context)
	{
		int itemCount = GetItemCount(context);
		float accumLength = (float)GetElementAt(context, itemCount - 1).DesiredSize.Width +
			(float)(m_ellipsisButton?.DesiredSize.Width ?? 0.0);

		for (int i = itemCount - 2; i >= 0; --i)
		{
			float newAccumLength = accumLength + (float)GetElementAt(context, i).DesiredSize.Width;
			if (newAccumLength > m_availableSize.Width)
			{
				return i + 1;
			}
			accumLength = newAccumLength;
		}

		return 0;
	}

	private float GetBreadcrumbBarItemsHeight(NonVirtualizingLayoutContext context, int firstItemToRender)
	{
		float maxElementHeight = 0f;

		if (m_ellipsisIsRendered)
		{
			maxElementHeight = (float)(m_ellipsisButton?.DesiredSize.Height ?? 0.0);
		}

		for (int i = firstItemToRender; i < GetItemCount(context); ++i)
		{
			maxElementHeight = (float)Math.Max(maxElementHeight, GetElementAt(context, i).DesiredSize.Height);
		}

		return maxElementHeight;
	}

	// Arranging is performed in a single step, as many elements are tried to be drawn going from the last element
	// towards the first one, if there's not enough space, then the ellipsis button is drawn
	protected internal override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
	{
		int itemCount = GetItemCount(context);
		int firstElementToRender = 0;
		m_firstRenderedItemIndexAfterEllipsis = (uint)(itemCount - 1);
		m_visibleItemsCount = 0;

		// If the ellipsis must be drawn, then we find the index (x) of the first element to be rendered, any element with
		// a lower index than x will be hidden (except for the ellipsis button) and every element after x (including x) will
		// be drawn. At the very least, the ellipis and the last item will be rendered
		if (m_ellipsisIsRendered)
		{
			firstElementToRender = GetFirstBreadcrumbBarItemToArrange(context);
			m_firstRenderedItemIndexAfterEllipsis = (uint)firstElementToRender;
		}

		float accumulatedWidths = 0f;
		float maxElementHeight = GetBreadcrumbBarItemsHeight(context, firstElementToRender);

		// If there is at least one element, we may render the ellipsis item
		if (itemCount > 0)
		{
			var ellipsisButton = m_ellipsisButton;
			if (ellipsisButton is not null)
			{
				if (m_ellipsisIsRendered)
				{
					ArrangeItem(ellipsisButton, ref accumulatedWidths, maxElementHeight);
				}
				else
				{
					HideItem(ellipsisButton);
				}
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
				ArrangeItem(context, i, ref accumulatedWidths, maxElementHeight);
				++m_visibleItemsCount;
			}
		}

		if (m_breadcrumb?.Target is BreadcrumbBar breadcrumb)
		{
			breadcrumb.ReIndexVisibleElementsForAccessibility();
		}

		return finalSize;
	}

	internal bool EllipsisIsRendered() => m_ellipsisIsRendered;

	internal uint FirstRenderedItemIndexAfterEllipsis() => m_firstRenderedItemIndexAfterEllipsis;

	internal uint GetVisibleItemsCount() => m_visibleItemsCount;
}
