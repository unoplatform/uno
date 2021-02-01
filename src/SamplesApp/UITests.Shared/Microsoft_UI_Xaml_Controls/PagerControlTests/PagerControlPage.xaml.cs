using System.Collections.Specialized;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UITests.Microsoft_UI_Xaml_Controls.PagerControlTests
{
	[Sample("PagerControl", "WinUI")]
	public sealed partial class PagerControlPage : Page
	{
		ComboBox pagerComboBox;
		NumberBox pagerNumberBox;
		ItemsRepeater pagerItemsRepeater;

		Button firstPageButton;
		Button previousPageButton;
		Button nextPageButton;
		Button lastPageButton;

		public PagerControlPage()
		{
			this.InitializeComponent();
#if HAS_UNO
			this.Loaded += OnLoad;
#endif

		}

#if HAS_UNO
		private void OnLoad(object sender, RoutedEventArgs args)
		{
			PagerDisplayModeComboBox.SelectionChanged += OnDisplayModeChanged;
			FirstPageButtonVisibilityComboBox.SelectionChanged += OnFirstButtonVisibilityChanged;
			PreviousPageButtonVisibilityComboBox.SelectionChanged += OnPreviousButtonVisibilityChanged;
			NextPageButtonVisibilityComboBox.SelectionChanged += OnNextButtonVisibilityChanged;
			LastPageButtonVisibilityComboBox.SelectionChanged += OnLastButtonVisibilityChanged;

			var rootGrid = VisualTreeHelper.GetChild(TestPager, 0);
			var backButtonsPanel = VisualTreeHelper.GetChild(rootGrid, 0);
			firstPageButton = VisualTreeHelper.GetChild(backButtonsPanel, 0) as Button;
			previousPageButton = VisualTreeHelper.GetChild(backButtonsPanel, 1) as Button;

			var boxPanels = VisualTreeHelper.GetChild(rootGrid, 1);
			pagerNumberBox = VisualTreeHelper.GetChild(boxPanels, 1) as NumberBox;
			pagerComboBox = VisualTreeHelper.GetChild(boxPanels, 2) as ComboBox;
			pagerItemsRepeater = VisualTreeHelper.GetChild(rootGrid, 2) as ItemsRepeater;

			var forwardButtonsPanel = VisualTreeHelper.GetChild(rootGrid, 4);
			nextPageButton = VisualTreeHelper.GetChild(forwardButtonsPanel, 0) as Button;
			lastPageButton = VisualTreeHelper.GetChild(forwardButtonsPanel, 1) as Button;

			NumberBoxVisibilityCheckBox.IsChecked = pagerNumberBox.Visibility == Visibility.Visible;
			ComboBoxVisibilityCheckBox.IsChecked = pagerComboBox.Visibility == Visibility.Visible;
			NumberPanelVisibilityCheckBox.IsChecked = pagerItemsRepeater.Visibility == Visibility.Visible;
			NumberBoxIsEnabledCheckBox.IsChecked = pagerNumberBox.IsEnabled;
			ComboBoxIsEnabledCheckBox.IsChecked = pagerComboBox.IsEnabled;

			FirstPageButtonVisibilityCheckBox.IsChecked = firstPageButton?.Visibility == Visibility.Visible && firstPageButton?.Opacity != 0;
			PreviousPageButtonVisibilityCheckBox.IsChecked = previousPageButton?.Visibility == Visibility.Visible && previousPageButton?.Opacity != 0;
			NextPageButtonVisibilityCheckBox.IsChecked = nextPageButton?.Visibility == Visibility.Visible && nextPageButton?.Opacity != 0;
			LastPageButtonVisibilityCheckBox.IsChecked = lastPageButton?.Visibility == Visibility.Visible && lastPageButton?.Opacity != 0;

			FirstPageButtonIsEnabledCheckBox.IsChecked = firstPageButton?.IsEnabled;
			PreviousPageButtonIsEnabledCheckBox.IsChecked = previousPageButton?.IsEnabled;
			NextPageButtonIsEnabledCheckBox.IsChecked = nextPageButton?.IsEnabled;
			LastPageButtonIsEnabledCheckBox.IsChecked = lastPageButton?.IsEnabled;
			UpdateNumberPanelContentTextBlock(this, null);
		}

		private void UpdateNumberPanelContentTextBlock(object sender, NotifyCollectionChangedEventArgs args)
		{
			NumberPanelContentTextBlock.Text = "";
			foreach (var item in TestPager.TemplateSettings.NumberPanelItems)
			{
				if (item.GetType() == typeof(SymbolIcon))
				{
					NumberPanelContentTextBlock.Text += (item as SymbolIcon).Symbol;
				}
				else if (item.GetType() == typeof(Button))
				{
					NumberPanelContentTextBlock.Text += (item as Button).Content;
				}
			}
		}

		private void NumberOfPagesSetterButtonClicked(object sender, RoutedEventArgs args)
		{
			if (TestPager.NumberOfPages == 5)
			{
				TestPager.NumberOfPages = 100;
				NumberOfPagesSetterButton.Content = "Set NumberOfPages to 5";
			}
			else
			{
				TestPager.NumberOfPages = 5;
				NumberOfPagesSetterButton.Content = "Set NumberOfPages to 100";
			}

			NumberBoxVisibilityCheckBox.IsChecked = pagerNumberBox?.Visibility == Visibility.Visible;
			ComboBoxVisibilityCheckBox.IsChecked = pagerComboBox?.Visibility == Visibility.Visible;
			NumberPanelVisibilityCheckBox.IsChecked = pagerItemsRepeater?.Visibility == Visibility.Visible;
			NumberBoxIsEnabledCheckBox.IsChecked = pagerNumberBox?.IsEnabled;
			ComboBoxIsEnabledCheckBox.IsChecked = pagerComboBox?.IsEnabled;
		}

		private void NumberOfPagesInfinityButtonClicked(object sender, RoutedEventArgs args)
		{
			TestPager.NumberOfPages = -1;

			NumberBoxVisibilityCheckBox.IsChecked = pagerNumberBox?.Visibility == Visibility.Visible;
			ComboBoxVisibilityCheckBox.IsChecked = pagerComboBox?.Visibility == Visibility.Visible;
			NumberPanelVisibilityCheckBox.IsChecked = pagerItemsRepeater?.Visibility == Visibility.Visible;
			NumberBoxIsEnabledCheckBox.IsChecked = pagerNumberBox?.IsEnabled;
			ComboBoxIsEnabledCheckBox.IsChecked = pagerComboBox?.IsEnabled;
		}

		private void IncreaseNumberOfPagesButtonClicked(object sender, RoutedEventArgs args)
		{
			TestPager.NumberOfPages += 1;

			NumberBoxVisibilityCheckBox.IsChecked = pagerNumberBox?.Visibility == Visibility.Visible;
			ComboBoxVisibilityCheckBox.IsChecked = pagerComboBox?.Visibility == Visibility.Visible;
			NumberPanelVisibilityCheckBox.IsChecked = pagerItemsRepeater?.Visibility == Visibility.Visible;
			NumberBoxIsEnabledCheckBox.IsChecked = pagerNumberBox?.IsEnabled;
			ComboBoxIsEnabledCheckBox.IsChecked = pagerComboBox?.IsEnabled;
		}

		private void OnSelectedIndexChanged(PagerControl sender, PagerControlSelectedIndexChangedEventArgs args)
		{
			if (PreviousPageTextBlock == null)
			{
				return;
			}
			UpdateNumberPanelContentTextBlock(this, null);
			PreviousPageTextBlock.Text = args.PreviousPageIndex.ToString();
			CurrentPageTextBlock.Text = args.NewPageIndex.ToString();

			FirstPageButtonVisibilityCheckBox.IsChecked = firstPageButton?.Visibility == Visibility.Visible && firstPageButton?.Opacity != 0;
			PreviousPageButtonVisibilityCheckBox.IsChecked = previousPageButton?.Visibility == Visibility.Visible && previousPageButton?.Opacity != 0;
			NextPageButtonVisibilityCheckBox.IsChecked = nextPageButton?.Visibility == Visibility.Visible && nextPageButton?.Opacity != 0;
			LastPageButtonVisibilityCheckBox.IsChecked = lastPageButton?.Visibility == Visibility.Visible && lastPageButton?.Opacity != 0;

			FirstPageButtonIsEnabledCheckBox.IsChecked = firstPageButton?.IsEnabled;
			PreviousPageButtonIsEnabledCheckBox.IsChecked = previousPageButton?.IsEnabled;
			NextPageButtonIsEnabledCheckBox.IsChecked = nextPageButton?.IsEnabled;
			LastPageButtonIsEnabledCheckBox.IsChecked = lastPageButton?.IsEnabled;
		}

		private void OnDisplayModeChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = PagerDisplayModeComboBox.SelectedItem;

			if (item == this.AutoDisplayModeItem)
			{
				TestPager.DisplayMode = PagerControlDisplayMode.Auto;
				CustomizedPager.DisplayMode = PagerControlDisplayMode.Auto;
			}
			else if (item == this.NumberBoxDisplayModeItem)
			{
				TestPager.DisplayMode = PagerControlDisplayMode.NumberBox;
				CustomizedPager.DisplayMode = PagerControlDisplayMode.NumberBox;
			}
			else if (item == this.ComboBoxDisplayModeItem)
			{
				TestPager.DisplayMode = PagerControlDisplayMode.ComboBox;
				CustomizedPager.DisplayMode = PagerControlDisplayMode.ComboBox;
			}
			else if (item == this.NumberPanelDisplayModeItem)
			{
				TestPager.DisplayMode = PagerControlDisplayMode.ButtonPanel;
				CustomizedPager.DisplayMode = PagerControlDisplayMode.ButtonPanel;
			}

			NumberBoxVisibilityCheckBox.IsChecked = pagerNumberBox?.Visibility == Visibility.Visible;
			ComboBoxVisibilityCheckBox.IsChecked = pagerComboBox?.Visibility == Visibility.Visible;
			NumberPanelVisibilityCheckBox.IsChecked = pagerItemsRepeater?.Visibility == Visibility.Visible;
			NumberBoxIsEnabledCheckBox.IsChecked = pagerNumberBox?.IsEnabled;
			ComboBoxIsEnabledCheckBox.IsChecked = pagerComboBox?.IsEnabled;
		}

		private void OnFirstButtonVisibilityChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = FirstPageButtonVisibilityComboBox.SelectedItem;

			if (item == this.NoneFirstPageButtonVisibilityItem)
			{
				TestPager.FirstButtonVisibility = PagerControlButtonVisibility.Hidden;
			}
			else if (item == this.AlwaysVisibleFirstPageButtonVisibilityItem)
			{
				TestPager.FirstButtonVisibility = PagerControlButtonVisibility.Visible;
			}
			else if (item == this.HiddenOnEdgeFirstPageButtonVisibilityItem)
			{
				TestPager.FirstButtonVisibility = PagerControlButtonVisibility.HiddenOnEdge;
			}

			FirstPageButtonVisibilityCheckBox.IsChecked = firstPageButton?.Visibility == Visibility.Visible && firstPageButton?.Opacity != 0;
		}

		private void OnPreviousButtonVisibilityChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = PreviousPageButtonVisibilityComboBox.SelectedItem;

			if (item == this.NonePreviousPageButtonVisibilityItem)
			{
				TestPager.PreviousButtonVisibility = PagerControlButtonVisibility.Hidden;
			}
			else if (item == this.AlwaysVisiblePreviousPageButtonVisibilityItem)
			{
				TestPager.PreviousButtonVisibility = PagerControlButtonVisibility.Visible;
			}
			else if (item == this.HiddenOnEdgePreviousPageButtonVisibilityItem)
			{
				TestPager.PreviousButtonVisibility = PagerControlButtonVisibility.HiddenOnEdge;
			}

			PreviousPageButtonVisibilityCheckBox.IsChecked = previousPageButton?.Visibility == Visibility.Visible && previousPageButton?.Opacity != 0;
		}

		private void OnNextButtonVisibilityChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = NextPageButtonVisibilityComboBox.SelectedItem;

			if (item == this.NoneNextPageButtonVisibilityItem)
			{
				TestPager.NextButtonVisibility = PagerControlButtonVisibility.Hidden;
			}
			else if (item == this.AlwaysVisibleNextPageButtonVisibilityItem)
			{
				TestPager.NextButtonVisibility = PagerControlButtonVisibility.Visible;
			}
			else if (item == this.HiddenOnEdgeNextPageButtonVisibilityItem)
			{
				TestPager.NextButtonVisibility = PagerControlButtonVisibility.HiddenOnEdge;
			}

			NextPageButtonVisibilityCheckBox.IsChecked = nextPageButton?.Visibility == Visibility.Visible && nextPageButton?.Opacity != 0;
		}
		private void OnLastButtonVisibilityChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = LastPageButtonVisibilityComboBox.SelectedItem;

			if (item == this.NoneLastPageButtonVisibilityItem)
			{
				TestPager.LastButtonVisibility = PagerControlButtonVisibility.Hidden;
			}
			else if (item == this.AlwaysVisibleLastPageButtonVisibilityItem)
			{
				TestPager.LastButtonVisibility = PagerControlButtonVisibility.Visible;
			}
			else if (item == this.HiddenOnEdgeLastPageButtonVisibilityItem)
			{
				TestPager.LastButtonVisibility = PagerControlButtonVisibility.HiddenOnEdge;
			}

			LastPageButtonVisibilityCheckBox.IsChecked = lastPageButton?.Visibility == Visibility.Visible && lastPageButton?.Opacity != 0;
		}
#endif
	}
}
