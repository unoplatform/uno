// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics.Display;
using Uno.UI.Samples.Controls;

#if HAS_UNO
using TwoPaneView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneView;
using TwoPaneViewMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneViewMode;
using TwoPaneViewPriority = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneViewPriority;
using TwoPaneViewWideModeConfiguration = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneViewWideModeConfiguration;
using TwoPaneViewTallModeConfiguration = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneViewTallModeConfiguration;
using DisplayRegionHelperTestApi = Microsoft/* UWP don't rename */.UI.Xaml.Controls.DisplayRegionHelperTestApi;
#endif

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.TwoPaneViewTests
{
	[Sample("MUX", Name = "TwoPaneView")]
	public sealed partial class TwoPaneViewPage : Page
	{
		// Need to be the same as c_defaultMinWideModeWidth/c_defaultMinTallModeHeight in TwoPaneViewFactory.cpp
		private const double c_defaultMinWideModeWidth = 641.0;
		private const double c_defaultMinTallModeHeight = 641.0;

		// Need to be the same as c_simulatedPaneWidth/c_simulatedPaneHeight/c_simulatedMiddle in TwoPaneViewTests.cs
		private const double c_simulatedPaneWidth = 300.0;
		private const double c_simulatedPaneHeight = 400.0;
		private const double c_simulatedMiddle = 12.0;

		// Need to be the same as c_controlMargin in TwoPaneViewTests.cs
		private Thickness c_controlMargin = new Thickness(40, 10, 30, 20);

		public TwoPaneViewPage()
		{
			this.InitializeComponent();

#if HAS_UNO
			DisplayRegionHelperTestApi.SimulateDisplayRegions = false;

			TwoPaneView.ModeChanged += TwoPaneView_ModeChanged;
			ConfigurationTextBlock.Text = TwoPaneView.Mode.ToString();
#endif
		}

#if HAS_UNO
		private void TwoPaneView_ModeChanged(TwoPaneView sender, object args)
		{
			ConfigurationTextBlock.Text = TwoPaneView.Mode.ToString();
		}

		private string ViewModeToString(TwoPaneViewMode configuration)
		{
			switch (configuration)
			{
				case TwoPaneViewMode.SinglePane: return "SinglePane";
				case TwoPaneViewMode.Wide: return "Wide";
				case TwoPaneViewMode.Tall: return "Tall";
			}
			return string.Empty;
		}

		private void PanePriorityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TwoPaneView == null) return;

			TwoPaneView.PanePriority = PanePriorityComboBox.SelectedIndex == 0 ? TwoPaneViewPriority.Pane1 : TwoPaneViewPriority.Pane2;
		}

		private void WideModeConfigurationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TwoPaneView == null) return;

			switch (WideModeConfigurationComboBox.SelectedIndex)
			{
				case 0: TwoPaneView.WideModeConfiguration = TwoPaneViewWideModeConfiguration.LeftRight; break;
				case 1: TwoPaneView.WideModeConfiguration = TwoPaneViewWideModeConfiguration.RightLeft; break;
				case 2: TwoPaneView.WideModeConfiguration = TwoPaneViewWideModeConfiguration.SinglePane; break;
			}
		}

		private void TallModeConfigurationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TwoPaneView == null) return;

			switch (TallModeConfigurationComboBox.SelectedIndex)
			{
				case 0: TwoPaneView.TallModeConfiguration = TwoPaneViewTallModeConfiguration.TopBottom; break;
				case 1: TwoPaneView.TallModeConfiguration = TwoPaneViewTallModeConfiguration.BottomTop; break;
				case 2: TwoPaneView.TallModeConfiguration = TwoPaneViewTallModeConfiguration.SinglePane; break;
			}
		}

		private void MinWideModeWidthTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TwoPaneView == null) return;

			double newWidth;
			if (double.TryParse(MinWideModeWidthTextBox.Text, out newWidth))
			{
				TwoPaneView.MinWideModeWidth = newWidth;
			}
		}

		private void MinTallModeHeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TwoPaneView == null) return;

			double newHeight;
			if (double.TryParse(MinTallModeHeightTextBox.Text, out newHeight))
			{
				TwoPaneView.MinTallModeHeight = newHeight;
			}
		}

		private void WidthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TwoPaneView == null) return;

			switch (WidthComboBox.SelectedIndex)
			{
				case 0: TwoPaneView.Width = double.NaN; break;
				case 1: TwoPaneView.Width = c_defaultMinWideModeWidth + 10; break;
				case 2: TwoPaneView.Width = c_defaultMinWideModeWidth - 10; break;
			}
		}

		private void HeightComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TwoPaneView == null) return;

			switch (HeightComboBox.SelectedIndex)
			{
				case 0: TwoPaneView.Height = double.NaN; break;
				case 1: TwoPaneView.Height = c_defaultMinTallModeHeight + 10; break;
				case 2: TwoPaneView.Height = c_defaultMinTallModeHeight - 10; break;
			}
		}

		private void SimulateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TwoPaneView == null) return;

			switch (SimulateComboBox.SelectedIndex)
			{
				case 0:
					DisplayRegionHelperTestApi.SimulateDisplayRegions = false;
					SimulatedWindow.Width = double.NaN;
					SimulatedWindow.Height = double.NaN;
					break;

				case 1:
					DisplayRegionHelperTestApi.SimulateDisplayRegions = true;
					DisplayRegionHelperTestApi.SimulateMode = TwoPaneViewMode.Wide;
					SimulatedWindow.Width = c_simulatedPaneWidth * 2 + c_simulatedMiddle;
					SimulatedWindow.Height = c_simulatedPaneHeight;
					break;

				case 2:
					DisplayRegionHelperTestApi.SimulateDisplayRegions = true;
					DisplayRegionHelperTestApi.SimulateMode = TwoPaneViewMode.Tall;
					SimulatedWindow.Width = c_simulatedPaneHeight;
					SimulatedWindow.Height = c_simulatedPaneWidth * 2 + c_simulatedMiddle;
					break;
			}
		}

		private void AddMarginCheckBox_Checked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			TwoPaneView.Margin = c_controlMargin;
		}

		private void AddMarginCheckBox_Unchecked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			TwoPaneView.Margin = new Thickness(0);
		}

		private void OneSideCheckBox_Checked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			TwoPaneView.HorizontalAlignment = HorizontalAlignment.Left;
			TwoPaneView.Width = c_simulatedPaneWidth;
		}

		private void OneSideCheckBox_Unchecked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			TwoPaneView.HorizontalAlignment = HorizontalAlignment.Stretch;
			TwoPaneView.Width = double.NaN;
		}

		private void PaneSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
		{
			TextBlock widthTextBlock = (sender == Pane1Content) ? WidthText1 : WidthText2;
			TextBlock heightTextBlock = (sender == Pane1Content) ? HeightText1 : HeightText2;

			widthTextBlock.Text = ((int)e.NewSize.Width).ToString();
			heightTextBlock.Text = ((int)e.NewSize.Height).ToString();

			if (TwoPaneView.Mode == TwoPaneViewMode.SinglePane)
			{
				SpacingTextBox.Text = "0";
			}
			else
			{
				GeneralTransform t = Pane1Content.TransformToVisual(SimulatedWindow);
				Rect bounds = new Rect(0, 0, Pane1Content.ActualWidth, Pane1Content.ActualHeight);
				Rect rc1 = t.TransformBounds(bounds);

				t = Pane2Content.TransformToVisual(SimulatedWindow);
				bounds = new Rect(0, 0, Pane2Content.ActualWidth, Pane2Content.ActualHeight);
				Rect rc2 = t.TransformBounds(bounds);

				int spacing = (int)((TwoPaneView.Mode == TwoPaneViewMode.Wide)
					? rc2.X - (rc1.X + rc1.Width)
					: rc2.Y - (rc1.Y + rc1.Height));

				SpacingTextBox.Text = spacing.ToString();
			}
		}

		private void TwoPaneView_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
		{
			ControlWidthText.Text = ((int)e.NewSize.Width).ToString();
			ControlHeightText.Text = ((int)e.NewSize.Height).ToString();
		}

		private void Pane1LengthTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateLength(1);
		}

		private void Pane1LengthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateLength(1);
		}

		private void Pane2LengthTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateLength(2);
		}

		private void Pane2LengthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateLength(2);
		}

		private void UpdateLength(int pane)
		{
			if (TwoPaneView == null) return;

			TextBox textBox = pane == 1 ? Pane1LengthTextBox : Pane2LengthTextBox;
			ComboBox comboBox = pane == 1 ? Pane1LengthComboBox : Pane2LengthComboBox;

			double value;
			if (double.TryParse(textBox.Text, out value))
			{
				GridUnitType type = GridUnitType.Auto;

				switch (comboBox.SelectedIndex)
				{
					case 1: type = GridUnitType.Pixel; break;
					case 2: type = GridUnitType.Star; break;
				}

				GridLength gridLength = new GridLength(value, type);

				if (pane == 1)
				{
					TwoPaneView.Pane1Length = gridLength;
				}
				else
				{
					TwoPaneView.Pane2Length = gridLength;
				}
			}
		}
#endif
	}
}
