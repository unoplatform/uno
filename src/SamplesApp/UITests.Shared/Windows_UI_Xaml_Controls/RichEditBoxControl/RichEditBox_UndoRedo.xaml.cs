using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_UndoRedo",
		Description = "Demonstrates undo/redo functionality, undo groups, and batch display updates.")]
	public sealed partial class RichEditBox_UndoRedo : UserControl
	{
		public RichEditBox_UndoRedo()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			UndoRichEditBox.Document.SetText(TextSetOptions.None,
				"Edit this text to build up undo history. Then use the controls below to undo/redo changes.");
		}

		private void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
		{
			var doc = UndoRichEditBox.Document;
			UndoRedoStatusTextBlock.Text =
				$"Can undo: {doc.CanUndo()}\n" +
				$"Can redo: {doc.CanRedo()}\n" +
				$"Undo limit: {doc.UndoLimit}";
		}

		private void UndoButton_Click(object sender, RoutedEventArgs e)
		{
			var doc = UndoRichEditBox.Document;
			if (doc.CanUndo())
			{
				doc.Undo();
				UndoRedoResultTextBlock.Text = "Undo performed successfully.";
			}
			else
			{
				UndoRedoResultTextBlock.Text = "Nothing to undo.";
			}
		}

		private void RedoButton_Click(object sender, RoutedEventArgs e)
		{
			var doc = UndoRichEditBox.Document;
			if (doc.CanRedo())
			{
				doc.Redo();
				UndoRedoResultTextBlock.Text = "Redo performed successfully.";
			}
			else
			{
				UndoRedoResultTextBlock.Text = "Nothing to redo.";
			}
		}

		private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
		{
			UndoRichEditBox.Document.ClearUndoRedoHistory();
			UndoRedoResultTextBlock.Text = "Undo/redo history cleared.";
		}

		private void SetUndoLimitButton_Click(object sender, RoutedEventArgs e)
		{
			var limit = (uint)UndoLimitInput.Value;
			UndoRichEditBox.Document.UndoLimit = limit;
			UndoLimitStatusTextBlock.Text = $"Undo limit set to {limit}.";
		}

		private void UndoGroupDemoButton_Click(object sender, RoutedEventArgs e)
		{
			var doc = UndoRichEditBox.Document;

			// Clear and start fresh
			doc.SetText(TextSetOptions.None, "Original text.");
			doc.ClearUndoRedoHistory();

			// Begin an undo group - all operations between Begin and End
			// will be treated as a single undo step
			doc.BeginUndoGroup();

			// Multiple operations within the group
			var sel = doc.Selection;
			sel.SetRange(0, 0);
			sel.TypeText("Added: ");

			sel.SetRange(doc.Selection.EndPosition, doc.Selection.EndPosition);
			sel.TypeText(" And more.");

			doc.EndUndoGroup();

			doc.GetText(TextGetOptions.None, out var finalText);
			UndoGroupResultTextBlock.Text =
				$"After grouped operations: \"{finalText}\"\n\n" +
				$"All the changes above are in a single undo group.\n" +
				$"Press Undo once to revert all of them together.";
		}

		private void BatchUpdatesDemoButton_Click(object sender, RoutedEventArgs e)
		{
			var doc = UndoRichEditBox.Document;

			doc.SetText(TextSetOptions.None, "Start.");
			doc.ClearUndoRedoHistory();

			// Batch display updates - suppresses rendering until ApplyDisplayUpdates
			var count = doc.BatchDisplayUpdates();

			var sel = doc.Selection;
			sel.SetRange(5, 5);
			sel.TypeText(" Middle.");

			sel.SetRange(doc.Selection.EndPosition, doc.Selection.EndPosition);
			sel.TypeText(" End.");

			var remaining = doc.ApplyDisplayUpdates();

			doc.GetText(TextGetOptions.None, out var result);
			BatchUpdatesResultTextBlock.Text =
				$"Batch count after BatchDisplayUpdates: {count}\n" +
				$"Remaining after ApplyDisplayUpdates: {remaining}\n" +
				$"Final text: \"{result}\"";
		}

		private void Step1Button_Click(object sender, RoutedEventArgs e)
		{
			UndoRichEditBox.Document.SetText(TextSetOptions.None, "Step 1: Initial content.");
		}

		private void Step2Button_Click(object sender, RoutedEventArgs e)
		{
			var doc = UndoRichEditBox.Document;
			doc.GetText(TextGetOptions.None, out var current);
			doc.SetText(TextSetOptions.None, current + "\rStep 2: Appended paragraph.");
		}

		private void Step3Button_Click(object sender, RoutedEventArgs e)
		{
			var doc = UndoRichEditBox.Document;
			doc.SetText(TextSetOptions.None, "Step 3: Completely replaced content.");
		}
	}
}
