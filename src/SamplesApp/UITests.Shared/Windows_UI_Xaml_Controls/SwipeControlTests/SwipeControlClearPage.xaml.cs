// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml;

namespace MUXControlsTestApp
{
    /// <summary>
    /// Test page used for clearing existing SwipeControls
    /// </summary>
    public sealed partial class SwipeControlClearPage : TestPage
    {
        private string[] items = new string[] { "some text" };

        public SwipeControlClearPage()
        {
            this.InitializeComponent();
            SwipeItemsChildSum.Text = (DefaultSwipeItemsHorizontal.Count + DefaultSwipeItemsVertical.Count).ToString();

            leftSwipe.ItemsSource = items;
            topSwipe.ItemsSource = items;
            rightSwipe.ItemsSource = items;
            bottomSwipe.ItemsSource = items;
        }

        public void AddSwipeItemsButton_Click(object sender, RoutedEventArgs e)
        {
            DefaultSwipeItemsHorizontal.Clear();
            DefaultSwipeItemsVertical.Clear();

            DefaultSwipeItemsHorizontal.Mode = Microsoft.UI.Xaml.Controls.SwipeMode.Reveal;
            DefaultSwipeItemsHorizontal.Add(DefaultSwipeItemHorizontal);

            // Using swipecontrol inside datatemplate prevents us from setting that:
            // Swipecontrol is in horizontal mode, can not add vertical swipe items...
            //DefaultSwipeItemsVertical.Mode = Microsoft.UI.Xaml.Controls.SwipeMode.Reveal;
            //DefaultSwipeItemsVertical.Add(DefaultSwipeItemVertical);

            SwipeItemsChildSum.Text = (DefaultSwipeItemsHorizontal.Count + DefaultSwipeItemsVertical.Count).ToString();
        }
        public void ClearSwipeItemsButton_Click(object sender, RoutedEventArgs e)
        {
            DefaultSwipeItemsHorizontal.Clear();
            DefaultSwipeItemsVertical.Clear();
            SwipeItemsChildSum.Text = (DefaultSwipeItemsHorizontal.Count + DefaultSwipeItemsVertical.Count).ToString();
        }

    }
}
