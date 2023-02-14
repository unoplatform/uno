// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Controls
{
	partial class UniformGridLayoutState
	{
		public void InitializeForContext(
			VirtualizingLayoutContext context,
			IFlowLayoutAlgorithmDelegates callbacks)
		{
			m_flowAlgorithm.InitializeForContext(context, callbacks);
			context.LayoutStateCore = this;
		}

		public void UninitializeForContext(VirtualizingLayoutContext context)
		{
			m_flowAlgorithm.UninitializeForContext(context);
		}

		internal void EnsureElementSize(
			Size availableSize,
			VirtualizingLayoutContext context,
			double layoutItemWidth,
			double LayoutItemHeight,
			UniformGridLayoutItemsStretch stretch,
			Orientation orientation,
			double minRowSpacing,
			double minColumnSpacing,
			uint maxItemsPerLine)
		{
			if (maxItemsPerLine == 0)
			{
				maxItemsPerLine = 1;
			}

			if (context.ItemCount > 0)
			{
				// If the first element is realized we don't need to get it from the context
				var realizedElement = m_flowAlgorithm.GetElementIfRealized(0);
				if (realizedElement is { })
				{
					realizedElement.Measure(availableSize);
					SetSize(realizedElement.DesiredSize, layoutItemWidth, LayoutItemHeight, availableSize, stretch,
						orientation, minRowSpacing, minColumnSpacing, maxItemsPerLine);
				}
				else
				{
					// Not realized by flowlayout, so do this now!
					var firstElement = context.GetOrCreateElementAt(0, ElementRealizationOptions.ForceCreate);

					if (firstElement is { })
					{
						firstElement.Measure(availableSize);
						SetSize(firstElement.DesiredSize, layoutItemWidth, LayoutItemHeight, availableSize, stretch,
							orientation, minRowSpacing, minColumnSpacing, maxItemsPerLine);
						context.RecycleElement(firstElement);
					}
				}
			}
		}

		void SetSize(
			Size desiredItemSize,
			double layoutItemWidth,
			double LayoutItemHeight,
			Size availableSize,
			UniformGridLayoutItemsStretch stretch,
			Orientation orientation,
			double minRowSpacing,
			double minColumnSpacing,
			uint maxItemsPerLine)
		{
			if (maxItemsPerLine == 0)
			{
				maxItemsPerLine = 1;
			}

			m_effectiveItemWidth = (layoutItemWidth.IsNaN() ? desiredItemSize.Width : layoutItemWidth);
			m_effectiveItemHeight = (LayoutItemHeight.IsNaN() ? desiredItemSize.Height : LayoutItemHeight);

			var availableSizeMinor =
				orientation == Orientation.Horizontal ? availableSize.Width : availableSize.Height;
			var minorItemSpacing = orientation == Orientation.Vertical ? minRowSpacing : minColumnSpacing;

			var itemSizeMinor =
				orientation == Orientation.Horizontal ? m_effectiveItemWidth : m_effectiveItemHeight;

			double extraMinorPixelsForEachItem = 0.0;
			if (availableSizeMinor.IsFinite())
			{
				var numItemsPerColumn = Math.Min(
					maxItemsPerLine,
					(uint)(Math.Max(1.0, availableSizeMinor / (itemSizeMinor + minorItemSpacing))));
				var usedSpace = (numItemsPerColumn * (itemSizeMinor + minorItemSpacing)) - minorItemSpacing;
				var remainingSpace = ((int)(availableSizeMinor - usedSpace));
				extraMinorPixelsForEachItem = remainingSpace / ((int)numItemsPerColumn);
			}

			if (stretch == UniformGridLayoutItemsStretch.Fill)
			{
				if (orientation == Orientation.Horizontal)
				{
					m_effectiveItemWidth += extraMinorPixelsForEachItem;
				}
				else
				{
					m_effectiveItemHeight += extraMinorPixelsForEachItem;
				}
			}
			else if (stretch == UniformGridLayoutItemsStretch.Uniform)
			{
				var itemSizeMajor =
					orientation == Orientation.Horizontal ? m_effectiveItemHeight : m_effectiveItemWidth;
				var extraMajorPixelsForEachItem = itemSizeMajor * (extraMinorPixelsForEachItem / itemSizeMinor);
				if (orientation == Orientation.Horizontal)
				{
					m_effectiveItemWidth += extraMinorPixelsForEachItem;
					m_effectiveItemHeight += extraMajorPixelsForEachItem;
				}
				else
				{
					m_effectiveItemHeight += extraMinorPixelsForEachItem;
					m_effectiveItemWidth += extraMajorPixelsForEachItem;
				}
			}
		}
	}
}
