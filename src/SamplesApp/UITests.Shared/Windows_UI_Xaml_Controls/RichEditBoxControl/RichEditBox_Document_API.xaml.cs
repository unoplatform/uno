using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_Document_API",
		Description = "Demonstrates Document.GetText/SetText, Document.GetRange, and GetDefaultCharacterFormat APIs.")]
	public sealed partial class RichEditBox_Document_API : UserControl
	{
		public RichEditBox_Document_API()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			MainRichEditBox.Document.SetText(TextSetOptions.None,
				"The quick brown fox jumps over the lazy dog.\rThis is the second paragraph.\rAnd here is a third one.");
		}

		private void GetTextButton_Click(object sender, RoutedEventArgs e)
		{
			MainRichEditBox.Document.GetText(TextGetOptions.None, out var text);
			GetTextOutputTextBlock.Text = string.IsNullOrEmpty(text)
				? "(document is empty)"
				: $"Length: {text.Length}\nContent: \"{text}\"";
		}

		private void GetTextCrLfButton_Click(object sender, RoutedEventArgs e)
		{
			MainRichEditBox.Document.GetText(TextGetOptions.UseCrlf, out var text);
			GetTextOutputTextBlock.Text = string.IsNullOrEmpty(text)
				? "(document is empty)"
				: $"Length: {text.Length} (with CRLF)\nContent: \"{text}\"";
		}

		private void SetTextNoneButton_Click(object sender, RoutedEventArgs e)
		{
			var inputText = SetTextInput.Text;
			if (!string.IsNullOrEmpty(inputText))
			{
				MainRichEditBox.Document.SetText(TextSetOptions.None, inputText);
			}
		}

		private void ClearTextButton_Click(object sender, RoutedEventArgs e)
		{
			MainRichEditBox.Document.SetText(TextSetOptions.None, string.Empty);
		}

		private void GetRangeButton_Click(object sender, RoutedEventArgs e)
		{
			var start = (int)RangeStartPosition.Value;
			var end = (int)RangeEndPosition.Value;
			var range = MainRichEditBox.Document.GetRange(start, end);
			range.GetText(TextGetOptions.None, out var rangeText);

			var fmt = range.CharacterFormat;
			RangeTextOutputTextBlock.Text =
				$"Range [{start}, {end}]:\n" +
				$"  Text: \"{rangeText}\"\n" +
				$"  Length: {rangeText.Length}\n" +
				$"  Bold: {fmt.Bold}\n" +
				$"  Italic: {fmt.Italic}\n" +
				$"  Underline: {fmt.Underline}";
		}

		private void ReplaceRangeButton_Click(object sender, RoutedEventArgs e)
		{
			var start = (int)RangeSetStart.Value;
			var end = (int)RangeSetEnd.Value;
			var replacementText = RangeSetTextInput.Text ?? string.Empty;

			var range = MainRichEditBox.Document.GetRange(start, end);
			range.SetText(TextSetOptions.None, replacementText);
		}

		private void GetDefaultFormatButton_Click(object sender, RoutedEventArgs e)
		{
			var fmt = MainRichEditBox.Document.GetDefaultCharacterFormat();
			DefaultFormatOutputTextBlock.Text =
				$"Default Character Format:\n" +
				$"  Bold: {fmt.Bold}\n" +
				$"  Italic: {fmt.Italic}\n" +
				$"  Underline: {fmt.Underline}\n" +
				$"  Strikethrough: {fmt.Strikethrough}\n" +
				$"  Size: {fmt.Size}\n" +
				$"  Name: \"{fmt.Name}\"\n" +
				$"  AllCaps: {fmt.AllCaps}\n" +
				$"  SmallCaps: {fmt.SmallCaps}\n" +
				$"  Subscript: {fmt.Subscript}\n" +
				$"  Superscript: {fmt.Superscript}";
		}

		private void RefreshDocInfoButton_Click(object sender, RoutedEventArgs e)
		{
			var doc = MainRichEditBox.Document;
			doc.GetText(TextGetOptions.None, out var fullText);

			var sel = doc.Selection;
			DocInfoTextBlock.Text =
				$"Document Info:\n" +
				$"  Total text length: {fullText.Length}\n" +
				$"  Selection start: {sel.StartPosition}\n" +
				$"  Selection end: {sel.EndPosition}\n" +
				$"  Selection length: {sel.Length}\n" +
				$"  Selection type: {sel.Type}\n" +
				$"  Can undo: {doc.CanUndo()}\n" +
				$"  Can redo: {doc.CanRedo()}\n" +
				$"  Undo limit: {doc.UndoLimit}\n" +
				$"  Default tab stop: {doc.DefaultTabStop}\n" +
				$"  Caret type: {doc.CaretType}";
		}
	}
}
