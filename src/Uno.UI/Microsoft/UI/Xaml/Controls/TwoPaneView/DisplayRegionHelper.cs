// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DisplayRegionHelper.cpp, tag winui3/release/1.4.2

using Uno.UI;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

internal partial class DisplayRegionHelper
{
	internal static DisplayRegionHelperInfo GetRegionInfo()
	{
		var instance = LifetimeHandler.GetDisplayRegionHelperInstance();

		var info = new DisplayRegionHelperInfo();
		info.Mode = TwoPaneViewMode.SinglePane;

		if (instance.m_simulateDisplayRegions)
		{
			// Create fake rectangles for test app
			if (instance.m_simulateMode == TwoPaneViewMode.Wide)
			{
				info.Regions[0] = m_simulateWide0;
				info.Regions[1] = m_simulateWide1;
				info.Mode = TwoPaneViewMode.Wide;
			}
			else if (instance.m_simulateMode == TwoPaneViewMode.Tall)
			{
				info.Regions[0] = m_simulateTall0;
				info.Regions[1] = m_simulateTall1;
				info.Mode = TwoPaneViewMode.Tall;
			}
			else
			{
				info.Regions[0] = m_simulateWide0;
			}
		}
		else
		{
			// ApplicationView.GetForCurrentView throws on failure; in that case we just won't do anything.
			ApplicationView view = null;
			try
			{
				view = ApplicationView.GetForCurrentView();
			}
			catch { }

			if (view != null && view.ViewMode == ApplicationViewMode.Spanning)
			{
				if (view is IApplicationViewSpanningRects appView)
				{
					var rects = appView.GetSpanningRects();

					if (rects.Count == 2)
					{
						info.Regions = new Rect[rects.Count];
						info.Regions[0] = new Rect(rects[0].Location.PhysicalToLogicalPixels(), rects[0].Size.PhysicalToLogicalPixels());
						info.Regions[1] = new Rect(rects[1].Location.PhysicalToLogicalPixels(), rects[1].Size.PhysicalToLogicalPixels());

						// Determine orientation. If neither of these are true, default to doing nothing.
						if (info.Regions[0].X < info.Regions[1].X && info.Regions[0].Y == info.Regions[1].Y)
						{
							// Double portrait
							info.Mode = TwoPaneViewMode.Wide;
						}
						else if (info.Regions[0].X == info.Regions[1].X && info.Regions[0].Y < info.Regions[1].Y)
						{
							// Double landscape
							info.Mode = TwoPaneViewMode.Tall;
						}
					}
				}
			}
		}

		return info;
	}

	/* static */
	internal static UIElement WindowElement()
	{
		var instance = LifetimeHandler.GetDisplayRegionHelperInstance();

		if (instance.m_simulateDisplayRegions)
		{
			// Instead of returning the actual window, find the SimulatedWindow element
			UIElement window = null;

			if (Window.IReallyUseCurrentWindow.Content is FrameworkElement fe)
			{
				window = SharedHelpers.FindInVisualTreeByName(fe, "SimulatedWindow");
			}

			return window;
		}
		else
		{
			return Window.IReallyUseCurrentWindow.Content;
		}
	}

	/* static */
	internal static Rect WindowRect()
	{
		var instance = LifetimeHandler.GetDisplayRegionHelperInstance();

		if (instance.m_simulateDisplayRegions)
		{
			// Return the bounds of the simulated window
			FrameworkElement window = WindowElement() as FrameworkElement;
			Rect rc = new Rect(
				0, 0,
				(float)window.ActualWidth,
				(float)window.ActualHeight);
			return rc;
		}
		else
		{
			return Window.IReallyUseCurrentWindow.Bounds;
		}
	}

	/* static */
	internal static void SimulateDisplayRegions(bool value)
	{
		var instance = LifetimeHandler.GetDisplayRegionHelperInstance();
		instance.m_simulateDisplayRegions = value;
	}

	/* static */
	internal static bool SimulateDisplayRegions()
	{
		var instance = LifetimeHandler.GetDisplayRegionHelperInstance();
		return instance.m_simulateDisplayRegions;
	}

	/* static */
	internal static void SimulateMode(TwoPaneViewMode value)
	{
		var instance = LifetimeHandler.GetDisplayRegionHelperInstance();
		instance.m_simulateMode = value;
	}

	/* static */
	internal static TwoPaneViewMode SimulateMode()
	{
		var instance = LifetimeHandler.GetDisplayRegionHelperInstance();
		return instance.m_simulateMode;
	}
}
