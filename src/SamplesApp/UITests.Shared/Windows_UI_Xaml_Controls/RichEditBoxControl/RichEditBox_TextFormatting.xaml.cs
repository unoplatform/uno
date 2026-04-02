using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_TextFormatting",
		Description = "Demonstrates text formatting: Bold, Italic, Underline, Strikethrough on selected text.")]
	public sealed partial class RichEditBox_TextFormatting : UserControl
	{
		public RichEditBox_TextFormatting()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			// Set up pre-formatted text
			var doc = PreFormattedRichEditBox.Document;
			doc.SetText(TextSetOptions.None, "This text has mixed formatting applied programmatically.");

			// Apply bold to "mixed"
			var range1 = doc.GetRange(19, 24);
			var fmt1 = range1.CharacterFormat;
			fmt1.Bold = FormatEffect.On;
			range1.CharacterFormat = fmt1;

			// Apply italic to "formatting"
			var range2 = doc.GetRange(25, 35);
			var fmt2 = range2.CharacterFormat;
			fmt2.Italic = FormatEffect.On;
			range2.CharacterFormat = fmt2;

			// Apply underline to "programmatically"
			var range3 = doc.GetRange(44, 59);
			var fmt3 = range3.CharacterFormat;
			fmt3.Underline = UnderlineType.Single;
			range3.CharacterFormat = fmt3;

			// Pre-populate the range format editor
			RangeFormatRichEditBox.Document.SetText(TextSetOptions.None,
				"Hello World, this is sample text for range formatting.");
		}

		private void BoldButton_Click(object sender, RoutedEventArgs e)
		{
			var selection = FormattingRichEditBox.Document.Selection;
			if (selection != null)
			{
				var fmt = selection.CharacterFormat;
				fmt.Bold = fmt.Bold == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
				selection.CharacterFormat = fmt;
			}
		}

		private void ItalicButton_Click(object sender, RoutedEventArgs e)
		{
			var selection = FormattingRichEditBox.Document.Selection;
			if (selection != null)
			{
				var fmt = selection.CharacterFormat;
				fmt.Italic = fmt.Italic == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
				selection.CharacterFormat = fmt;
			}
		}

		private void UnderlineButton_Click(object sender, RoutedEventArgs e)
		{
			var selection = FormattingRichEditBox.Document.Selection;
			if (selection != null)
			{
				var fmt = selection.CharacterFormat;
				fmt.Underline = fmt.Underline == UnderlineType.Single ? UnderlineType.None : UnderlineType.Single;
				selection.CharacterFormat = fmt;
			}
		}

		private void StrikethroughButton_Click(object sender, RoutedEventArgs e)
		{
			var selection = FormattingRichEditBox.Document.Selection;
			if (selection != null)
			{
				var fmt = selection.CharacterFormat;
				fmt.Strikethrough = fmt.Strikethrough == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
				selection.CharacterFormat = fmt;
			}
		}

		private void ClearFormattingButton_Click(object sender, RoutedEventArgs e)
		{
			var selection = FormattingRichEditBox.Document.Selection;
			if (selection != null)
			{
				var fmt = selection.CharacterFormat;
				fmt.Bold = FormatEffect.Off;
				fmt.Italic = FormatEffect.Off;
				fmt.Underline = UnderlineType.None;
				fmt.Strikethrough = FormatEffect.Off;
				selection.CharacterFormat = fmt;
			}
		}

		private void CheckFormatButton_Click(object sender, RoutedEventArgs e)
		{
			var selection = FormattingRichEditBox.Document.Selection;
			if (selection == null || selection.Length == 0)
			{
				FormatInfoTextBlock.Text = "No text selected. Select some text first.";
				return;
			}

			var fmt = selection.CharacterFormat;
			var info = $"Selection ({selection.StartPosition}-{selection.EndPosition}):\n" +
					   $"  Bold: {fmt.Bold}\n" +
					   $"  Italic: {fmt.Italic}\n" +
					   $"  Underline: {fmt.Underline}\n" +
					   $"  Strikethrough: {fmt.Strikethrough}\n" +
					   $"  Size: {fmt.Size}";
			FormatInfoTextBlock.Text = info;
		}

		private void ApplyRangeFormatButton_Click(object sender, RoutedEventArgs e)
		{
			var doc = RangeFormatRichEditBox.Document;
			var start = (int)FormatStartPosition.Value;
			var end = (int)FormatEndPosition.Value;
			var range = doc.GetRange(start, end);
			var fmt = range.CharacterFormat;

			var selectedFormat = (FormatTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
			switch (selectedFormat)
			{
				case "Bold":
					fmt.Bold = FormatEffect.On;
					break;
				case "Italic":
					fmt.Italic = FormatEffect.On;
					break;
				case "Underline":
					fmt.Underline = UnderlineType.Single;
					break;
				case "Strikethrough":
					fmt.Strikethrough = FormatEffect.On;
					break;
			}

			range.CharacterFormat = fmt;
		}
	}
}
