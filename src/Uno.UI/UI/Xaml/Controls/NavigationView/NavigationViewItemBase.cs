// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewItemBase.cpp file from WinUI controls.
//

using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationViewItemBase : ListViewItem
	{
		NavigationViewListPosition m_position = NavigationViewListPosition.LeftNav;

		protected virtual void OnNavigationViewListPositionChanged() { }

		internal NavigationViewListPosition Position()
		{
			return m_position;
		}

		internal void Position(NavigationViewListPosition value)
		{
			if (m_position != value)
			{
				m_position = value;
				OnNavigationViewListPositionChanged();
			}
		}

		internal NavigationView GetNavigationView()
		{
			//Because of Overflow popup, we can't get NavigationView by SharedHelpers::GetAncestorOfType
			NavigationView navigationView = null;
			var navigationViewList = GetNavigationViewList();
			if (navigationViewList != null)
			{
				navigationView = navigationViewList.GetNavigationViewParent();
			}
			else
			{
				// Like Settings, it's NavigationViewItem, but it's not in NavigationViewList. Give it a second chance
				navigationView = SharedHelpers.GetAncestorOfType<NavigationView>(VisualTreeHelper.GetParent(this));
			}
			return navigationView;
		}

		internal SplitView GetSplitView()
		{
			SplitView splitView = null;
			var navigationView = GetNavigationView();
			if (navigationView != null)
			{
				splitView = navigationView.GetSplitView();
			}
			return splitView;
		}

		internal NavigationViewList GetNavigationViewList()
		{
			// Find parent NavigationViewList
			return SharedHelpers.GetAncestorOfType<NavigationViewList>(VisualTreeHelper.GetParent(this));
		}
	}
}
