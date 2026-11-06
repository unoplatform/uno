// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_IItemsLookupAndIPaginatedPanel.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	// Get the last visible item index if not grouping, and get the last visible group index if we are grouping
	internal int GetLastItemIndexInViewport(ScrollViewer scrollViewer)
	{
		int returnValue = -1;

		Rect window = default;
		Orientation orientation = m_spLayoutStrategy.VirtualizationDirection;

		// Let's determine the window of interest
		{
			double horizontalScrollOffset;
			double verticalScrollOffset;
			double viewportWidth;
			double viewportHeight;
			float zoomFactor = 1.0f;
			Rect scrollViewport = default;

			horizontalScrollOffset = scrollViewer.HorizontalOffset;
			verticalScrollOffset = scrollViewer.VerticalOffset;
			viewportWidth = scrollViewer.ViewportWidth;
			viewportHeight = scrollViewer.ViewportHeight;

			zoomFactor = scrollViewer.ZoomFactor;

			scrollViewport.X = (float)horizontalScrollOffset;
			scrollViewport.Y = (float)verticalScrollOffset;
			scrollViewport.Width = (float)viewportWidth;
			scrollViewport.Height = (float)viewportHeight;
			window = CalculateVisibleWindowFromScrollData(scrollViewport, zoomFactor);
		}

		// Let's walk the realized elements back from the end and grab the first one that passes the far edge of the window
		if (!m_cacheManager.IsGrouping())
		{
			int closestValidContainerIndex = -1;
			for (int validContainerIndex = m_containerManager.GetValidContainerCount() - 1; validContainerIndex >= 0 && closestValidContainerIndex < 0; --validContainerIndex)
			{
				UIElement? spContainer = m_containerManager.GetContainerAtValidIndex(validContainerIndex);

				if (!GetElementIsSentinel(spContainer))
				{
					Rect bounds = GetBoundsFromElement(spContainer!);

					switch (orientation)
					{
						case Orientation.Horizontal:
							if (bounds.X < window.X + window.Width)
							{
								closestValidContainerIndex = validContainerIndex;
							}
							break;

						case Orientation.Vertical:
							if (bounds.Y < window.Y + window.Height)
							{
								closestValidContainerIndex = validContainerIndex;
							}
							break;
					}
				}
			}

			if (closestValidContainerIndex >= 0)
			{
				returnValue = m_containerManager.GetItemIndexFromValidIndex(closestValidContainerIndex);
			}
		}
		else
		{
			int closestValidHeaderIndex = -1;
			for (int validHeaderIndex = m_containerManager.GetValidHeaderCount() - 1; validHeaderIndex >= 0 && closestValidHeaderIndex < 0; --validHeaderIndex)
			{
				UIElement? spHeader = m_containerManager.GetHeaderAtValidIndex(validHeaderIndex);

				if (!GetElementIsSentinel(spHeader))
				{
					Rect bounds = GetBoundsFromElement(spHeader!);

					switch (orientation)
					{
						case Orientation.Horizontal:
							if (bounds.X < window.X + window.Width)
							{
								closestValidHeaderIndex = validHeaderIndex;
							}
							break;

						case Orientation.Vertical:
							if (bounds.Y < window.Y + window.Height)
							{
								closestValidHeaderIndex = validHeaderIndex;
							}
							break;
					}
				}
			}

			if (closestValidHeaderIndex >= 0)
			{
				returnValue = m_containerManager.GetGroupIndexFromValidIndex(closestValidHeaderIndex);
			}
		}

		return returnValue;
	}

	internal double GetItemsPerPage(ScrollViewer scrollViewer)
	{
		double returnValue;

		Rect window;

		// Let's determine the window of interest
		{
			double horizontalScrollOffset;
			double verticalScrollOffset;
			double viewportWidth;
			double viewportHeight;
			float zoomFactor = 1.0f;
			Rect scrollViewport = default;

			horizontalScrollOffset = scrollViewer.HorizontalOffset;
			verticalScrollOffset = scrollViewer.VerticalOffset;
			viewportWidth = scrollViewer.ViewportWidth;
			viewportHeight = scrollViewer.ViewportHeight;

			zoomFactor = scrollViewer.ZoomFactor;

			scrollViewport.X = (float)horizontalScrollOffset;
			scrollViewport.Y = (float)verticalScrollOffset;
			scrollViewport.Width = (float)viewportWidth;
			scrollViewport.Height = (float)viewportHeight;
			window = CalculateVisibleWindowFromScrollData(scrollViewport, zoomFactor);
		}

		returnValue = GetItemsPerPageImpl(window);

		return returnValue;
	}

	// Virtual helper method to get the ItemsPerPage that can be overridden by derived classes.
	private protected virtual double GetItemsPerPageImpl(Rect window)
	{
		double itemsPerPage;
		float panelExtent = 0;
		float windowSize = 0;
		Orientation orientation = Orientation.Horizontal;

		itemsPerPage = 0;

		orientation = m_spLayoutStrategy.VirtualizationDirection;

		// We're going to follow (non-virtualizing) StackPanel's lead here
		// Meaning, we'll return an average items per page based on the total number or items
		// and the panel's extent in units of how many pages it spans
		{
			Size desiredSize;
			desiredSize = DesiredSize;

			switch (orientation)
			{
				case Orientation.Horizontal:
					panelExtent = (float)desiredSize.Width;
					windowSize = (float)window.Width;
					break;

				case Orientation.Vertical:
					panelExtent = (float)desiredSize.Height;
					windowSize = (float)window.Height;
					break;
			}
		}

		if (!DoubleUtil.AreClose(panelExtent, 0))
		{
			// This is easy when thinking in terms of units conversion/cancellation:
			// (elements/panel) * (pixels/window) / (pixels/panel) => (elements/panel) * (panels/window) => (elements/window)
			itemsPerPage = m_elementCountAtLastMeasure * (windowSize / panelExtent);
		}

		return itemsPerPage;
	}
}
