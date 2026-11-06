// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsPresenter_IManipulationDataProvider.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsPresenter
{
	// IManipulationDataProvider methods accessed by owning ScrollViewer to support DirectManipulation.

	internal Orientation PhysicalOrientation
	{
		get
		{
			var value = Orientation.Horizontal;
			var spPanel = Panel;

#if !HAS_UNO // TODO Uno: no panel implements IManipulationDataProvider
			if (spPanel is IManipulationDataProvider spIManipulationDataProvider)
			{
				value = spIManipulationDataProvider.PhysicalOrientation;
			}
			else
#endif
			if (spPanel is StackPanel spStackPanel)
			{
				value = spStackPanel.Orientation;
			}
			else if (spPanel is VariableSizedWrapGrid spVSWG)
			{
				value = ((IOrientedPanel)spVSWG).PhysicalOrientation;
			}
			// unknown why we're not looking for IOrientedPanel. I do not want to change
			// behavior across the board, so I'm doing a lookup for our new style panel instead of the
			// interface
			else if (spPanel is ModernCollectionBasePanel spNewStyleVirtualizingPanel)
			{
				if (spNewStyleVirtualizingPanel is IOrientedPanel spOrientedPanel)
				{
					value = spOrientedPanel.PhysicalOrientation;
				}
			}
			// Uno: other panels report their orientation through Panel.PhysicalOrientation.
			else if (spPanel is Panel spOtherPanel && spOtherPanel.PhysicalOrientation is { } physicalOrientation)
			{
				value = physicalOrientation;
			}

			return value;
		}
	}

#if !HAS_UNO // TODO Uno: IManipulationDataProvider has no Uno consumer yet
	// Called when a manipulation is starting or ending.
	public void UpdateInManipulation(bool isInManipulation, bool isInLiveTree, double nonVirtualizingOffset)
	{
		if (isInLiveTree)
		{
			InvalidateMeasure();
		}

		if (Panel is IManipulationDataProvider spIManipulationDataProvider)
		{
			spIManipulationDataProvider.UpdateInManipulation(isInManipulation,
				isInLiveTree,
				nonVirtualizingOffset);
		}
	}

	// Updates the zoom factor
	public void SetZoomFactor(float newZoomFactor)
	{
		if (Panel is IManipulationDataProvider spIManipulationDataProvider)
		{
			spIManipulationDataProvider.SetZoomFactor(newZoomFactor);
		}

		m_fZoomFactor = newZoomFactor;

		var bIsInDMZoom = IsInDirectManipulationZoom();
		if (!bIsInDMZoom)
		{
			InvalidateMeasure();
		}
	}

	// Gets the scrolling extent in pixels even for logical scrolling scenarios.
	public void ComputePixelExtent(bool ignoreZoomFactor, ref double extent)
	{
		float zoomFactor = 1;

		if (!ignoreZoomFactor)
		{
			zoomFactor = GetZoomFactor();
			global::System.Diagnostics.Debug.Assert(zoomFactor == m_fZoomFactor);
		}

		if (Panel is IManipulationDataProvider spIManipulationDataProvider)
		{
			spIManipulationDataProvider.ComputePixelExtent(ignoreZoomFactor, ref extent);
		}

		var headerSize = GetHeaderSize();
		var padding = PaddingInternal;
		var isHorizontal = IsHorizontal();

		if (isHorizontal)
		{
			extent += (headerSize.Width + padding.Left + padding.Right) * zoomFactor;
		}
		else
		{
			extent += (headerSize.Height + padding.Top + padding.Bottom) * zoomFactor;
		}
	}

	// Gets the offset in pixels even for logical scrolling scenarios.
	public void ComputePixelOffset(bool isForHorizontalOrientation, out double offset)
	{
		Size sizeOfFirstVisibleChild = default;
		double currentOffset = 0;

		offset = 0;

		var zoomFactor = GetZoomFactor();

		global::System.Diagnostics.Debug.Assert(zoomFactor == m_fZoomFactor);

		if (Panel is IManipulationDataProvider spIManipulationDataProvider)
		{
			spIManipulationDataProvider.ComputePixelOffset(isForHorizontalOrientation, out offset);
		}

		var isHorizontal = IsHorizontal();
		if (isHorizontal)
		{
			currentOffset = HorizontalOffset;
		}
		else
		{
			currentOffset = VerticalOffset;
		}

		var innerPanelOffset = GetInnerPanelOffset(isHorizontal);

		// Account for header/leading padding.
		double headerAndLeadingPaddingSize = 0;
		if (DoubleUtil.LessThan(currentOffset, 2))
		{
			headerAndLeadingPaddingSize = GetHeaderAndLeadingPaddingPixelSize(isHorizontal, currentOffset);
		}
		else
		{
			headerAndLeadingPaddingSize = GetHeaderAndLeadingPaddingPixelSize(isHorizontal);
		}

		offset += headerAndLeadingPaddingSize * zoomFactor;

		// Account for trailing padding
		double offsetForTrailingPadding = 0;
		double firstVisibleItemSize = 0;
		if (DoubleUtil.GreaterThan(currentOffset - innerPanelOffset, 2))
		{
			sizeOfFirstVisibleChild = GetSizeOfContainer();
			firstVisibleItemSize = isHorizontal ? sizeOfFirstVisibleChild.Width : sizeOfFirstVisibleChild.Height;
			offsetForTrailingPadding = DoubleUtil.Min(1, currentOffset - innerPanelOffset - 2) * firstVisibleItemSize;
		}

		offset += offsetForTrailingPadding * zoomFactor;
	}

	// Gets the logical offset given a pixel delta.
	public void ComputeLogicalOffset(bool isForHorizontalOrientation, ref double pixelDelta, out double logicalOffset)
	{
		double currentOffset = 0;
		double logicalOffsetInIP = 2;
		double logicalOffsetInPanel = 0;

		logicalOffset = 0;

		if (isForHorizontalOrientation)
		{
			currentOffset = m_ScrollData.OffsetX;
		}
		else
		{
			currentOffset = m_ScrollData.OffsetY;
		}

		pixelDelta = DoubleUtil.Round(pixelDelta, 0);
		logicalOffset = currentOffset;

		// we done, no more pixels to translate
		if (pixelDelta == 0)
		{
			return;
		}

		// Handle pixel delta for padding and header
		if (currentOffset < 2 || (currentOffset == 2 && pixelDelta < 0))
		{
			TranslatePixelDeltaToOffset(currentOffset, isForHorizontalOrientation, ref pixelDelta, out logicalOffsetInIP);
			pixelDelta = DoubleUtil.Round(pixelDelta, 0);
		}
		logicalOffset = logicalOffsetInIP;

		// we done, no more pixels to translate
		if (pixelDelta == 0 || logicalOffsetInIP == 0)
		{
			return;
		}

		if (Panel is IManipulationDataProvider spIManipulationDataProvider)
		{
			spIManipulationDataProvider.ComputeLogicalOffset(isForHorizontalOrientation,
				ref pixelDelta,
				out logicalOffsetInPanel);
			pixelDelta = DoubleUtil.Round(pixelDelta, 0);
		}
		logicalOffset += logicalOffsetInPanel;

		// we done, no more pixels to translate
		if (pixelDelta >= 0 || logicalOffset > 2)
		{
			return;
		}

		TranslatePixelDeltaToOffset(2, isForHorizontalOrientation, ref pixelDelta, out logicalOffset);
		pixelDelta = DoubleUtil.Round(pixelDelta, 0);
	}

	public Size GetSizeOfFirstVisibleChild()
	{
		Size size = default;
		double currentOffset = 0;

		var isHorizontal = IsHorizontal();

		if (isHorizontal)
		{
			currentOffset = HorizontalOffset;
		}
		else
		{
			currentOffset = VerticalOffset;
		}

		if (DoubleUtil.GreaterThanOrClose(currentOffset, 2))
		{
			size = GetSizeOfContainer();
		}
		else
		{
			double dimension = 0;
			dimension = GetSentinelItemSize((int)currentOffset, true);
			size.Width = (float)dimension;
			dimension = GetSentinelItemSize((int)currentOffset, false);
			size.Height = (float)dimension;
		}

		return size;
	}
#endif

	internal Size GetSizeOfContainer()
	{
		Size size = default;
		var spPanel = Panel;

#if !HAS_UNO // TODO Uno: no panel implements IManipulationDataProvider
		if (spPanel is IManipulationDataProvider spIManipulationDataProvider)
		{
			size = spIManipulationDataProvider.GetSizeOfFirstVisibleChild();
		}
		else
#endif
		if (spPanel is Panel spPanelAsPanel)
		{
			var spChildren = spPanelAsPanel.Children;
			if (spChildren.Count > 0)
			{
				var spFirstChild = spChildren[0];
				size = spFirstChild.DesiredSize;
			}
		}

		return size;
	}

	// End of IManipulationDataProvider methods accessed by owning ScrollViewer to support DirectManipulation.

#if !HAS_UNO // TODO Uno: only used by the IScrollInfo and IManipulationDataProvider paths
	private void TranslatePixelDeltaToOffset(double currentOffset, bool isHorizontal, ref double delta, out double value)
	{
		var currentIndex = (int)currentOffset;
		double currentItemSize = 0;
		double itemLogicalOffset = DoubleUtil.Fractional(currentOffset);
		value = DoubleUtil.Floor(currentOffset);
		var zoomFactor = GetZoomFactor();
		global::System.Diagnostics.Debug.Assert(zoomFactor == m_fZoomFactor);

		if (delta < 0)
		{
			while (delta < 0 && currentIndex >= 0)
			{
				currentItemSize = GetSentinelItemSize(currentIndex, isHorizontal);
				double itemOffsetLeftOrBottom = 0.0;

				currentItemSize *= zoomFactor;
				itemOffsetLeftOrBottom = itemLogicalOffset * currentItemSize;

				itemOffsetLeftOrBottom += delta;
				if (itemOffsetLeftOrBottom >= 0)
				{
					value += itemOffsetLeftOrBottom / currentItemSize;
				}
				else
				{
					itemLogicalOffset = 1;
					value--;
					currentIndex--;
				}

				delta = DoubleUtil.Min(0, itemOffsetLeftOrBottom);
			}

			if (delta < 0)
			{
				delta = 0;
			}
		}
		else
		{
			while (delta > 0 && currentIndex <= 1)
			{
				currentItemSize = GetSentinelItemSize(currentIndex, isHorizontal);
				double itemOffsetRightOrTop = 0.0;

				currentItemSize *= zoomFactor;
				itemOffsetRightOrTop = (1 - itemLogicalOffset) * currentItemSize;

				itemOffsetRightOrTop -= delta;
				if (itemOffsetRightOrTop >= 0)
				{
					value += (currentItemSize - itemOffsetRightOrTop) / currentItemSize;
				}
				else
				{
					itemLogicalOffset = 0;
					value++;
					currentIndex++;
				}

				delta = DoubleUtil.Max(0, -itemOffsetRightOrTop);
			}
		}

		value = DoubleUtil.Max(isHorizontal ? m_ScrollData.m_MinOffset.X : m_ScrollData.m_MinOffset.Y, value);
	}

	private void TranslateVerticalPixelDeltaToOffset(double currentOffset, ref double delta, out double value)
	{
		var isHorizontal = IsHorizontal();
		if (!isHorizontal)
		{
			ComputeLogicalOffset(isHorizontal, ref delta, out value);
		}
		else
		{
			value = currentOffset + delta;
		}
	}

	private void TranslateHorizontalPixelDeltaToOffset(double currentOffset, ref double delta, out double value)
	{
		var isHorizontal = IsHorizontal();
		if (isHorizontal)
		{
			ComputeLogicalOffset(isHorizontal, ref delta, out value);
		}
		else
		{
			value = currentOffset + delta;
		}
	}
#endif
}
