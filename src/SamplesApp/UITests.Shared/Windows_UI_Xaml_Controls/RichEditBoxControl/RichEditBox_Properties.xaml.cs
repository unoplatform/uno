using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_Properties",
		Description = "Demonstrates IsReadOnly, MaxLength, AcceptsReturn, TextWrapping, TextAlignment, and CharacterCasing.")]
	public sealed partial class RichEditBox_Properties : UserControl
	{
		public RichEditBox_Properties()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			// Populate editors with sample text
			ReadOnlyRichEditBox.Document.SetText(TextSetOptions.None,
				"This text can be toggled between read-only and editable mode using the switch below.");

			TextWrappingRichEditBox.Document.SetText(TextSetOptions.None,
				"This is a long line of text that demonstrates wrapping behavior. When TextWrapping is set to Wrap, this text will wrap to the next visual line. When set to NoWrap, it will extend beyond the visible area and may require horizontal scrolling.");

			TextAlignmentRichEditBox.Document.SetText(TextSetOptions.None,
				"This text demonstrates alignment. Try changing the alignment option below to see how the text position changes within the control.");

			// Alignment comparison boxes
			LeftAlignedBox.Document.SetText(TextSetOptions.None, "Left aligned sample text for comparison.");
			CenterAlignedBox.Document.SetText(TextSetOptions.None, "Center aligned sample text for comparison.");
			RightAlignedBox.Document.SetText(TextSetOptions.None, "Right aligned sample text for comparison.");
			JustifyAlignedBox.Document.SetText(TextSetOptions.None, "Justified sample text that should spread across the available width when it wraps.");
		}

		// IsReadOnly
		private void IsReadOnlyToggle_Toggled(object sender, RoutedEventArgs e)
		{
			ReadOnlyRichEditBox.IsReadOnly = IsReadOnlyToggle.IsOn;
		}

		// MaxLength
		private void ApplyMaxLengthButton_Click(object sender, RoutedEventArgs e)
		{
			var maxLength = (int)MaxLengthInput.Value;
			MaxLengthRichEditBox.MaxLength = maxLength;
			MaxLengthStatusTextBlock.Text = maxLength == 0
				? "Current MaxLength: 0 (unlimited)"
				: $"Current MaxLength: {maxLength}";
		}

		// AcceptsReturn
		private void AcceptsReturnToggle_Toggled(object sender, RoutedEventArgs e)
		{
			AcceptsReturnRichEditBox.AcceptsReturn = AcceptsReturnToggle.IsOn;
		}

		// TextWrapping
		private void TextWrappingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TextWrappingRichEditBox == null)
			{
				return;
			}

			var selected = (TextWrappingComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
			TextWrappingRichEditBox.TextWrapping = selected switch
			{
				"NoWrap" => TextWrapping.NoWrap,
				"WrapWholeWords" => TextWrapping.WrapWholeWords,
				_ => TextWrapping.Wrap,
			};
		}

		// TextAlignment
		private void TextAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TextAlignmentRichEditBox == null)
			{
				return;
			}

			var selected = (TextAlignmentComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
			TextAlignmentRichEditBox.TextAlignment = selected switch
			{
				"Center" => TextAlignment.Center,
				"Right" => TextAlignment.Right,
				"Justify" => TextAlignment.Justify,
				_ => TextAlignment.Left,
			};
		}

		// CharacterCasing
		private void CharacterCasingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (CharacterCasingRichEditBox == null)
			{
				return;
			}

			var selected = (CharacterCasingComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
			CharacterCasingRichEditBox.CharacterCasing = selected switch
			{
				"Upper" => CharacterCasing.Upper,
				"Lower" => CharacterCasing.Lower,
				_ => CharacterCasing.Normal,
			};
		}

		// Combined properties
		private void GetPropertiesButton_Click(object sender, RoutedEventArgs e)
		{
			CombinedPropertiesTextBlock.Text =
				$"IsReadOnly: {CombinedRichEditBox.IsReadOnly}\n" +
				$"MaxLength: {CombinedRichEditBox.MaxLength}\n" +
				$"AcceptsReturn: {CombinedRichEditBox.AcceptsReturn}\n" +
				$"TextWrapping: {CombinedRichEditBox.TextWrapping}\n" +
				$"TextAlignment: {CombinedRichEditBox.TextAlignment}\n" +
				$"CharacterCasing: {CombinedRichEditBox.CharacterCasing}\n" +
				$"IsEnabled: {CombinedRichEditBox.IsEnabled}\n" +
				$"IsSpellCheckEnabled: {CombinedRichEditBox.IsSpellCheckEnabled}\n" +
				$"IsTextPredictionEnabled: {CombinedRichEditBox.IsTextPredictionEnabled}";
		}
	}
}
