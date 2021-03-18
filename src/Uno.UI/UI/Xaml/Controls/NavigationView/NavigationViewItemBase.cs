// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewItemBase.cpp file from WinUI controls.
//

using System;
using Uno.UI.Helpers.WinUI;
using Uno.UI;
#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
#else
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
#endif

#if XAMARIN_IOS_UNIFIED
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationViewItemBase : ListViewItem
	{
		public NavigationViewItemBase()
		{
			Loaded += NavigationViewItemBase_Loaded;
		}

		private void NavigationViewItemBase_Loaded(object sender, RoutedEventArgs e)
		{
			// Workaround for https://github.com/unoplatform/uno/issues/2477
			// In case this container is hosted in another container (if
			// materialized through a DataTemplate), forward some of the properties
			// to the original container.

			if (GetValue(ItemsControl.ItemsControlForItemContainerProperty) == DependencyProperty.UnsetValue)
			{
				var parentItemsControl = this.FindFirstParent<ItemsControl>();

				if (parentItemsControl != null)
				{
					SetValue(ItemsControl.ItemsControlForItemContainerProperty, new WeakReference<ItemsControl>(parentItemsControl));

					var parentSelector = this.FindFirstParent<SelectorItem>();

					SetBinding(IsSelectedProperty, new Binding { Path = "IsSelected", Source = parentSelector, Mode=BindingMode.TwoWay });
				}
			}
		}

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
