// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using NavigationViewPaneDisplayMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
//using MaterialHelperTestApi = Microsoft.UI.Private.Media.MaterialHelperTestApi;
using NavigationView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationView;
using NavigationViewItemInvokedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewSelectionChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using MUXControlsTestApp.Utilities;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{

	//[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewInitPage : TestPage
	{
		ObservableCollection<string> m_menuItems;
		ObservableCollection<string> m_footerItems;
		LinkedList<string> m_menuItemsEnumerable = null;

		public NavigationViewInitPage()
		{
			this.InitializeComponent();

			//MaterialHelperTestApi.IgnoreAreEffectsFast = true;
			//MaterialHelperTestApi.SimulateDisabledByPolicy = false;

			m_menuItems = new ObservableCollection<string>();

			m_menuItems.Add("Menu Item 1");
			m_menuItems.Add("Menu Item 2");
			m_menuItems.Add("Menu Item 3");
			m_menuItems.Add("Music");

			m_footerItems = new ObservableCollection<string>();

			m_footerItems.Add("Footer Item 1");
			m_footerItems.Add("Footer Item 2");
			m_footerItems.Add("Footer Item 3");

			NavView.MenuItemsSource = m_menuItems;
			NavView.FooterMenuItemsSource = m_footerItems;
			NavView.SelectedItem = m_menuItems[0];
		}

		protected
#if HAS_UNO
			internal
#endif
			override void OnNavigatedFrom(NavigationEventArgs e)
		{
			// Unset all override flags to avoid impacting subsequent tests
			//MaterialHelperTestApi.IgnoreAreEffectsFast = false;
			//MaterialHelperTestApi.SimulateDisabledByPolicy = false;
			base.OnNavigatedFrom(e);
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			if (m_menuItemsEnumerable == null)
			{
				m_menuItems.Add("New Menu Item");
			}
			else
			{
				m_menuItemsEnumerable.AddLast("New Menu Item");
			}
		}

		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			if (m_menuItemsEnumerable == null)
			{
				m_menuItems.RemoveAt(m_menuItems.Count - 1);
			}
			else
			{
				m_menuItemsEnumerable.RemoveLast();
			}
		}

		private void AddFooterButton_Click(object sender, RoutedEventArgs e)
		{
			m_footerItems.Add("New Footer Item");
		}

		private void RemoveFooterButton_Click(object sender, RoutedEventArgs e)
		{
			m_footerItems.RemoveAt(m_footerItems.Count - 1);
		}

		private void ChangeToIEnumerableButton_Clicks(object sender, RoutedEventArgs e)
		{
			var newMenuItems = new LinkedList<string>();
			newMenuItems.AddLast("IIterator/Enumerable/LinkedList Item 1");
			newMenuItems.AddLast("IIterator/Enumerable/LinkedList Item 2");
			newMenuItems.AddLast("IIterator/Enumerable/LinkedList Item 3");

			NavView.MenuItemsSource = newMenuItems;
		}

		private void FlipOrientation_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneDisplayMode = NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top ? NavigationViewPaneDisplayMode.Auto : NavigationViewPaneDisplayMode.Top;
		}

		private void SwitchFrame_Click(object sender, RoutedEventArgs e)
		{
			if (Frame2.Content == null)
			{
				var content = Frame1.Content;
				Frame1.Content = null;
				Frame2.Content = content;
			}
			else
			{
				var content = Frame2.Content;
				Frame2.Content = null;
				Frame1.Content = content;
			}
		}

		private void NavView2_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			if (Frame2.Content == null)
			{
				MyLocationResult.Text = "Frame1";
			}
			else
			{
				MyLocationResult.Text = "Frame2";
			}
		}

		private void ChangePaneVisibleButton_Click(object sender, RoutedEventArgs e)
		{
			NavView4.IsPaneVisible = !NavView4.IsPaneVisible;
		}

		private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			// Reset argument type indicatiors
			ItemInvokedItemType.Text = "null";
			ItemInvokedItemContainerType.Text = "null";

			if (args.InvokedItem != null)
			{
				ItemInvokedItemType.Text = args.InvokedItem.GetType().ToString();
			}

			if (args.InvokedItemContainer != null)
			{
				ItemInvokedItemContainerType.Text = args.InvokedItemContainer.GetType().ToString();
			}
		}

		private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			// Reset argument type indicatiors
			SelectionChangedItemType.Text = "null";
			SelectionChangedItemContainerType.Text = "null";

			if (args.SelectedItem != null)
			{
				SelectionChangedItemType.Text = args.SelectedItem.GetType().ToString();
			}

			if (args.SelectedItemContainer != null)
			{
				SelectionChangedItemContainerType.Text = args.SelectedItemContainer.GetType().ToString();
			}
		}
	}
}
