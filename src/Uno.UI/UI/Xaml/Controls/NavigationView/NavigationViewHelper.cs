// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewHelper.cpp file from WinUI controls.
//

#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal enum NavigationViewVisualStateDisplayMode
	{
		Compact,
		Expanded,
		Minimal,
		MinimalWithBackButton
	}

	internal enum NavigationViewListPosition
	{
		LeftNav,
		TopPrimary,
		TopOverflow
	}

	internal enum NavigationViewPropagateTarget
	{
		LeftListView,
		TopListView,
		OverflowListView,
		All
	}

	public class NavigationViewItemHelper
	{
		public readonly static string c_OnLeftNavigationReveal = "OnLeftNavigationReveal";
		public readonly static string c_OnLeftNavigation = "OnLeftNavigation";
		public readonly static string c_OnTopNavigationPrimary = "OnTopNavigationPrimary";
		public readonly static string c_OnTopNavigationPrimaryReveal = "OnTopNavigationPrimaryReveal";
		public readonly static string c_OnTopNavigationOverflow = "OnTopNavigationOverflow";
	}

	public class NavigationViewItemHelper<T> where T : Control
	{
		static string c_selectionIndicatorName = "SelectionIndicator";

		public NavigationViewItemHelper()
		{
		}

		public UIElement GetSelectionIndicator() { return m_selectionIndicator; }

		public void Init(T item)
		{
			m_selectionIndicator = item.GetTemplateChild(c_selectionIndicatorName) as UIElement;
		}

		UIElement m_selectionIndicator;
	}
}
