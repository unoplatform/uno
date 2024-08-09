using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample(
		"TextBox",
		IsManualTest = true,
		IgnoreInSnapshotTests = true,
		Description =
			"Verifies the Paste event functionality. " +
			"When the CheckBox is not checked, pasting from clipboard should be possible. " +
			"When checked, pasting should not happen.")]
	public sealed partial class TextBox_PasteEvent : UserControl
	{
		public TextBox_PasteEvent()
		{
			InitializeComponent();
		}

		private void OnMultilineChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			SampleTextBox.AcceptsReturn = MultilineCheckBox.IsChecked == true;
		}

		private void SampleTextBox_Paste(object sender, TextControlPasteEventArgs e)
		{
			if (HandlePasteCheckBox.IsChecked == true)
			{
				e.Handled = true;
			}

			EventLogTextBlock.Text = "TextBox.Paste at " + DateTime.Now.ToString() + " " + (e.Handled ? " handled" : "not handled");
		}

		private void SamplePasswordBox_Paste(object sender, TextControlPasteEventArgs e)
		{
			if (HandlePasteCheckBox.IsChecked == true)
			{
				e.Handled = true;
			}

			EventLogTextBlock.Text = "PasswordBox.Paste at " + DateTime.Now.ToString() + " " + (e.Handled ? " handled" : "not handled");
		}
	}
}
