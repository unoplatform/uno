// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PipsPagerPage.xaml.cs, commit fc2d862
#pragma warning disable CS0105 // duplicate namespace because of WinUI source conversion

using System;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using PipsPagerButtonVisibility = Microsoft.UI.Xaml.Controls.PipsPagerButtonVisibility;
using PipsPagerSelectedIndexChangedEventArgs = Microsoft.UI.Xaml.Controls.PipsPagerSelectedIndexChangedEventArgs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	[Sample("PipsPager", "WinUI")]
	public sealed partial class PipsPagerPage : Page
	{
		Button previousPageButton;
		Button nextPageButton;
		ItemsRepeater repeater;
		ItemsRepeater verticalOrientationPipsPagerRepeater;

		public List<string> Pictures = new List<string>()
		{
			"Assets/ingredient1.png",
			"Assets/ingredient2.png",
			"Assets/ingredient3.png",
			"Assets/ingredient4.png",
			"Assets/ingredient5.png",
			"Assets/ingredient6.png",
			"Assets/ingredient7.png",
			"Assets/ingredient8.png",
		};

		public PipsPagerPage()
		{
			this.InitializeComponent();
			this.Loaded += OnLoad;
		}

		private void OnLoad(object sender, RoutedEventArgs args)
		{
			var rootPanel = VisualTreeHelper.GetChild(TestPipsPager, 0);
			previousPageButton = VisualTreeHelper.GetChild(rootPanel, 0) as Button;
			nextPageButton = VisualTreeHelper.GetChild(rootPanel, 2) as Button;
			repeater = rootPanel.FindVisualChildByType<ItemsRepeater>();
			var rootPanelVerticalPipsPager = VisualTreeHelper.GetChild(TestPipsPagerVerticalOrientation, 0);
			verticalOrientationPipsPagerRepeater = rootPanelVerticalPipsPager.FindVisualChildByType<ItemsRepeater>();


			PreviousPageButtonVisibilityComboBox.SelectionChanged += OnPreviousPageButtonVisibilityChanged;
			NextPageButtonVisibilityComboBox.SelectionChanged += OnNextPageButtonVisibilityChanged;
			TestPipsPagerNumberOfPagesComboBox.SelectionChanged += OnNumberOfPagesChanged;
			TestPipsPagerMaxVisiblePipsComboBox.SelectionChanged += OnMaxVisiblePipsChanged;
			TestPipsPagerOrientationComboBox.SelectionChanged += OnOrientationChanged;
			TestPipsPagerWrapModeComboBox.SelectionChanged += OnWrapModeChanged;
			TestPipsPager.PointerEntered += TestPipsPager_PointerEntered;
			TestPipsPager.PointerExited += TestPipsPager_PointerExited;
			TestPipsPager.GotFocus += TestPipsPager_GotFocus;
			TestPipsPager.LostFocus += TestPipsPager_LostFocus;
			TestPipsPager.PointerCanceled += TestPipsPager_PointerCanceled;
			previousPageButton.IsEnabledChanged += OnButtonEnabledChanged;
			nextPageButton.IsEnabledChanged += OnButtonEnabledChanged;
			repeater.GotFocus += OnRepeaterGotFocus;


			PreviousPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(previousPageButton);
			NextPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(nextPageButton);

			PreviousPageButtonIsEnabledCheckBox.IsChecked = previousPageButton?.IsEnabled;
			NextPageButtonIsEnabledCheckBox.IsChecked = nextPageButton?.IsEnabled;

			CurrentNumberOfPagesTextBlock.Text = GetNumberOfPages();
			CurrentMaxVisiblePipsTextBlock.Text = $"Current max visual indicators: {TestPipsPager.MaxVisiblePips}";
			CurrentOrientationTextBlock.Text = GetCurrentOrientation();
		}

		private void SkipToLastPage(object sender, RoutedEventArgs args)
		{
			TestPipsPager.SelectedPageIndex = TestPipsPager.NumberOfPages - 1;
		}

		private void SkipToFirstPage(object sender, RoutedEventArgs args)
		{
			TestPipsPager.SelectedPageIndex = 0;
		}

		private void OnGetPipsPagerButtonSizesClicked(object sender, RoutedEventArgs args)
		{
			//Button horizontalOrientationButton;
			if (repeater.TryGetElement(0) as FrameworkElement is var horizontalOrientationPip && horizontalOrientationPip != null)
			{
				HorizontalOrientationPipsPagerButtonWidthTextBlock.Text = $"{horizontalOrientationPip.ActualWidth}";
				HorizontalOrientationPipsPagerButtonHeightTextBlock.Text = $"{horizontalOrientationPip.ActualHeight}";
			}
			if (verticalOrientationPipsPagerRepeater.TryGetElement(1) as FrameworkElement is var verticalOrientationPip && verticalOrientationPip != null)
			{
				VerticalOrientationPipsPagerButtonWidthTextBlock.Text = $"{verticalOrientationPip.ActualWidth}";
				VerticalOrientationPipsPagerButtonHeightTextBlock.Text = $"{verticalOrientationPip.ActualHeight}";
			}
		}

		private void OnRepeaterGotFocus(object sender, RoutedEventArgs e)
		{
			FocusedPageIndexTextBlock.Text = $"Current focused page index: {repeater.GetElementIndex((UIElement)e.OriginalSource).ToString()}";
		}

		private void OnButtonEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender.Equals(previousPageButton))
			{
				PreviousPageButtonIsEnabledCheckBox.IsChecked = previousPageButton?.IsEnabled;
			}
			else
			{
				NextPageButtonIsEnabledCheckBox.IsChecked = nextPageButton?.IsEnabled;
			}
		}

		private string GetCurrentOrientation()
		{
			return $"Current orientation is: {TestPipsPager.Orientation}";
		}

		private string GetNumberOfPages()
		{
			string prefix = "Current number of pages: ";
			int numberOfPages = TestPipsPager.NumberOfPages;
			return numberOfPages > 0 ? $"{prefix}{numberOfPages}" : $"{prefix}Inifinite";
		}
		private bool IsButtonVisible(Button btn)
		{
			return btn?.Visibility == Visibility.Visible && btn?.Opacity != 0;
		}

		private void UpdateButtonVisibilityCheckboxes()
		{
			PreviousPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(previousPageButton);
			NextPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(nextPageButton);
		}

		private void TestPipsPager_GotFocus(object sender, RoutedEventArgs e)
		{
			UpdateButtonVisibilityCheckboxes();
		}

		private void TestPipsPager_LostFocus(object sender, RoutedEventArgs e)
		{
			UpdateButtonVisibilityCheckboxes();
		}

		private void TestPipsPager_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			UpdateButtonVisibilityCheckboxes();
		}

		private void TestPipsPager_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			UpdateButtonVisibilityCheckboxes();
		}

		private void TestPipsPager_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			UpdateButtonVisibilityCheckboxes();
		}

		public void OnPreviousPageButtonVisibilityChanged(object sender, SelectionChangedEventArgs e)
		{
			TestPipsPager.PreviousButtonVisibility = ConvertComboBoxItemToVisibilityEnum((sender as ComboBox).SelectedItem as ComboBoxItem, TestPipsPager.PreviousButtonVisibility);
			UpdateButtonVisibilityCheckboxes();
		}
		public void OnNextPageButtonVisibilityChanged(object sender, SelectionChangedEventArgs e)
		{
			TestPipsPager.NextButtonVisibility = ConvertComboBoxItemToVisibilityEnum((sender as ComboBox).SelectedItem as ComboBoxItem, TestPipsPager.NextButtonVisibility);
			UpdateButtonVisibilityCheckboxes();
		}
		public void OnNumberOfPagesChanged(object sender, SelectionChangedEventArgs e)
		{
			TestPipsPager.NumberOfPages = ConvertComboBoxItemToNumberOfPages((sender as ComboBox).SelectedItem as ComboBoxItem);
			CurrentNumberOfPagesTextBlock.Text = GetNumberOfPages();
		}
		public void OnMaxVisiblePipsChanged(object sender, SelectionChangedEventArgs e)
		{
			TestPipsPager.MaxVisiblePips = ConvertComboBoxItemToNumberOfPages((sender as ComboBox).SelectedItem as ComboBoxItem);
			CurrentMaxVisiblePipsTextBlock.Text = $"Current max visual indicators: {TestPipsPager.MaxVisiblePips}";
		}

		public void OnSelectedIndexChanged(PipsPager sender, PipsPagerSelectedIndexChangedEventArgs args)
		{
			PreviousPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(previousPageButton);
			NextPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(nextPageButton);

			PreviousPageButtonIsEnabledCheckBox.IsChecked = previousPageButton?.IsEnabled;
			NextPageButtonIsEnabledCheckBox.IsChecked = nextPageButton?.IsEnabled;

			CurrentPageIndexTextBlock.Text = $"Current index is: {sender.SelectedPageIndex}";
		}

		public void OnOrientationChanged(object sender, SelectionChangedEventArgs e)
		{
			Orientation orientation = TestPipsPager.Orientation;
			string selectedItem = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string;
			if (!Enum.TryParse<Orientation>(selectedItem, out orientation))
			{
				System.Diagnostics.Debug.WriteLine("Unable to convert " + selectedItem + " to Orientation Enum");
			}
			TestPipsPager.Orientation = orientation;
			CurrentOrientationTextBlock.Text = GetCurrentOrientation();
		}

		public void OnWrapModeChanged(object sender, SelectionChangedEventArgs e)
		{
			TestPipsPager.WrapMode = (sender as ComboBox).SelectedIndex == 0 ? PipsPagerWrapMode.None : PipsPagerWrapMode.Wrap;
		}

		private PipsPagerButtonVisibility ConvertComboBoxItemToVisibilityEnum(ComboBoxItem item, PipsPagerButtonVisibility defaultValue)
		{
			PipsPagerButtonVisibility newVisibility = defaultValue;
			string selectedItem = item.Content as string;
			if (!Enum.TryParse<PipsPagerButtonVisibility>(selectedItem, out newVisibility))
			{
				System.Diagnostics.Debug.WriteLine("Unable to convert " + selectedItem + " to PipsPagerButtonVisibility Enum");
			}

			return newVisibility;
		}
		private int ConvertComboBoxItemToNumberOfPages(ComboBoxItem item)
		{
			int numberOfPages = -1;
			string digitsOnlyString = new String((item.Content as string).Where(Char.IsDigit).ToArray());
			if (!string.IsNullOrEmpty(digitsOnlyString))
			{
				Int32.TryParse(digitsOnlyString, out numberOfPages);
			}
			return numberOfPages;
		}

		private void GoToExamplesPage(object sender, RoutedEventArgs args)
		{
			Frame.NavigateWithoutAnimation(typeof(PipsPagerExamples));
		}
	}
}
