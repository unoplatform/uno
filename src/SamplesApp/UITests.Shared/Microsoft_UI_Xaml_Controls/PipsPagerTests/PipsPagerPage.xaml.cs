// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using PipsPagerButtonVisibility = Microsoft.UI.Xaml.Controls.PipsPagerButtonVisibility;
using PipsPagerSelectedIndexChangedEventArgs = Microsoft.UI.Xaml.Controls.PipsPagerSelectedIndexChangedEventArgs;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	[Sample("PipsPager", "WinUI")]
	public sealed partial class PipsPagerPage : Page
    {
        Button previousPageButton;
        Button nextPageButton;

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

            PreviousPageButtonVisibilityComboBox.SelectionChanged += OnPreviousPageButtonVisibilityChanged;
            NextPageButtonVisibilityComboBox.SelectionChanged += OnNextPageButtonVisibilityChanged;
            TestPipsPagerNumberOfPagesComboBox.SelectionChanged += OnNumberOfPagesChanged;
            TestPipsPagerMaxVisualIndicatorsComboBox.SelectionChanged += OnMaxVisualIndicatorsChanged;
            TestPipsPagerOrientationComboBox.SelectionChanged += OnOrientationChanged;
            TestPipsPager.PointerEntered += TestPipsPager_PointerEntered;
            TestPipsPager.PointerExited += TestPipsPager_PointerExited;
            previousPageButton.IsEnabledChanged += OnButtonEnabledChanged; ;
            nextPageButton.IsEnabledChanged += OnButtonEnabledChanged;

            PreviousPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(previousPageButton);
            NextPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(nextPageButton);

            PreviousPageButtonIsEnabledCheckBox.IsChecked = previousPageButton?.IsEnabled;
            NextPageButtonIsEnabledCheckBox.IsChecked = nextPageButton?.IsEnabled;

            CurrentNumberOfPagesTextBlock.Text = GetNumberOfPages();
            CurrentMaxVisualIndicatorsTextBlock.Text = $"Current max visual indicators: {TestPipsPager.MaxVisualIndicators}";
            CurrentOrientationTextBlock.Text = GetCurrentOrientation();
        }

        private void OnButtonEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender == previousPageButton)
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

        private void TestPipsPager_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            PreviousPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(previousPageButton);
            NextPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(nextPageButton);
        }

        private void TestPipsPager_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            PreviousPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(previousPageButton);
            NextPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(nextPageButton);
        }

        public void OnPreviousPageButtonVisibilityChanged(object sender, SelectionChangedEventArgs e)
        {
            TestPipsPager.PreviousButtonVisibility = ConvertComboBoxItemToVisibilityEnum((sender as ComboBox).SelectedItem as ComboBoxItem, TestPipsPager.PreviousButtonVisibility);
            PreviousPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(previousPageButton);
        }
        public void OnNextPageButtonVisibilityChanged(object sender, SelectionChangedEventArgs e)
        {
            TestPipsPager.NextButtonVisibility = ConvertComboBoxItemToVisibilityEnum((sender as ComboBox).SelectedItem as ComboBoxItem, TestPipsPager.NextButtonVisibility);
            NextPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(nextPageButton);
        }
        public void OnNumberOfPagesChanged(object sender, SelectionChangedEventArgs e)
        {
            TestPipsPager.NumberOfPages = ConvertComboBoxItemToNumberOfPages((sender as ComboBox).SelectedItem as ComboBoxItem);
            CurrentNumberOfPagesTextBlock.Text = GetNumberOfPages();
        }
        public void OnMaxVisualIndicatorsChanged(object sender, SelectionChangedEventArgs e)
        {
            TestPipsPager.MaxVisualIndicators = ConvertComboBoxItemToNumberOfPages((sender as ComboBox).SelectedItem as ComboBoxItem);
            CurrentMaxVisualIndicatorsTextBlock.Text = $"Current max visual indicators: {TestPipsPager.MaxVisualIndicators}";
        }

        public void OnSelectedIndexChanged(object sender, PipsPagerSelectedIndexChangedEventArgs args)
        {
            PreviousPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(previousPageButton);
            NextPageButtonIsVisibleCheckBox.IsChecked = IsButtonVisible(nextPageButton);

            PreviousPageButtonIsEnabledCheckBox.IsChecked = previousPageButton?.IsEnabled;
            NextPageButtonIsEnabledCheckBox.IsChecked = nextPageButton?.IsEnabled;

            CurrentPageIndexTextBlock.Text = $"Current index is: {args.NewPageIndex}";
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
    }
}
