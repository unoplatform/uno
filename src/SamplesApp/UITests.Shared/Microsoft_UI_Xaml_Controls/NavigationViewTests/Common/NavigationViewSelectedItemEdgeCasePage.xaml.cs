// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Automation;

using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewItemSeparator = Microsoft.UI.Xaml.Controls.NavigationViewItemSeparator;
using NavigationViewDisplayModeChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs;
using MUXControlsTestApp.Utilities;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	[Sample("NavigationView", "WinUI")]
    public sealed partial class NavigationViewSelectedItemEdgeCasePage : TestPage
	{
        public NavigationViewSelectedItemEdgeCasePage()
        {
            this.InitializeComponent();

            NavView.SelectedItem = NavView.MenuItems[1];
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {

        }

        private void Button_click(object sender, RoutedEventArgs e)
        {
            var menuItem = new NavigationViewItem();
            menuItem.Content = "New Menu Item Ay";
            menuItem.Icon = new SymbolIcon(Symbol.AllApps);
            NavView.MenuItems.Add(menuItem);
        }
        private void Movies_Click(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = MoviesItem;
        }

        private void Movies_Click2(object sender, RoutedEventArgs e)
        {
            MoviesItem.IsSelected = true;
        }

        private void TV_Click(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = TVItem;
        }

        private void TV_Click2(object sender, RoutedEventArgs e)
        {
            TVItem.IsSelected = true;
        }

        private void CopyIsSelected_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = MoviesItem.IsSelected + " " + TVItem.IsSelected;
        }

    }
}
