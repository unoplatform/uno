#pragma warning disable CS0105 // Using directive appeared previously in this namespace
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Globalization.NumberFormatting;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests
{
	[Sample("NumberBox", "WinUI", Name="MUX_Test")]
	public sealed partial class MUX_Test : UserControl
	{
		public MUX_Test()
		{
			this.InitializeComponent();
			TestNumberBox.RegisterPropertyChangedCallback(NumberBox.TextProperty, new DependencyPropertyChangedCallback(TextPropertyChanged));
		}

		private void SpinMode_Changed(object sender, RoutedEventArgs e)
		{
			if (TestNumberBox != null)
			{
				if (SpinModeComboBox.SelectedIndex == 0)
				{
					TestNumberBox.SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden;
				}
				else if (SpinModeComboBox.SelectedIndex == 1)
				{
					TestNumberBox.SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact;
				}
				else if (SpinModeComboBox.SelectedIndex == 2)
				{
					TestNumberBox.SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline;
				}
			}
		}

		private void Validation_Changed(object sender, RoutedEventArgs e)
		{
			if (TestNumberBox != null)
			{
				if (ValidationComboBox.SelectedIndex == 0)
				{
					TestNumberBox.ValidationMode = NumberBoxValidationMode.InvalidInputOverwritten;
				}
				else if (ValidationComboBox.SelectedIndex == 1)
				{
					TestNumberBox.ValidationMode = NumberBoxValidationMode.Disabled;
				}
			}
		}

		private void MinCheckBox_CheckChanged(object sender, RoutedEventArgs e)
		{
			MinNumberBox.IsEnabled = (bool)MinCheckBox.IsChecked;
			MinValueChanged(null, null);
		}

		private void MaxCheckBox_CheckChanged(object sender, RoutedEventArgs e)
		{
			MaxNumberBox.IsEnabled = (bool)MaxCheckBox.IsChecked;
			MaxValueChanged(null, null);
		}

		private void MinValueChanged(object sender, object e)
		{
			if (TestNumberBox != null)
			{
				TestNumberBox.Minimum = (bool)MinCheckBox.IsChecked ? MinNumberBox.Value : double.MinValue;
			}
		}

		private void MaxValueChanged(object sender, object e)
		{
			if (TestNumberBox != null)
			{
				TestNumberBox.Maximum = (bool)MaxCheckBox.IsChecked ? MaxNumberBox.Value : double.MaxValue;
			}
		}

		private void NumberBoxValueChanged(object sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs e)
		{
			if (TestNumberBox != null && NewValueTextBox != null && OldValueTextBox != null)
			{
				NewValueTextBox.Text = e.NewValue.ToString();
				OldValueTextBox.Text = e.OldValue.ToString();
			}
		}

		private void CustomFormatterButton_Click(object sender, RoutedEventArgs e)
		{
			List<string> languages = new List<string>() { "fr-FR" };
			DecimalFormatter formatter = new DecimalFormatter(languages, "FR");
			formatter.IntegerDigits = 1;
			formatter.FractionDigits = 2;
			TestNumberBox.NumberFormatter = formatter;
		}

		private void SetTextButton_Click(object sender, RoutedEventArgs e)
		{
			TestNumberBox.Text = "15";
		}

		private void SetValueButton_Click(object sender, RoutedEventArgs e)
		{
			TestNumberBox.Value = 42;
		}

		private void SetNaNButton_Click(object sender, RoutedEventArgs e)
		{
			TestNumberBox.Value = Double.NaN;
		}

		private void TextPropertyChanged(DependencyObject o, DependencyProperty p)
		{
			TextTextBox.Text = TestNumberBox.Text;
		}
	}
}
