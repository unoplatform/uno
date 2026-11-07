// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbLayout.cpp, branch main, commit e3c555a53

#nullable enable

using System;
using System.Collections.Generic;
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

	// Measure ancestors naturally, then measure the last item in the larger of the
	// inline and overflow slots. Choose the visible suffix from these final sizes.
	protected internal override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
	{
		var children = context.Children;
		int itemCount = children.Count;

		// Preparing a new last item can change the previous last item's template.
		UIElement? lastItem = itemCount > 1 ? children[itemCount - 1] : null;

		// Keep ancestor measure constraints stable across resizes, including repeated zero-width layouts.
		Size naturalSize = new Size(
			availableSize.Width > 0 ? float.PositiveInfinity : 0,
			availableSize.Height);
		int oversizedAncestor = -1;
		float ancestorsWidth = 0f;
		float ellipsisWidth = 0f;

		// The first item is the overflow button; its measured width is the cost of hiding ancestors.
		m_ellipsisButton = itemCount > 0 ? children[0] : null;

		if (m_ellipsisButton is not null)
		{
			m_ellipsisButton.Measure(availableSize);
			ellipsisWidth = (float)m_ellipsisButton.DesiredSize.Width;
		}

		for (int i = 1; i < itemCount - 1; ++i)
		{
			var item = children[i];
			item.Measure(naturalSize);
			var desiredSize = item.DesiredSize;
			ancestorsWidth += (float)Math.Min(desiredSize.Width, availableSize.Width);

			if (desiredSize.Width > availableSize.Width)
			{
				oversizedAncestor = i;
			}
		}

		// Leave the last item measured at the same constraint used to decide overflow.
		// If its desired size does not change, the visibility decision stays valid too.
		float lastItemWidth = (float)Math.Max(0.0, availableSize.Width - Math.Min(ancestorsWidth, ellipsisWidth));
		Size lastItemSize = default;

		if (lastItem is not null)
		{
			lastItem.Measure(new Size(lastItemWidth, availableSize.Height));
			lastItemSize = lastItem.DesiredSize;
		}

		m_ellipsisIsRendered = ancestorsWidth > ellipsisWidth &&
			ancestorsWidth + lastItemSize.Width > availableSize.Width;
		m_firstRenderedItemIndexAfterEllipsis = itemCount > 0 ? (uint)(itemCount - 1) : 0u;
		bool constrainedAncestor = false;

		if (m_ellipsisIsRendered)
		{
			m_firstRenderedItemIndexAfterEllipsis = (uint)GetFirstBreadcrumbBarItemToArrange(children, availableSize, ref constrainedAncestor);
		}
		else if (oversizedAncestor >= 0)
		{
			// An oversized ancestor can still be visible alongside zero-width items.
			children[oversizedAncestor].Measure(availableSize);
			constrainedAncestor = true;
		}

		if (constrainedAncestor)
		{
			ancestorsWidth = 0;

			for (int i = 1; i < itemCount - 1; ++i)
			{
				ancestorsWidth += (float)Math.Min(children[i].DesiredSize.Width, availableSize.Width);
			}

			float updatedWidth = (float)Math.Max(0.0, availableSize.Width - Math.Min(ancestorsWidth, ellipsisWidth));

			if (updatedWidth != lastItemWidth)
			{
				lastItem!.Measure(new Size(updatedWidth, availableSize.Height));
				lastItemSize = lastItem.DesiredSize;
			}

			m_ellipsisIsRendered = ancestorsWidth > ellipsisWidth &&
				ancestorsWidth + lastItemSize.Width > availableSize.Width;

			if (!m_ellipsisIsRendered)
			{
				m_firstRenderedItemIndexAfterEllipsis = (uint)(itemCount - 1);
			}
		}

		// Width constraints can also reduce height (for example, in a Viewbox).
		// Only the final sizes of visible items should reserve vertical space.
		int firstItemToRender = m_ellipsisIsRendered ? (int)m_firstRenderedItemIndexAfterEllipsis : 1;
		return new Size(ancestorsWidth + lastItemSize.Width, GetBreadcrumbBarItemsHeight(children, firstItemToRender));
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

	private int GetFirstBreadcrumbBarItemToArrange(
		IReadOnlyList<UIElement> children,
		Size availableSize,
		ref bool constrainedAncestor)
	{
		int itemCount = children.Count;
		float accumLength = (float)children[itemCount - 1].DesiredSize.Width +
			(float)(m_ellipsisButton?.DesiredSize.Width ?? 0.0);

		for (int i = itemCount - 2; i >= 1; --i)
		{
			var item = children[i];
			float itemWidth = (float)item.DesiredSize.Width;

			if (accumLength == 0 && itemWidth > availableSize.Width)
			{
				item.Measure(availableSize);
				itemWidth = (float)item.DesiredSize.Width;
				constrainedAncestor = true;
			}

			float newAccumLength = accumLength + (float)Math.Min(itemWidth, availableSize.Width);

			if (newAccumLength > availableSize.Width)
			{
				return i + 1;
			}

			accumLength = newAccumLength;
		}

		return 1;
	}

	private float GetBreadcrumbBarItemsHeight(IReadOnlyList<UIElement> children, int firstItemToRender)
	{
		float maxElementHeight = 0f;

		if (m_ellipsisIsRendered)
		{
			maxElementHeight = (float)(m_ellipsisButton?.DesiredSize.Height ?? 0.0);
		}

		int itemCount = children.Count;

		for (int i = firstItemToRender; i < itemCount; ++i)
		{
			maxElementHeight = (float)Math.Max(maxElementHeight, children[i].DesiredSize.Height);
		}

		return maxElementHeight;
	}

	// Arranging is performed in a single step, as many elements are tried to be drawn going from the last element
	// towards the first one, if there's not enough space, then the ellipsis button is drawn
	protected internal override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
	{
		int itemCount = GetItemCount(context);
		int firstElementToRender = 1;
		m_visibleItemsCount = 0;

		// Use the visible suffix selected during measure. At least the ellipsis and
		// the last item are rendered when the leading items overflow.
		if (m_ellipsisIsRendered)
		{
			firstElementToRender = (int)m_firstRenderedItemIndexAfterEllipsis;
		}

		float accumulatedWidths = 0f;
		float maxElementHeight = GetBreadcrumbBarItemsHeight(context.Children, firstElementToRender);

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
