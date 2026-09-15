// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.Core;

using NavigationViewBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
using System.Globalization;
using Uno.UI.Samples.Controls;
using MUXControlsTestApp.Utilities;

namespace MUXControlsTestApp
{
	[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewCustomThemeResourcesPage : TestPage
	{
		private Style defaultPaneTitleTextBlockStyle = null;

		public NavigationViewCustomThemeResourcesPage()
		{
			this.InitializeComponent();

			this.Loaded += NavigationViewPage_Loaded;
		}

		private void NavigationViewPage_Loaded(object sender, RoutedEventArgs e)
		{
			SettingsItemVisibilityCheckbox.IsChecked = NavView.IsSettingsVisible;
			PaneToggleButtonVisiblityCheckbox.IsChecked = NavView.IsPaneToggleButtonVisible;
			HeaderVisiblityCheckbox.IsChecked = NavView.AlwaysShowHeader;
		}

		private void OpenPaneButton_Click(object sender, RoutedEventArgs e)
		{
			NavView.IsPaneOpen = true;
		}

		private void ChangeContent_Click(object sender, RoutedEventArgs e)
		{
			if (NavView.Content == null)
			{
				NavView.Content = new TextBox() { Text = "|Test|" };
			}
			else
			{
				NavView.Content = null;
			}
		}

		private void CompactModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToDouble(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			NavView.CompactModeThresholdWidth = tag;
		}

		private void ExpandedModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToDouble(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			NavView.ExpandedModeThresholdWidth = tag;
		}

		private void PaneDisplayModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToString(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			var mode = Enum.Parse<NavigationViewPaneDisplayMode>(tag);
			NavView.PaneDisplayMode = mode;
		}

		private void SettingsItemVisibilityCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsSettingsVisible = true;
		}

		private void SettingsItemVisibilityCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsSettingsVisible = false;
		}

		private void PaneToggleButtonVisiblityCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsPaneToggleButtonVisible = true;
		}

		private void PaneToggleButtonVisiblityCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsPaneToggleButtonVisible = false;
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

		private void HeaderVisiblityCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.AlwaysShowHeader = true;
		}

		private void HeaderVisiblityCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.AlwaysShowHeader = false;
		}

		private void WidthCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToDouble(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			NavView.Width = tag;
		}

		private void HeightCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToString(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			if (tag == "Default")
			{
				MainGridRow.Height = new GridLength(1, GridUnitType.Star);
			}
			else if (tag == "300")
			{
				MainGridRow.Height = new GridLength(300, GridUnitType.Pixel);
			}
		}

		private void IsTitleBarAutoPaddingEnabledCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsTitleBarAutoPaddingEnabled = true;
		}

		private void IsTitleBarAutoPaddingEnabledCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsTitleBarAutoPaddingEnabled = false;
		}

		private void TitleBarCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;
			titleBar.ExtendViewIntoTitleBar = false;
		}

		private void TitleBarCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;
			titleBar.ExtendViewIntoTitleBar = true;
		}

		private void ChangePaneTitleText_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneTitle = (String.IsNullOrEmpty(NavView.PaneTitle) ? "|NavView Test|" : string.Empty);
		}

		private void ChangePaneTitleStyle_Click(object sender, RoutedEventArgs e)
		{
			Grid rootGrid = VisualTreeHelper.GetChild(NavView, 0) as Grid;
			if (rootGrid != null)
			{
				var paneTitleTextBlock = rootGrid.FindName("PaneTitleTextBlock") as TextBlock;
				if (paneTitleTextBlock != null)
				{
					if (defaultPaneTitleTextBlockStyle == null)
					{
						defaultPaneTitleTextBlockStyle = paneTitleTextBlock.Style;
					}

					if (defaultPaneTitleTextBlockStyle == paneTitleTextBlock.Style)
					{
						paneTitleTextBlock.Style = Resources["NavigationViewPaneTitleStyle"] as Style;
					}
					else
					{
						paneTitleTextBlock.Style = defaultPaneTitleTextBlockStyle;
					}
				}
			}
		}

		private void ChangeHeaderButton_Click(object sender, RoutedEventArgs args)
		{
			NavView.Header = NavView.Header == null ? new TextBlock() { Text = "|Bananas|" } : null;
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

		private void FlipOrientation_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneDisplayMode = NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top ? NavigationViewPaneDisplayMode.Auto : NavigationViewPaneDisplayMode.Top;
		}

		private void GetHeaderContentMargin_Click(object sender, RoutedEventArgs e)
		{
			string margin = "|Cannot find HeaderContent|";
			Grid rootGrid = VisualTreeHelper.GetChild(NavView, 0) as Grid;
			if (rootGrid != null)
			{
				var control = rootGrid.FindName("HeaderContent") as FrameworkElement;
				if (control != null)
				{
					margin = control.Margin.ToString();
				}
			}
			HeaderContentMarginResult.Text = margin;
		}

		private void GetNavViewActiveVisualStates_Click(object sender, RoutedEventArgs e)
		{
			var visualstates = Utilities.VisualStateHelper.GetCurrentVisualStateName(NavView);
			NavViewActiveVisualStatesResult.Text = string.Join(",", visualstates);
		}

		private void ChangePaneHeader_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneHeader = NavView.PaneHeader == null ? new TextBlock() { Text = "|Modified pane header|" } : null;
		}
	}
}
