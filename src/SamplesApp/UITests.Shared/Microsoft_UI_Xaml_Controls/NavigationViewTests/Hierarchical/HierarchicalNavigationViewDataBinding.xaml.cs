// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewItemExpandingEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemExpandingEventArgs;
using NavigationViewItemCollapsedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemCollapsedEventArgs;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;

namespace MUXControlsTestApp
{
    public class Category
    {
        public String Content { get; set; }
        public String Name { get; set; }
        public String Icon { get; set; }
        public ObservableCollection<Category> Children { get; set; }
        public bool SelectsOnInvoked { get; set; }

        public Category(String content, String name, String icon, ObservableCollection<Category> children, bool selectsOnInvoked)
        {
            this.Content = content;
            this.Name = name;
            this.Icon = icon;
            this.Children = children;
            this.SelectsOnInvoked = selectsOnInvoked;
        }
    }

    public sealed partial class HierarchicalNavigationViewDataBinding : Page
    {

        ObservableCollection<Category> categories = new ObservableCollection<Category>();

        public HierarchicalNavigationViewDataBinding()
        {
            this.InitializeComponent();

            var categories3 = new ObservableCollection<Category>();
            categories3.Add(new Category("Menu Item 4", "MI4", "Icon", null, true));
            categories3.Add(new Category("Menu Item 5", "MI5", "Icon", null, true));

            var categories2 = new ObservableCollection<Category>();
            categories2.Add(new Category("Menu Item 2", "MI2", "Icon", categories3, false));
            categories2.Add(new Category("Menu Item 3", "MI3", "Icon", null, true));

            var categories5 = new ObservableCollection<Category>();
            categories5.Add(new Category("Menu Item 8", "MI8", "Icon", null, true));
            categories5.Add(new Category("Menu Item 9", "MI9", "Icon", null, true));

            var categories4 = new ObservableCollection<Category>();
            categories4.Add(new Category("Menu Item 7 (Selectable)", "MI7", "Icon", categories5, true));

            var categories7 = new ObservableCollection<Category>();
            categories7.Add(new Category("Menu Item 13", "MI13", "Icon", null, true));
            categories7.Add(new Category("Menu Item 14", "MI14", "Icon", null, true));

            var categories6 = new ObservableCollection<Category>();
            categories6.Add(new Category("Menu Item 12", "MI12", "Icon", categories7, false));

            var categories9 = new ObservableCollection<Category>();
            categories9.Add(new Category("Menu Item 17", "MI17", "Icon", null, true));
            categories9.Add(new Category("Menu Item 18", "MI18", "Icon", null, true));

            var categories8 = new ObservableCollection<Category>();
            categories8.Add(new Category("Menu Item 16 (Selectable)", "MI16", "Icon", categories9, true));

            categories.Add(new Category("Menu Item 1", "MI1", "Icon", categories2, false));
            categories.Add(new Category("Menu Item 6 (Selectable)", "MI6", "Icon", categories4, true));
            categories.Add(new Category("Menu Item 10", "MI10", "Icon", null, true));
            categories.Add(new Category("Menu Item 11", "MI11", "Icon", categories6, false));
            categories.Add(new Category("Menu Item 15", "MI15", "Icon", categories8, false));

        }

        private void ClickedItem(object sender, NavigationViewItemInvokedEventArgs e)
        {
            var clickedItem = e.InvokedItem;
            var clickedItemContainer = e.InvokedItemContainer;
        }

        private void PrintSelectedItem(object sender, RoutedEventArgs e)
        {
            var selectedItem = navview.SelectedItem;
            if(selectedItem != null)
            {
                var label = ((Category)selectedItem).Content;
                SelectedItemLabel.Text = label;
            }
            else
            {
                SelectedItemLabel.Text = "No Item Selected";
            }
        }

        private void AddMenuItem(object sender, RoutedEventArgs e)
        {
            categories.Add(new Category("Menu Item G", "MIG", "Icon", null, true));
        }

        private void RemoveSecondMenuItem(object sender, RoutedEventArgs e)
        {
            categories.RemoveAt(1);
        }
        private void SelectSecondItem(object sender, RoutedEventArgs e)
        {
            navview.SelectedItem = categories[1];
        }        
        private void SelectItemUsingAPI(object sender, RoutedEventArgs e)
        {
            navview.SelectedItem = categories[0].Children[0].Children[1];
        }
        private void OnItemExpanding(object sender, NavigationViewItemExpandingEventArgs e)
        {
            var expandingItemContainerContent = (string)(e.ExpandingItemContainer.Content);
            TextBlockExpandingItem.Text = expandingItemContainerContent;

            // Verify that returned item corresponds to the returned container
            var item = (Category)e.ExpandingItem;
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
            var item = (Category)e.CollapsedItem;
            var areItemAndContainerTheSame = "false";
            if((string)(item.Content) == collapsedItemContainerContent)
            {
                areItemAndContainerTheSame = "true";
            }
            TextblockCollapsedItemAndContainerMatch.Text = areItemAndContainerTheSame;
        }

        private void PaneDisplayModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tag = Convert.ToString(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
            var mode = (NavigationViewPaneDisplayMode)Enum.Parse(typeof(NavigationViewPaneDisplayMode), tag);
            navview.PaneDisplayMode = mode;
        }
    }
}
