// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

using NavigationViewPaneDisplayMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
using NavigationViewBackButtonVisible = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewBackButtonVisible;
using NavigationViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItem;
using MUXControlsTestApp.Utilities;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewCompactPaneLengthTestPage : TestPage
	{
		public NavigationViewCompactPaneLengthTestPage()
		{
			this.InitializeComponent();

			NavView.CompactPaneLength = 96;
		}

		private void CompactPaneLength_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToDouble(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			NavView.CompactPaneLength = tag;
		}

		private void OpenPaneLength_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToDouble(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			NavView.OpenPaneLength = tag;
		}

		private void PaneToggleButtonVisiblityCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsPaneToggleButtonVisible = true;
		}

		private void PaneToggleButtonVisiblityCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsPaneToggleButtonVisible = false;
		}

		private void PaneDisplayModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToString(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			var mode = (NavigationViewPaneDisplayMode)Enum.Parse(typeof(NavigationViewPaneDisplayMode), tag);
			NavView.PaneDisplayMode = mode;
		}

		private void BackButtonVisibilityCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Visible;
		}

		private void BackButtonVisibilityCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
		}

		private void BackButtonEnabledCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsBackEnabled = true;
		}

		private void BackButtonEnabledCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsBackEnabled = false;
		}

		private void CheckMenuItemsOffset_Click(object sender, RoutedEventArgs e)
		{
			bool allCorrect = true;

			foreach (var item in NavView.MenuItems)
			{
				if (item as NavigationViewItem == null)
				{
					continue;
				}
				var transform = GetContentBox(item as NavigationViewItem).TransformToVisual(null) as MatrixTransform;
				if (Math.Abs(transform.Matrix.OffsetX - NavView.CompactPaneLength) > double.Epsilon)
				{
					allCorrect = false;
				}
			}

			var rootgrid = VisualTreeHelper.GetChild(NavView, 0);
			var paneToggleButtonGrid = VisualTreeHelper.GetChild(rootgrid, 0);
			var buttonHolderGrid = VisualTreeHelper.GetChild(paneToggleButtonGrid, 1);
			var backButton = VisualTreeHelper.GetChild(buttonHolderGrid, 0) as Button;
			var togglePaneButton = VisualTreeHelper.GetChild(buttonHolderGrid, 2) as Button;

			if (Math.Abs(backButton.ActualWidth - NavView.CompactPaneLength) > double.Epsilon)
			{
				allCorrect = false;
			}

			if (Math.Abs(togglePaneButton.ActualWidth - NavView.CompactPaneLength) > double.Epsilon)
			{
				allCorrect = false;
			}

			MenuItemsCorrectOffset.IsChecked = allCorrect;

		}

		private void NavView_ItemInvoked(object sender, Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs e)
		{
			if (e.InvokedItem as string == "Home")
			{
				int invokeCount = int.Parse(HomeItemInvokedCount.Text);
				invokeCount++;
				HomeItemInvokedCount.Text = invokeCount.ToString();
			}
		}


		/* Helper functions */
		private UIElement GetContentBox(NavigationViewItem element)
		{
			if (element == null)
			{
				return null;
			}
			// Path we are using here: NVIGrid->NavigationViewItemPresenter->LayoutRoot
			// ->PresenterContentRootGrid->ContentGrid->ContentPresenter
			var elementGrid = VisualTreeHelper.GetChild(element, 0);
			var presenter = VisualTreeHelper.GetChild(elementGrid, 0);
			var layoutRoot = VisualTreeHelper.GetChild(presenter, 0);
			var presenterContentRootGrid = VisualTreeHelper.GetChild(layoutRoot, 1);
			var contentGrid = VisualTreeHelper.GetChild(presenterContentRootGrid, 1);
			var contentPresenter = VisualTreeHelper.GetChild(contentGrid, 1);
			return contentPresenter as UIElement;

		}
	}
}
