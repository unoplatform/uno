// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeControl_TestUI/SwipeControlClearPage.xaml.cs

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

using SwipeMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeMode;

namespace MUXControlsTestApp
{
	/// <summary>
	/// Test page used for clearing existing SwipeControls
	/// </summary>
	[Sample("SwipeControl")]
	public sealed partial class SwipeControlClearPage : Page //: TestPage
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

			DefaultSwipeItemsHorizontal.Mode = SwipeMode.Reveal;
			DefaultSwipeItemsHorizontal.Add(DefaultSwipeItemHorizontal);

			// Using swipecontrol inside datatemplate prevents us from setting that:
			// Swipecontrol is in horizontal mode, can not add vertical swipe items...
			//DefaultSwipeItemsVertical.Mode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeMode.Reveal;
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
