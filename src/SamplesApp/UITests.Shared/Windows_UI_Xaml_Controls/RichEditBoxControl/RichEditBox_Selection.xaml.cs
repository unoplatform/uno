using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_Selection",
		Description = "Demonstrates selection manipulation: SetRange, StartPosition/EndPosition, Expand, Collapse, TypeText.")]
	public sealed partial class RichEditBox_Selection : UserControl
	{
		public RichEditBox_Selection()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.SetText(TextSetOptions.None,
				"The quick brown fox jumps over the lazy dog.\rThis is the second paragraph with more words.\rAnd a third paragraph for testing selection features.");
		}

		private void SelectionRichEditBox_SelectionChanged(object sender, RoutedEventArgs e)
		{
			var sel = SelectionRichEditBox.Document.Selection;
			SelectionInfoTextBlock.Text =
				$"Start: {sel.StartPosition}, End: {sel.EndPosition}, Length: {sel.Length}, Type: {sel.Type}";

			sel.GetText(TextGetOptions.None, out var selectedText);
			SelectedTextBlock.Text = string.IsNullOrEmpty(selectedText)
				? "Selected text: (none)"
				: $"Selected text: \"{selectedText}\"";
		}

		// SetRange
		private void SetRangeButton_Click(object sender, RoutedEventArgs e)
		{
			var start = (int)SetRangeStart.Value;
			var end = (int)SetRangeEnd.Value;
			SelectionRichEditBox.Document.Selection.SetRange(start, end);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		// StartPosition / EndPosition
		private void SetStartPositionButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.StartPosition = (int)StartPositionInput.Value;
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void SetEndPositionButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.EndPosition = (int)EndPositionInput.Value;
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		// Expand
		private void ExpandWordButton_Click(object sender, RoutedEventArgs e)
		{
			var delta = SelectionRichEditBox.Document.Selection.Expand(TextRangeUnit.Word);
			ExpandResultTextBlock.Text = $"Expanded by {delta} characters (Word)";
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void ExpandParagraphButton_Click(object sender, RoutedEventArgs e)
		{
			var delta = SelectionRichEditBox.Document.Selection.Expand(TextRangeUnit.Paragraph);
			ExpandResultTextBlock.Text = $"Expanded by {delta} characters (Paragraph)";
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void ExpandStoryButton_Click(object sender, RoutedEventArgs e)
		{
			var delta = SelectionRichEditBox.Document.Selection.Expand(TextRangeUnit.Story);
			ExpandResultTextBlock.Text = $"Expanded by {delta} characters (Story = select all)";
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		// Collapse
		private void CollapseStartButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.Collapse(false);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void CollapseEndButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.Collapse(true);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		// TypeText
		private void TypeTextButton_Click(object sender, RoutedEventArgs e)
		{
			var text = TypeTextInput.Text;
			if (!string.IsNullOrEmpty(text))
			{
				SelectionRichEditBox.Document.Selection.TypeText(text);
				SelectionRichEditBox.Focus(FocusState.Programmatic);
			}
		}

		// Move left/right
		private void MoveLeftCharButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.MoveLeft(TextRangeUnit.Character, 1, false);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void MoveRightCharButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.MoveRight(TextRangeUnit.Character, 1, false);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void MoveLeftWordButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.MoveLeft(TextRangeUnit.Word, 1, false);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void MoveRightWordButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.MoveRight(TextRangeUnit.Word, 1, false);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		// Extend (select while moving)
		private void ExtendLeftButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.MoveLeft(TextRangeUnit.Character, 1, true);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void ExtendRightButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.MoveRight(TextRangeUnit.Character, 1, true);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		// Home / End
		private void HomeLineButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.HomeKey(TextRangeUnit.Line, false);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void EndLineButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.EndKey(TextRangeUnit.Line, false);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void HomeStoryButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.HomeKey(TextRangeUnit.Story, false);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		private void EndStoryButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionRichEditBox.Document.Selection.EndKey(TextRangeUnit.Story, false);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}

		// GetText / SetText on selection
		private void GetSelectionTextButton_Click(object sender, RoutedEventArgs e)
		{
			var sel = SelectionRichEditBox.Document.Selection;
			sel.GetText(TextGetOptions.None, out var text);
			SelectionGetTextOutput.Text = string.IsNullOrEmpty(text)
				? "(no text selected)"
				: $"Selection text: \"{text}\"";
		}

		private void SetSelectionTextButton_Click(object sender, RoutedEventArgs e)
		{
			var newText = SelectionSetTextInput.Text ?? string.Empty;
			SelectionRichEditBox.Document.Selection.SetText(TextSetOptions.None, newText);
			SelectionRichEditBox.Focus(FocusState.Programmatic);
		}
	}
}
