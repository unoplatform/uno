// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using Uno.UI.Samples.Controls;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using MUXControlsTestApp;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.RadioButtonsTests
{
	[Sample("Buttons", "MUX")]
	public sealed partial class RadioButtonsFocusPage : Page
	{
		public RadioButtonsFocusPage()
		{
			this.InitializeComponent();

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			ChangeTestFrameVisibility(Visibility.Collapsed);
		}

		private void ChangeTestFrameVisibility(Visibility visibility)
		{
			//TODO Uno: Add support for TestFrame in MUX test pages
			//var testFrame = Window.Current.Content as TestFrame;
			//testFrame.ChangeBarVisibility(visibility);
		}

		private void TestRadioButtons_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
		{
			var index = TestRadioButtons1.SelectedIndex;
			SelectedIndexTextBlock1.Text = index.ToString();
			if (TestRadioButtons1.SelectedItem != null)
			{
				SelectedItemTextBlock1.Text = TestRadioButtons1.SelectedItem.ToString();
			}
			else
			{
				SelectedItemTextBlock1.Text = "null";
			}

		}

		private void TestRadioButtons2_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
		{
			var index = TestRadioButtons2.SelectedIndex;
			SelectedIndexTextBlock2.Text = index.ToString();
			if (TestRadioButtons2.SelectedItem != null)
			{
				SelectedItemTextBlock2.Text = TestRadioButtons2.SelectedItem.ToString();
			}
			else
			{
				SelectedItemTextBlock2.Text = "null";
			}

		}

		private void TestRadioButtons_GotFocus(object sender, RoutedEventArgs e)
		{
			var stackPanel = VisualTreeHelper.GetChild(TestRadioButtons1, 0);
			var repeater = (ItemsRepeater)VisualTreeHelper.GetChild(stackPanel, 1);
			FocusedIndexTextBlock1.Text = repeater.GetElementIndex((UIElement)e.OriginalSource).ToString();
			TestRadioButtons1HasFocusCheckBox.IsChecked = true;
		}

		private void TestRadioButtons_LostFocus(object sender, RoutedEventArgs e)
		{
			FocusedIndexTextBlock1.Text = "-1";
			TestRadioButtons1HasFocusCheckBox.IsChecked = false;
		}

		private void TestRadioButtons2_GotFocus(object sender, RoutedEventArgs e)
		{
			var stackPanel = VisualTreeHelper.GetChild(TestRadioButtons2, 0);
			var repeater = (ItemsRepeater)VisualTreeHelper.GetChild(stackPanel, 1);
			FocusedIndexTextBlock2.Text = repeater.GetElementIndex((UIElement)e.OriginalSource).ToString();
			TestRadioButtons2HasFocusCheckBox.IsChecked = true;
		}

		private void TestRadioButtons2_LostFocus(object sender, RoutedEventArgs e)
		{
			FocusedIndexTextBlock2.Text = "-1";
			TestRadioButtons2HasFocusCheckBox.IsChecked = false;
		}

		private void SelectByIndexButton1_Click(object sender, RoutedEventArgs e)
		{
			var index = getIndexToSelect(false);
			if (index != -2)
			{
				TestRadioButtons1.SelectedIndex = index;
			}
		}

		private void SelectByIndexButton2_Click(object sender, RoutedEventArgs e)
		{
			var index = getIndexToSelect(true);
			if (index != -2)
			{
				TestRadioButtons2.SelectedIndex = index;
			}
		}

		private int getIndexToSelect(bool selectRadioButtons2)
		{
			int value;
			bool success;
			if (selectRadioButtons2)
			{
				success = Int32.TryParse(IndexToSelectTextBox2.Text, out value);
			}
			else
			{
				success = Int32.TryParse(IndexToSelectTextBox1.Text, out value);
			}

			if (success)
			{
				if (value >= 3)
				{
					return 1;
				}
				if (value < -1)
				{
					return -2;
				}
				return value;
			}
			return -2;
		}
	}
}
