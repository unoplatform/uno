// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
using System;

using NavigationViewDisplayMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewDisplayMode;
using NavigationView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationView;
using NavigationViewSelectionChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using NavigationViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewBackButtonVisible = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewBackButtonVisible;
using NavigationViewItemSeparator = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItemSeparator;
using NavigationViewDisplayModeChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs;
using Uno.UI.Samples.Controls;
using MUXControlsTestApp.Utilities;

namespace MUXControlsTestApp
{
	/// <summary>
	/// Verify ShouldPreserveNavigationViewRS3Behavior
	/// </summary>
	[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewRS3Page : TestPage
	{
		public NavigationViewRS3Page()
		{
			this.InitializeComponent();

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			ChangeTestFrameVisibility(Visibility.Collapsed);

			CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;
			titleBar.ExtendViewIntoTitleBar = true;

			NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Visible;
			NavView.IsBackEnabled = true;
		}
		private void TestFrameCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			ChangeTestFrameVisibility(Visibility.Visible);
			// Show titlebar to reenable clicking the buttons in the test frame
			CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
		}

		private void TestFrameCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			ChangeTestFrameVisibility(Visibility.Collapsed);
			// Hide titlebar again in case we hid it before
			CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
		}

		private void ChangeTestFrameVisibility(Visibility visibility)
		{
			//TODO: Add to uno
			//var testFrame = Window.Current.Content as TestFrame;
			//testFrame.ChangeBarVisibility(visibility);
		}

		private void GetTopPaddingHeight_Click(object sender, RoutedEventArgs e)
		{
			Grid rootGrid = VisualTreeHelper.GetChild(NavView, 0) as Grid;
			if (rootGrid != null)
			{
				Grid paneContentGrid = rootGrid.FindName("TogglePaneTopPadding") as Grid;
				TestResult.Text = paneContentGrid.Height.ToString();
			}
		}

		private void GetToggleButtonRowHeight_Click(object sender, RoutedEventArgs e)
		{
			Grid rootGrid = VisualTreeHelper.GetChild(NavView, 0) as Grid;
			if (rootGrid != null)
			{
				Grid paneContentGrid = rootGrid.FindName("PaneContentGrid") as Grid;
				TestResult.Text = paneContentGrid.RowDefinitions[1].Height.ToString();
			}
		}

	}
}
