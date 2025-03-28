// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// UniformGridLayoutState.cpp, commit a4dcfc96267edb80fe9cc346eb24c0dfac21b40d

#nullable enable

using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
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
			double layoutItemHeight,
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
					realizedElement.Measure(CalculateAvailableSize(availableSize, orientation, stretch, maxItemsPerLine, layoutItemWidth, layoutItemHeight, minRowSpacing, minColumnSpacing));
					SetSize(realizedElement.DesiredSize, layoutItemWidth, layoutItemHeight, availableSize, stretch,
						orientation, minRowSpacing, minColumnSpacing, maxItemsPerLine);
				}
				else
				{
					// Not realized by flowlayout, so do this now!
					var firstElement = context.GetOrCreateElementAt(0, ElementRealizationOptions.ForceCreate);

					if (firstElement is { })
					{
						firstElement.Measure(CalculateAvailableSize(availableSize, orientation, stretch, maxItemsPerLine, layoutItemWidth, layoutItemHeight, minRowSpacing, minColumnSpacing));
						SetSize(firstElement.DesiredSize, layoutItemWidth, layoutItemHeight, availableSize, stretch,
							orientation, minRowSpacing, minColumnSpacing, maxItemsPerLine);
						context.RecycleElement(firstElement);
					}
				}
			}
		}

		private Size CalculateAvailableSize(
			Size availableSize,
			Orientation orientation,
			UniformGridLayoutItemsStretch stretch,
			uint maxItemsPerLine,
			double itemWidth,
			double itemHeight,
			double minRowSpacing,
			double minColumnSpacing)
		{
			// Since some controls might have certain requirements when rendering (e.g. maintaining an aspect ratio),
			// we will let elements know the actual size they will get within our layout and let them measure based on that assumption.
			// That way we ensure that no gaps will be created within our layout because of a control deciding it doesn't need as much height (or width)
			// for the column width (or row height) being provided.
			if (orientation == Orientation.Horizontal)
			{
				if (!itemWidth.IsNaN())
				{
					double allowedColumnWidth = itemWidth;
					if (stretch != UniformGridLayoutItemsStretch.None)
					{
						allowedColumnWidth += CalculateExtraPixelsInLine(maxItemsPerLine, (float)availableSize.Width, itemWidth, minColumnSpacing);
					}

					return new Size((float)allowedColumnWidth, availableSize.Height);
				}
			}
			else
			{
				if (!itemHeight.IsNaN())
				{
					double allowedRowHeight = itemHeight;
					if (stretch != UniformGridLayoutItemsStretch.None)
					{
						allowedRowHeight += CalculateExtraPixelsInLine(maxItemsPerLine, (float)availableSize.Height, itemHeight, minRowSpacing);
					}

					return new Size(availableSize.Width, (float)itemHeight);
				}
			}

			return availableSize;
		}

		double CalculateExtraPixelsInLine(
			uint maxItemsPerLine,
			float availableSizeMinor,
			double itemSizeMinor,
			double minorItemSpacing)
		{
			uint numItemsPerColumn;
			uint numItemsBasedOnSize = (uint)(Math.Max(1.0, availableSizeMinor / (itemSizeMinor + minorItemSpacing)));
			if (numItemsBasedOnSize == 0)
			{
				numItemsPerColumn = maxItemsPerLine;
			}
			else
			{
				numItemsPerColumn = Math.Min(
					maxItemsPerLine,
					numItemsBasedOnSize);
			}

			var usedSpace = (numItemsPerColumn * (itemSizeMinor + minorItemSpacing)) - minorItemSpacing;
			var remainingSpace = ((int)(availableSizeMinor - usedSpace));
			return remainingSpace / ((int)numItemsPerColumn);
		}

		void SetSize(
			Size desiredItemSize,
			double layoutItemWidth,
			double layoutItemHeight,
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
			m_effectiveItemHeight = (layoutItemHeight.IsNaN() ? desiredItemSize.Height : layoutItemHeight);

			var availableSizeMinor =
				orientation == Orientation.Horizontal ? availableSize.Width : availableSize.Height;
			var minorItemSpacing = orientation == Orientation.Vertical ? minRowSpacing : minColumnSpacing;

			var itemSizeMinor =
				orientation == Orientation.Horizontal ? m_effectiveItemWidth : m_effectiveItemHeight;

			double extraMinorPixelsForEachItem = 0.0;
			if (availableSizeMinor.IsFinite())
			{
				extraMinorPixelsForEachItem = CalculateExtraPixelsInLine(maxItemsPerLine, (float)availableSizeMinor, itemSizeMinor, minorItemSpacing);
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
