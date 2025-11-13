// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewItemExpandingEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemExpandingEventArgs;
using NavigationViewItemCollapsedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemCollapsedEventArgs;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
using MUXControlsTestApp.Utilities;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	[Sample("NavigationView", "MUX")]
	public sealed partial class HierarchicalNavigationViewMarkup : TestPage
	{

		public HierarchicalNavigationViewMarkup()
		{
			this.InitializeComponent();
		}

		private void ClickedItem(object sender, NavigationViewItemInvokedEventArgs e)
		{
		}

		private void PrintSelectedItem(object sender, RoutedEventArgs e)
		{
			var selectedItem = navview.SelectedItem;
			if (selectedItem != null)
			{
				var label = (String)((NavigationViewItem)selectedItem).Content;
				SelectedItemLabel.Text = label;
			}
			else
			{
				SelectedItemLabel.Text = "No Item Selected";
			}
		}

		private void CollapseSelectedItem(object sender, RoutedEventArgs e)
		{
			var selectedItem = navview.SelectedItem;
			if (selectedItem != null)
			{
				var container = (NavigationViewItem)navview.ContainerFromMenuItem(selectedItem);
				container.IsExpanded = false;
			}
		}

		private void RemoveSecondMenuItem(object sender, RoutedEventArgs e)
		{
			navview.MenuItems.RemoveAt(2);
		}

		private void PrintTopLevelIsChildSelectedItems(object sender, RoutedEventArgs e)
		{
			string itemstring = "";
			itemstring = BuildIsChildSelectedString(navview.MenuItems, itemstring);
			if (itemstring == "")
			{
				itemstring = "None";
			}
			IsChildSelectedLabel.Text = itemstring;
		}

		private string BuildIsChildSelectedString(IList<object> items, string itemstring)
		{
			foreach (NavigationViewItem item in items)
			{
				if (item.IsChildSelected == true)
				{
					itemstring += item.Name + " ";
				}
			}
			return itemstring;
		}

		private void SelectSecondItem(object sender, RoutedEventArgs e)
		{
			IList<Object> menuItems = navview.MenuItems;
			navview.SelectedItem = menuItems[1];

		}

		private void OnItemExpanding(object sender, NavigationViewItemExpandingEventArgs e)
		{
			var expandingItemContainerContent = (string)(e.ExpandingItemContainer.Content);
			TextBlockExpandingItem.Text = expandingItemContainerContent;

			// Verify that returned item corresponds to the returned container
			var item = (NavigationViewItem)e.ExpandingItem;
			var areItemAndContainerTheSame = "false";
			if ((string)(item.Content) == expandingItemContainerContent)
			{
				areItemAndContainerTheSame = "true";
			}
			TextblockExpandingItemAndContainerMatch.Text = areItemAndContainerTheSame;
		}

		private void OnItemCollapsed(object sender, NavigationViewItemCollapsedEventArgs e)
		{
			var collapsedItemContainerContent = (string)(e.CollapsedItemContainer.Content);
			TextBlockCollapsedItem.Text = collapsedItemContainerContent;

			// Verify that returned item corresponds to the returned container
			var item = (NavigationViewItem)e.CollapsedItem;
			var areItemAndContainerTheSame = "false";
			if ((string)(item.Content) == collapsedItemContainerContent)
			{
				areItemAndContainerTheSame = "true";
			}
			TextblockCollapsedItemAndContainerMatch.Text = areItemAndContainerTheSame;
		}

		private void PaneDisplayModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToString(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			var mode = Enum.Parse<NavigationViewPaneDisplayMode>(tag);
			navview.PaneDisplayMode = mode;
		}

		private void GetMenuItem1ChildrenFlyoutCornerRadiusButton_Click(object sender, RoutedEventArgs e)
		{
			var parent = MI2.FindVisualParentByType<FlyoutPresenter>();
			if (parent is FlyoutPresenter flyoutPresenter)
			{
				var contentPresenter = flyoutPresenter.FindVisualChildByName("ContentPresenter") as ContentPresenter;
				MenuItem1ChildrenFlyoutCornerRadiusTextBlock.Text = contentPresenter?.CornerRadius.ToString() ?? "Internal ContentPresenter not found";
			}
		}
	}
}
